namespace logingui;

using Microsoft.Web.WebView2.WinForms;

internal static class SelfTest
{
    public static void RunCaptcha()
    {
        var form = MakeForm();
        var web = (WebView2)form.Controls[0];
        form.Shown += async (_, _) =>
        {
            try
            {
                var ts = new TurnstileService(web);
                var token = await ts.GetTokenAsync(TimeSpan.FromSeconds(90));
                AccountStore.Log($"SELFTEST captcha OK len={token.Length} head={token[..Math.Min(24, token.Length)]}");
            }
            catch (Exception ex)
            {
                AccountStore.Log("SELFTEST captcha FAIL " + ex);
            }
            form.Close();
        };
        Application.Run(form);
    }

    public static void RunTicket(string token)
    {
        try
        {
            var t = FirestormApi.GetWowLoginTicketAsync(token).GetAwaiter().GetResult();
            AccountStore.Log($"SELFTEST ticket OK ticket={t.LoginTicket} portal={t.PortalAddress} raw={t.Raw}");
        }
        catch (Exception ex)
        {
            AccountStore.Log("SELFTEST ticket FAIL " + ex.Message);
        }
    }

    public static void RunFull(string token)
    {
        try
        {
            var t = FirestormApi.GetWowLoginTicketAsync(token).GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(t.LoginTicket)) { AccountStore.Log("SELFTEST full FAIL no ticket " + t.Raw); return; }

            WowLauncher.WriteLaunchOptions(t.LoginTicket, t.PortalAddress);

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Firestorm Servers Game\Battle.net\Launch Options\WoW");
            var blob = (byte[]?)key?.GetValue("WEB_TOKEN");
            var cs = key?.GetValue("CONNECTION_STRING") as string;
            var entropy = new byte[]
            {
                0xC8,0x76,0xF4,0xAE,0x4C,0x95,0x2E,0xFE,0xF2,0xFA,0x0F,0x54,0x19,0xC0,0x9C,0x43
            };
            var plain = System.Security.Cryptography.ProtectedData.Unprotect(
                blob!, entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            var decoded = System.Text.Encoding.UTF8.GetString(plain);
            AccountStore.Log($"SELFTEST full OK match={decoded == t.LoginTicket} conn={cs}");
        }
        catch (Exception ex)
        {
            AccountStore.Log("SELFTEST full FAIL " + ex);
        }
    }

    /// <summary>Fetches a fresh Turnstile token then performs a dummy login to probe server behaviour.</summary>
    public static void RunDummyLogin()
    {
        var form = MakeForm();
        var web = (WebView2)form.Controls[0];
        form.Shown += async (_, _) =>
        {
            try
            {
                var ts = new TurnstileService(web);
                var token = await ts.GetTokenAsync(TimeSpan.FromSeconds(90));
                AccountStore.Log($"SELFTEST dummylogin token len={token.Length}");
                var t = FirestormApi.LoginAsync("nouser@example.com", "badpass", "", token).GetAwaiter().GetResult();
                AccountStore.Log("SELFTEST dummylogin raw=" + t.Raw);
            }
            catch (Exception ex)
            {
                AccountStore.Log("SELFTEST dummylogin FAIL " + ex.Message);
            }
            form.Close();
        };
        Application.Run(form);
    }

    private static Form MakeForm()
    {
        var form = new Form { Width = 460, Height = 240, Text = "selftest", ShowInTaskbar = true };
        form.Controls.Add(new WebView2 { Dock = DockStyle.Fill });
        return form;
    }
}
