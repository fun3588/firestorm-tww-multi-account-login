namespace logingui;

public sealed class SettingsDialog : Form
{
    public AppSettings Result { get; }

    private readonly TextBox _exe = new();
    private readonly TextBox _args = new();
    private readonly TextBox _proxy = new();

    private readonly CheckBox _defaultShowPassword = new();
    private readonly CheckBox _defaultShowToken = new();

    public SettingsDialog(AppSettings settings)
    {
        Result = new AppSettings
        {
            WowExe = settings.WowExe,
            ExtraArgs = settings.ExtraArgs,
            HttpProxy = settings.HttpProxy,
            LastSelectedEmail = settings.LastSelectedEmail,
            UiLanguage = settings.UiLanguage,
            ShowPasswordByDefault = settings.ShowPasswordByDefault,
            ShowTokenByDefault = settings.ShowTokenByDefault,
        };

        Text = UiText.Get("设置");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(850, 430);

        var lbl1 = new Label { Text = UiText.Get("游戏程序:"), Left = 20, Top = 27, Width = 190, TextAlign = ContentAlignment.MiddleRight };
        _exe.Left = 220; _exe.Top = 24; _exe.Width = 430;
        var browse = new Button { Text = UiText.Get("浏览..."), Left = 668, Top = 22, Width = 156 };
        browse.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = UiText.Get("可执行文件 (*.exe)|*.exe"), Title = UiText.Get("选择 WoW 客户端") };
            if (ofd.ShowDialog(this) == DialogResult.OK) _exe.Text = ofd.FileName;
        };

        var lbl2 = new Label { Text = UiText.Get("额外参数:"), Left = 20, Top = 97, Width = 190, TextAlign = ContentAlignment.MiddleRight };
        _args.Left = 220; _args.Top = 94; _args.Width = 604;

        var lbl3 = new Label { Text = UiText.Get("API 代理:"), Left = 20, Top = 177, Width = 190, TextAlign = ContentAlignment.MiddleRight };
        _proxy.Left = 220; _proxy.Top = 174; _proxy.Width = 604;

        var proxyHint = new Label
        {
            Text = UiText.Get("留空表示直连。修改代理后需重启程序。"),
            Left = 220, Top = 214, Width = 604, Height = 50, ForeColor = Color.DimGray, AutoSize = false,
        };

        var argHint = new Label
        {
            Text = UiText.Get("启动参数会自动附带 -launcherlogin。"),
            Left = 220, Top = 134, Width = 604, Height = 36, ForeColor = Color.DimGray, AutoSize = false,
        };

        _defaultShowPassword.Text = UiText.Get("默认显示密码");
        _defaultShowPassword.SetBounds(220, 270, 604, 30);
        _defaultShowPassword.Checked = settings.ShowPasswordByDefault;
        _defaultShowToken.Text = UiText.Get("默认显示令牌");
        _defaultShowToken.SetBounds(220, 310, 604, 30);
        _defaultShowToken.Checked = settings.ShowTokenByDefault;
        Controls.AddRange(new Control[] { _defaultShowPassword, _defaultShowToken });

        var ok = new Button { Text = UiText.Get("确定"), Left = 488, Top = 366, Width = 160, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = UiText.Get("取消"), Left = 664, Top = 366, Width = 160, DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) =>
        {
            Result.WowExe = _exe.Text.Trim();
            Result.ExtraArgs = _args.Text.Trim();
            Result.HttpProxy = _proxy.Text.Trim();
            Result.ShowPasswordByDefault = _defaultShowPassword.Checked;
            Result.ShowTokenByDefault = _defaultShowToken.Checked;
        };

        Controls.AddRange(new Control[] { lbl1, _exe, browse, lbl2, _args, argHint, lbl3, _proxy, proxyHint, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;

        _exe.Text = settings.WowExe;
        _args.Text = settings.ExtraArgs;
        _proxy.Text = settings.HttpProxy;
        UiTheme.Apply(this);
        UiTheme.StyleButton(ok, primary: true);
    }
}
