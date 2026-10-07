namespace logingui;

public sealed class AccountDialog : Form
{
    public Account Result { get; }
    public string SessionToken { get; private set; } = "";

    private readonly TextBox _email = new();
    private readonly TextBox _password = new();
    private readonly TextBox _token = new();
    private readonly TextBox _note = new();
    private readonly CheckBox _showPwd = new() { Text = UiText.Get("显示密码") };

    private readonly CheckBox _showToken = new() { Text = UiText.Get("显示令牌") };

    public AccountDialog(Account account, bool isEdit = false, AppSettings? settings = null)
    {
        settings ??= AccountStore.LoadSettings();
        Result = new Account
        {
            Email = account.Email,
            Password = account.Password,
            AuthCode = account.AuthCode,
            Note = account.Note,
            LastUsed = account.LastUsed,
        };

        Text = isEdit ? UiText.Get("编辑账号") : UiText.Get("添加账号");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(720, 380);

        AddRow(UiText.Get("邮箱/账号:"), _email, 24);
        AddRow(UiText.Get("密码:"), _password, 84);
        _password.UseSystemPasswordChar = false;

        _showPwd.Left = 490; _showPwd.Top = 88; _showPwd.Width = 200; _showPwd.Height = 30;
        _password.Width = 270;
        _showPwd.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !_showPwd.Checked;
        Controls.Add(_showPwd);

        AddRow(UiText.Get("令牌:"), _token, 144);
        _token.Width = 270;
        _showToken.SetBounds(490, 148, 200, 30);
        _showToken.CheckedChanged += (_, _) => _token.UseSystemPasswordChar = !_showToken.Checked;
        Controls.Add(_showToken);
        AddRow(UiText.Get("备注:"), _note, 204);

        var hint = new Label
        {
            Text = UiText.Get("可填写登录令牌，或使用“获取令牌”按钮。"),
            Left = 210, Top = 250, Width = 480, Height = 48, ForeColor = Color.DimGray, AutoSize = false,
        };
        Controls.Add(hint);

        var ok = new Button { Text = UiText.Get("确定"), Left = 354, Top = 316, Width = 160, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = UiText.Get("取消"), Left = 530, Top = 316, Width = 160, DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) =>
        {
            var email = _email.Text.Trim();
            if (email.Length == 0)
            {
                MessageBox.Show(this, UiText.Get("邮箱/账号不能为空。"));
                DialogResult = DialogResult.None;
                return;
            }
            Result.Email = email;
            Result.Password = _password.Text;
            SessionToken = _token.Text.Trim();
            Result.Note = _note.Text.Trim();
        };
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;

        _email.Text = account.Email;
        _password.Text = account.Password;
        _showPwd.Checked = settings.ShowPasswordByDefault;
        _password.UseSystemPasswordChar = !settings.ShowPasswordByDefault;
        _showToken.Checked = settings.ShowTokenByDefault;
        _token.UseSystemPasswordChar = !settings.ShowTokenByDefault;
        try { _token.Text = TokenStore.Read(account.Email) ?? LauncherCreds.ReadSessionToken(account.Email) ?? ""; }
        catch { _token.Text = ""; }
        _email.TextChanged += (_, _) =>
        {
            if (!_email.Text.Trim().Equals(account.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                _token.Clear();
        };
        _note.Text = account.Note;
        UiTheme.Apply(this);
        UiTheme.StyleButton(ok, primary: true);
    }

    private void AddRow(string label, TextBox box, int top)
    {
        var lbl = new Label { Text = label, Left = 20, Top = top + 3, Width = 180, TextAlign = ContentAlignment.MiddleRight };
        box.Left = 210; box.Top = top; box.Width = 480;
        Controls.Add(lbl);
        Controls.Add(box);
    }
}
