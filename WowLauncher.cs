using System.Security.Cryptography;
using Microsoft.Win32;

namespace logingui;

public static class WowLauncher
{
    // Same DPAPI entropy the Firestorm launcher uses (verified by decrypting an existing WEB_TOKEN).
    private static readonly byte[] Entropy =
    {
        0xC8, 0x76, 0xF4, 0xAE, 0x4C, 0x95, 0x2E, 0xFE,
        0xF2, 0xFA, 0x0F, 0x54, 0x19, 0xC0, 0x9C, 0x43,
    };

    private const string RegPath = @"Software\Firestorm Servers Game\Battle.net\Launch Options\WoW";

    public static string? ReadCurrentConnectionString()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath);
        return key?.GetValue("CONNECTION_STRING") as string;
    }

    /// <summary>Writes the account/ticket into the registry as the launcher does.</summary>
    public static void WriteLaunchOptions(string loginTicket, string connectionString, string gameAccount = "")
    {
        var ticketBytes = System.Text.Encoding.UTF8.GetBytes(loginTicket);
        var protectedToken = ProtectedData.Protect(ticketBytes, Entropy, DataProtectionScope.CurrentUser);

        using var key = Registry.CurrentUser.CreateSubKey(RegPath, writable: true)
            ?? throw new InvalidOperationException(UiText.Get("无法创建注册表项: ") + RegPath);

        key.SetValue("WEB_TOKEN", protectedToken, RegistryValueKind.Binary);
        key.SetValue("CONNECTION_STRING", connectionString, RegistryValueKind.String);
        key.SetValue("GAME_ACCOUNT", gameAccount, RegistryValueKind.String);

        AccountStore.Log($"registry written: CONNECTION_STRING={connectionString}, WEB_TOKEN={protectedToken.Length} bytes");
    }

    public static System.Diagnostics.Process Launch(string exePath, string extraArgs)
    {
        if (!File.Exists(exePath))
            throw new FileNotFoundException(UiText.Get("找不到游戏可执行文件: ") + exePath);

        var args = "-launcherlogin";
        if (!string.IsNullOrWhiteSpace(extraArgs)) args += " " + extraArgs.Trim();

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = true,
        };
        var process = System.Diagnostics.Process.Start(psi) ?? throw new IOException("Game process could not be started.");
        AccountStore.Log($"launched: \"{exePath}\" {args}");
        return process;
    }

    private sealed class LaunchLease : IDisposable
    {
        internal readonly ManualResetEventSlim Released = new();
        public void Dispose() => Released.Set();
    }
    public static Task<IDisposable> AcquireLaunchLockAsync(CancellationToken ct = default)
    {
        var completion = new TaskCompletionSource<IDisposable>(TaskCreationOptions.RunContinuationsAsynchronously);
        // Mutex ownership stays on this thread, including release after the async caller disposes its lease.
        new Thread(() =>
        {
            using var mutex = new Mutex(false, @"Local\FirestormAccountManagerLaunch");
            bool acquired = false;
            try
            {
                while (!acquired)
                {
                    ct.ThrowIfCancellationRequested();
                    try { acquired = mutex.WaitOne(200); } catch (AbandonedMutexException) { acquired = true; }
                }
                ct.ThrowIfCancellationRequested();
                var lease = new LaunchLease(); completion.TrySetResult(lease);
                lease.Released.Wait(); lease.Released.Dispose();
            }
            catch (OperationCanceledException) { completion.TrySetCanceled(ct); }
            catch (Exception ex) { completion.TrySetException(ex); }
            finally { if (acquired) mutex.ReleaseMutex(); }
        }) { IsBackground = true, Name = "Firestorm launch lock" }.Start();
        return completion.Task;
    }
    public static async Task<System.Diagnostics.Process> LaunchWithTicketAsync(string ticket, string portal, string exe, string args)
    {
        using var lease = await AcquireLaunchLockAsync();
        if (!File.Exists(exe)) throw new FileNotFoundException(UiText.Get("找不到游戏可执行文件: ") + exe);
        WriteLaunchOptions(ticket, portal);
        var process = Launch(exe, args);
        // Allow the new client to consume the shared registry ticket before another caller writes it.
        await Task.Delay(8000);
        return process;
    }
}
