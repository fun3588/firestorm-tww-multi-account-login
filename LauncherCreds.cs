using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace logingui;

/// <summary>
/// Reads what the official FirestormLauncher already has on this machine:
///  - account list from %APPDATA%\com.firestorm.launcher\auth.json (emails only)
///  - the per-account session token from Windows Credential Manager, stored by
///    the launcher's Rust `keyring` crate under target "&lt;email&gt;.Firestorm".
/// A valid session token can fetch a WoW login ticket without the password.
/// </summary>
public static class LauncherCreds
{
    public static string LauncherDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "com.firestorm.launcher");

    public static string AuthJsonPath => Path.Combine(LauncherDir, "auth.json");

    // ---------- Windows Credential Manager (generic) ----------

    private const uint CRED_TYPE_GENERIC = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CredReadW")]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredFree")]
    private static extern void CredFree(IntPtr cred);

    /// <summary>Reads the launcher's saved session token for an account, or null.</summary>
    public static string? ReadSessionToken(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var target = email.Trim() + ".Firestorm";

        if (!CredRead(target, CRED_TYPE_GENERIC, 0, out var ptr))
        {
            AccountStore.Log($"launcher cred: not found for {target} (winerr={Marshal.GetLastWin32Error()})");
            return null;
        }

        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(ptr);
            if (cred.CredentialBlobSize == 0 || cred.CredentialBlob == IntPtr.Zero) return null;

            var bytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, bytes.Length);

            // The launcher (keyring-rs) stores the secret as UTF-16LE.
            var s = Encoding.Unicode.GetString(bytes).Trim('\0', ' ', '\r', '\n', '\t');
            if (string.IsNullOrWhiteSpace(s))
                s = Encoding.UTF8.GetString(bytes).Trim('\0', ' ', '\r', '\n', '\t');

            AccountStore.Log($"launcher cred: read token for {email} len={s.Length}");
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        finally
        {
            CredFree(ptr);
        }
    }

    public static bool HasSessionToken(string email)
    {
        try { return !string.IsNullOrEmpty(ReadSessionToken(email)); }
        catch { return false; }
    }

    // ---------- account list from auth.json ----------

    public sealed class LauncherAccount
    {
        public string Email { get; set; } = "";
        public long LastUsedAt { get; set; }
        public int ServiceFlags { get; set; }

        public DateTime? LastUsedLocal => LastUsedAt > 0
            ? DateTimeOffset.FromUnixTimeSeconds(LastUsedAt).ToLocalTime().DateTime
            : null;
    }

    public static List<LauncherAccount> DiscoverAccounts()
    {
        var list = new List<LauncherAccount>();
        try
        {
            if (!File.Exists(AuthJsonPath)) return list;

            using var doc = JsonDocument.Parse(File.ReadAllText(AuthJsonPath));
            if (doc.RootElement.TryGetProperty("accounts", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in arr.EnumerateArray())
                {
                    list.Add(new LauncherAccount
                    {
                        Email = a.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "",
                        LastUsedAt = a.TryGetProperty("last_used_at", out var l) && l.ValueKind == JsonValueKind.Number
                            ? l.GetInt64() : 0,
                        ServiceFlags = a.TryGetProperty("service_flags", out var s) && s.ValueKind == JsonValueKind.Number
                            ? s.GetInt32() : 0,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            throw new IOException(UiText.Get("无法读取 Firestorm Launcher 账号文件，未执行导入。"), ex);
        }
        return list.Where(a => !string.IsNullOrWhiteSpace(a.Email)).ToList();
    }

    public static string? ActiveEmail()
    {
        try
        {
            if (!File.Exists(AuthJsonPath)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(AuthJsonPath));
            return doc.RootElement.TryGetProperty("active_email", out var e) ? e.GetString() : null;
        }
        catch { return null; }
    }

    /// <summary>Best-effort: the game exe the launcher currently has installed, or null.</summary>
    public static string? ResolveWowExe()
    {
        try
        {
            var path = Path.Combine(LauncherDir, "installs.json");
            if (!File.Exists(path)) return null;

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var v = prop.Value;
                if (v.ValueKind != JsonValueKind.Object) continue;

                var exe = v.TryGetProperty("executable", out var e) ? e.GetString() : null;
                var dir = v.TryGetProperty("install_path", out var d) ? d.GetString() : null;
                if (string.IsNullOrWhiteSpace(exe) || string.IsNullOrWhiteSpace(dir)) continue;

                var full = Path.Combine(dir!, exe!);
                if (File.Exists(full)) return full;
            }
        }
        catch (Exception ex)
        {
            AccountStore.Log("launcher installs.json parse fail: " + ex.Message);
        }
        return null;
    }

    public sealed record ImportResult(int Found, int Added, int TokensSaved);

    public static ImportResult ImportInto(List<Account> accounts) =>
        MergeAccounts(accounts, DiscoverAccounts(), ActiveEmail(), TokenStore.Read,
            ReadSessionToken, TokenStore.Save);

    internal static ImportResult MergeAccounts(List<Account> accounts,
        IEnumerable<LauncherAccount> source, string? active,
        Func<string, string?> readLocal, Func<string, string?> readLauncher,
        Action<string, string> saveToken)
    {
        int found = 0, added = 0, saved = 0;
        foreach (var la in source)
        {
            var email = la.Email.Trim();
            if (email.Length == 0) continue;
            found++;
            var account = accounts.FirstOrDefault(a =>
                a.Email.Trim().Equals(email, StringComparison.OrdinalIgnoreCase));
            if (account == null)
            {
                account = new Account
                {
                    Email = email,
                    Note = email.Equals(active, StringComparison.OrdinalIgnoreCase)
                        ? "imported from FirestormLauncher (active)" : "imported from FirestormLauncher",
                    LastUsed = la.LastUsedLocal?.ToString("yyyy-MM-dd HH:mm:ss") ?? "",
                };
                accounts.Add(account);
                added++;
            }
            try
            {
                if (!string.IsNullOrWhiteSpace(readLocal(account.Email))) continue;
                var token = readLauncher(email);
                if (string.IsNullOrWhiteSpace(token)) continue;
                saveToken(account.Email, token);
                saved++;
            }
            catch (Exception ex)
            {
                AccountStore.Log($"导入账号 {email} 的令牌失败: " + ex.Message);
            }
        }
        return new ImportResult(found, added, saved);
    }
}
