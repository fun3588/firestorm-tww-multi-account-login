using System.Reflection;
using logingui;
internal static class Preview
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var isolated = Path.Combine(Path.GetTempPath(), "logingui-ui-" + Guid.NewGuid().ToString("N"));
        AppContext.SetData("logingui.DataDirectory", isolated);
        var settingsPath = AccountStore.SettingsPath;
        var originalSettings = File.Exists(settingsPath) ? File.ReadAllBytes(settingsPath) : null;
        try
        {
            using var form = new Form1();
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-10000, -10000);
            form.ShowInTaskbar = false;
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var accounts = (List<Account>)typeof(Form1).GetField("_accounts", flags)!.GetValue(form)!;
            accounts.Clear();
            for (int i = 0; i < 80; i++) accounts.Add(new Account { Email = $"sample{i}@example.test" });
            typeof(Form1).GetMethod("RefreshList", flags)!.Invoke(form, null);
            var list = (ListView)typeof(Form1).GetField("_list", flags)!.GetValue(form)!;
            form.Show();
            Application.DoEvents();
            var up = (Button)typeof(Form1).GetField("_btnMoveUp", flags)!.GetValue(form)!;
            var down = (Button)typeof(Form1).GetField("_btnMoveDown", flags)!.GetValue(form)!;
            if (!up.Visible || !down.Visible) throw new Exception("Move buttons must be visible");
            if (up.Top != down.Top || up.Right > down.Left) throw new Exception("Move buttons layout is invalid");
            list.EnsureVisible(79);
            Application.DoEvents();
            if (!list.Scrollable || list.TopItem!.Index == 0) throw new Exception("Vertical scroll failed");
            var top = list.TopItem.Index;
            var picker = (ComboBox)typeof(Form1).GetField("_languagePicker", flags)!.GetValue(form)!;
            if (picker.Items.Count != 2) throw new Exception("Only Chinese and English should be offered");
            foreach (var choice in new[] { (1, "Launch Game"), (0, "启动游戏") })
            {
                picker.SelectedIndex = choice.Item1;
                Application.DoEvents();
                var launch = (Button)typeof(Form1).GetField("_btnLogin", flags)!.GetValue(form)!;
                if (launch.Text != choice.Item2) throw new Exception("Live language switch failed");
                if (!form.Text.Contains("v1.2")) throw new Exception("Version title is incorrect");
                var toolbar = (FlowLayoutPanel)typeof(Form1).GetField("_toolbar", flags)!.GetValue(form)!;
                var rowCount = toolbar.Controls.Cast<Control>().Select(c => c.Top).Distinct().Count();
                if (rowCount != 1) throw new Exception("Unexpected toolbar row count");
                if (list.TopItem!.Index != top) throw new Exception("Scroll position was lost");
            }
            using var dialog = new AccountDialog(new Account { Email = "sample@example.test" }, settings: new AppSettings { ShowPasswordByDefault = false, ShowTokenByDefault = false });
            var token = (TextBox)typeof(AccountDialog).GetField("_token", flags)!.GetValue(dialog)!;
            var show = (CheckBox)typeof(AccountDialog).GetField("_showToken", flags)!.GetValue(dialog)!;
            token.Text = "test-session-token";
            if (!token.UseSystemPasswordChar) throw new Exception("Token should be hidden by default");
            show.Checked = true;
            if (token.UseSystemPasswordChar || token.Text != "test-session-token") throw new Exception("Show token failed");
            show.Checked = false;
            if (!token.UseSystemPasswordChar || token.Text != "test-session-token") throw new Exception("Hide token failed");
            foreach (bool passwordVisible in new[] { false, true })
            foreach (bool tokenVisible in new[] { false, true })
            {
                var defaults = new AppSettings { ShowPasswordByDefault = passwordVisible, ShowTokenByDefault = tokenVisible };
                var json = System.Text.Json.JsonSerializer.Serialize(defaults);
                var restored = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json)!;
                using var configured = new AccountDialog(new Account { Email = "configured@example.test", Password = "test-password" }, settings: restored);
                var pwdBox = (TextBox)typeof(AccountDialog).GetField("_password", flags)!.GetValue(configured)!;
                var tokenBox = (TextBox)typeof(AccountDialog).GetField("_token", flags)!.GetValue(configured)!;
                if (pwdBox.UseSystemPasswordChar == passwordVisible || tokenBox.UseSystemPasswordChar == tokenVisible)
                    throw new Exception("Visibility defaults not applied");
            }
            using var settings = new SettingsDialog(new AppSettings());
            settings.ShowInTaskbar = false;
            settings.StartPosition = FormStartPosition.Manual;
            settings.Location = new Point(-10000, -10000);
            settings.Show();
            var defaultPassword = (CheckBox)typeof(SettingsDialog).GetField("_defaultShowPassword", flags)!.GetValue(settings)!;
            var defaultToken = (CheckBox)typeof(SettingsDialog).GetField("_defaultShowToken", flags)!.GetValue(settings)!;
            defaultPassword.Checked = false;
            defaultToken.Checked = false;
            ((Button)settings.AcceptButton!).PerformClick();
            if (settings.Result.ShowPasswordByDefault || settings.Result.ShowTokenByDefault)
                throw new Exception("Settings values not returned");
            Console.WriteLine("PASS: all visibility default combinations, JSON round trip, settings controls");
            foreach (var language in new[] { 0, 1 })
            {
                picker.SelectedIndex = language; Application.DoEvents();
                using var preview = new Bitmap(form.Width, form.Height); form.DrawToBitmap(preview, new Rectangle(Point.Empty, preview.Size));
                preview.Save(Path.Combine(Path.GetTempPath(), language == 0 ? "logingui-v12-zh.png" : "logingui-v12-en.png"));
            }
            form.Close();
            Console.WriteLine("PASS: 80-account vertical scroll, live language switching, scroll retention, token visibility");
        }
        finally
        {
            if (originalSettings != null) File.WriteAllBytes(settingsPath, originalSettings);
            else if (File.Exists(settingsPath)) File.Delete(settingsPath);
        }
    }
}
