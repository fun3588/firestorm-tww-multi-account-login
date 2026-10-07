using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace logingui;

/// <summary>Independent per-user token cache; never modifies Firestorm Launcher credentials.</summary>
public static class TokenStore
{
    public static string TokensPath => Path.Combine(Path.GetDirectoryName(AccountStore.AccountsPath)!, "tokens.json");

    private static Dictionary<string, string> Load()
    {
        if (!File.Exists(TokensPath)) return new(StringComparer.OrdinalIgnoreCase);
        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(TokensPath))
            ?? throw new InvalidDataException(UiText.Get("令牌文件内容无效。"));
        return new(data, StringComparer.OrdinalIgnoreCase);
    }

    public static string? Read(string email)
    {
        try
        {
            if (!Load().TryGetValue(email.Trim(), out var encrypted)) return null;
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                Convert.FromBase64String(encrypted), null, DataProtectionScope.CurrentUser));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            AccountStore.Log($"读取本地令牌失败({email}): " + ex.Message);
            return null;
        }
    }

    public static bool Has(string email)
    {
        try { return !string.IsNullOrWhiteSpace(Read(email)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or CryptographicException or FormatException or ArgumentException)
        {
            AccountStore.Log(UiText.Get("读取本地令牌失败: ") + ex.Message);
            return false;
        }
    }

    public static void Save(string email, string token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            throw new ArgumentException(UiText.Get("账号和令牌不能为空。"));
        using var mutex = new Mutex(false, @"Local\logingui-token-store");
        bool locked = false;
        try
        {
            try { locked = mutex.WaitOne(TimeSpan.FromSeconds(10)); }
            catch (AbandonedMutexException) { locked = true; }
            if (!locked) throw new IOException(UiText.Get("令牌文件正在使用，请稍后重试。"));
            var data = Load();
            data[email.Trim()] = Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(token), null, DataProtectionScope.CurrentUser));
            Directory.CreateDirectory(Path.GetDirectoryName(TokensPath)!);
            var temporary = TokensPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            // Windows scanners can briefly hold the source or destination after a write.
            // Keep the original cache intact and retry the replacement for a bounded time.
            for (int attempt = 0; ; attempt++)
            {
                try { File.Move(temporary, TokensPath, true); break; }
                catch (Exception ex) when (attempt < 5 && ex is IOException or UnauthorizedAccessException)
                {
                    Thread.Sleep(100);
                }
            }
        }
        finally { if (locked) mutex.ReleaseMutex(); }
    }
    public static string? GetOrImport(string email)
    {
        var token = Read(email);
        if (!string.IsNullOrWhiteSpace(token)) return token;
        token = LauncherCreds.ReadSessionToken(email);
        if (!string.IsNullOrWhiteSpace(token)) Save(email, token);
        return token;
    }
}
