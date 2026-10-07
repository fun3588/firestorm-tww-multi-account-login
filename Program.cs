namespace logingui;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try { Run(args); }
        catch (Exception ex)
        {
            AccountStore.Log(UiText.Get("启动或命令执行失败: ") + ex.Message);
            Environment.ExitCode = 1;
            if (args.Length == 0) MessageBox.Show(ex.Message, UiText.Get("启动失败"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            else Console.Error.WriteLine(ex.Message);
        }
    }

    private static void Run(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) =>
        {
            AccountStore.Log(UiText.Get("操作失败: ") + e.Exception.Message);
            MessageBox.Show(e.Exception.Message, UiText.Get("操作失败"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        var settings = AccountStore.LoadSettings();
        FirestormApi.SetProxy(settings.HttpProxy);

        // Route the embedded WebView2 (Turnstile) through the same proxy.
        if (!string.IsNullOrWhiteSpace(settings.HttpProxy))
            Environment.SetEnvironmentVariable(
                "WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--proxy-server=" + settings.HttpProxy);

        if (args.Length > 0 && args[0] == "--selftest-captcha")
        {
            SelfTest.RunCaptcha();
            return;
        }

        if (args.Length > 1 && args[0] == "--selftest-ticket")
        {
            SelfTest.RunTicket(args[1]);
            return;
        }

        if (args.Length > 1 && args[0] == "--selftest-full")
        {
            SelfTest.RunFull(args[1]);
            return;
        }

        if (args.Length > 0 && args[0] == "--selftest-dummylogin")
        {
            SelfTest.RunDummyLogin();
            return;
        }

        // ---- launcher integration / headless batch modes ----
        if (args.Length > 0 && args[0] == "--import-launcher")
        {
            Cli.ImportLauncher();
            return;
        }

        if (args.Length > 0 && args[0] == "--list-tokens")
        {
            Cli.ListTokens();
            return;
        }

        if (args.Length > 1 && args[0] == "--token-login")
        {
            Cli.TokenLogin(args[1], args.Any(a => a == "--launch"));
            return;
        }

        Application.Run(new Form1());
    }
}
