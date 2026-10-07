using System.Runtime.InteropServices;

namespace logingui;

/// <summary>
/// Headless helpers so this WinExe can be scripted for batch operations.
/// Console output is attached to the parent console when launched from a terminal.
/// </summary>
internal static class Cli
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    private const int ATTACH_PARENT_PROCESS = -1;

    private static void AttachConsoleIfNeeded()
    {
        try
        {
            if (AttachConsole(ATTACH_PARENT_PROCESS))
            {
                var stdout = Console.OpenStandardOutput();
                var sw = new StreamWriter(stdout) { AutoFlush = true };
                Console.SetOut(sw);
            }
        }
        catch { /* no console available; output also goes to debug.log */ }
    }

    private static void Out(string line)
    {
        try { Console.WriteLine(line); } catch { }
        AccountStore.Log("CLI " + line);
    }

    /// <summary>--import-launcher : merge the official launcher's account list into accounts.json.</summary>
    public static void ImportLauncher()
    {
        AttachConsoleIfNeeded();
        var accounts = AccountStore.Load();
        LauncherCreds.ImportResult result;
        try { result = LauncherCreds.ImportInto(accounts); }
        finally { AccountStore.Save(accounts); }

        Out($"launcher accounts found={result.Found} added={result.Added} tokensSaved={result.TokensSaved} total={accounts.Count}");
        Out("accounts.json = " + AccountStore.AccountsPath);
        foreach (var a in accounts) Out("  " + a.Email);
    }

    /// <summary>--token-login &lt;email&gt; [--launch] : password-less login via the launcher's stored session token.</summary>
    public static void TokenLogin(string email, bool launch)
    {
        AttachConsoleIfNeeded();
        var token = TokenStore.GetOrImport(email);
        if (string.IsNullOrEmpty(token)) { Out($"NO-TOKEN {email}"); return; }

        var ticket = FirestormApi.GetWowLoginTicketAsync(token).GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(ticket.LoginTicket)) { Out($"NO-TICKET {email}"); return; }

        var portal = string.IsNullOrEmpty(ticket.PortalAddress)
            ? (WowLauncher.ReadCurrentConnectionString() ?? "eu.sl.logon.firestorm-servers.com:1119")
            : ticket.PortalAddress;

        Out($"OK {email} portal={portal}");

        if (launch)
        {
            var s = AccountStore.LoadSettings();
            var exe = s.WowExe;
            if (!File.Exists(exe)) exe = LauncherCreds.ResolveWowExe() ?? exe;
            using var process = WowLauncher.LaunchWithTicketAsync(ticket.LoginTicket, portal, exe, s.ExtraArgs).GetAwaiter().GetResult();
            Out($"launched {exe}");
        }
    }

    /// <summary>--list-tokens : show which launcher accounts have a stored session token.</summary>
    public static void ListTokens()
    {
        AttachConsoleIfNeeded();
        var list = AccountStore.Load().Select(a => a.Email)
            .Concat(LauncherCreds.DiscoverAccounts().Select(a => a.Email))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Out($"accounts={list.Count}");
        foreach (var a in list)
        {
            var t = TokenStore.Read(a) ?? LauncherCreds.ReadSessionToken(a);
            Out(string.IsNullOrEmpty(t) ? $"  NO  {a}" : $"  OK  {a}  len={t.Length}");
        }
    }
}
