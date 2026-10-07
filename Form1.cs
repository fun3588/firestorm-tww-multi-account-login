using Microsoft.Web.WebView2.WinForms;

namespace logingui;

public partial class Form1 : Form
{
    private readonly List<Account> _accounts = AccountStore.Load();
    private AppSettings _settings = AccountStore.LoadSettings();

    private readonly ListView _list = new();
    private readonly TextBox _log = new();
    private readonly WebView2 _web = new();
    private readonly Label _status = new();
    private readonly Button _btnLogin = new();
    private readonly Button _btnGetToken = new();
    private readonly Button _btnCheckToken = new();
    private readonly Button _btnAdd = new();
    private readonly Button _btnEdit = new();
    private readonly Button _btnDelete = new();
    private readonly Button _btnMoveUp = new();
    private readonly Button _btnMoveDown = new();
    private readonly Button _btnSettings = new();
    private readonly Button _btnOpenLog = new();
    private readonly Button _btnTestCaptcha = new();
    private readonly Button _btnImport = new();
    private readonly Panel _turnstilePanel = new();

    private TurnstileService _turnstile = null!;
    private bool _busy;
    private readonly ComboBox _languagePicker = new();
    private readonly FlowLayoutPanel _toolbar = new();

    public Form1()
    {
        UiText.SetLanguage(_settings.UiLanguage);
        InitializeComponent();
        BuildUi();
        _turnstile = new TurnstileService(_web);
        RefreshList();
        AppendLog(UiText.Get("就绪。账号文件: ") + AccountStore.AccountsPath);
    }

    private void BuildUi()
    {
        SuspendLayout();
        UiTheme.Apply(this);
        Text = "Firestorm · " + UiText.Language + " · v1.2";
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
            Padding = new Padding(24), BackColor = UiTheme.Background,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Navy, Margin = new Padding(0, 0, 0, 16) };
        var title = new Label { Text = "FIRESTORM", Font = new Font("Segoe UI", 23f, FontStyle.Bold),
            ForeColor = Color.White, AutoSize = true, Location = new Point(22, 10) };
        var subtitle = new Label { Text = UiText.Get("多账号管理 · 令牌登录"), Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(148, 163, 184), AutoSize = true, Location = new Point(24, 52) };
        subtitle.Tag = "多账号管理 · 令牌登录";
        var languagePanel = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 260,
            Padding = new Padding(18, 12, 18, 0), BackColor = UiTheme.Navy };
        var version = new Label { Text = "v1.2", Width = 220, Height = 24,
            ForeColor = Color.FromArgb(147, 197, 253) };
        _languagePicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _languagePicker.Width = 220;
        _languagePicker.Items.AddRange(new object[] { "中文", "English" });
        _languagePicker.SelectedIndex = UiText.Mode == "en" ? 1 : 0;
        _languagePicker.SelectedIndexChanged += (_, _) => ChangeLanguage();
        languagePanel.Controls.Add(version);
        languagePanel.Controls.Add(_languagePicker);
        header.Controls.AddRange(new Control[] { title, subtitle, languagePanel });
        layout.Controls.Add(header, 0, 0);

        var toolbar = _toolbar;
        toolbar.Dock = DockStyle.Fill;
        toolbar.AutoSize = true;
        toolbar.WrapContents = true;
        toolbar.Margin = new Padding(0, 0, 0, 8);
        foreach (var (btn, text, handler) in new (Button, string, EventHandler)[]
        {
            (_btnAdd, "添加", (_, _) => OnAdd()), (_btnEdit, "编辑", (_, _) => OnEdit()),
            (_btnDelete, "删除", (_, _) => OnDelete()), (_btnGetToken, "获取令牌", OnGetToken),
            (_btnCheckToken, "检测令牌", OnCheckToken),
            (_btnLogin, "启动游戏", OnLogin), (_btnImport, "导入账号", (_, _) => OnImportLauncher()),
            (_btnSettings, "设置", (_, _) => OnSettings()), (_btnTestCaptcha, "测试CF", OnTestCaptcha),
            (_btnOpenLog, "日志", (_, _) => OpenLog()),
        })
        {
            btn.Text = UiText.Get(text);
            btn.Tag = text;
            btn.AutoSize = true;
            btn.MinimumSize = new Size(84, 40);
            btn.Padding = new Padding(10, 0, 10, 0);
            btn.Margin = new Padding(0, 0, 8, 8);
            btn.Click += handler;
            UiTheme.StyleButton(btn, btn == _btnLogin, btn == _btnDelete);
            toolbar.Controls.Add(btn);
        }
        UpdateToolbarLayout();
        layout.Controls.Add(toolbar, 0, 1);

        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.BackColor = Color.FromArgb(219, 234, 254);
        _status.ForeColor = Color.FromArgb(30, 64, 175);
        _status.Padding = new Padding(12, 0, 12, 0);
        _status.Margin = new Padding(0, 0, 0, 10);
        _status.Text = UiText.Get("请选择账号，使用令牌登录；没有令牌时先点击“获取令牌”。");
        _status.AutoEllipsis = true;
        layout.Controls.Add(_status, 0, 2);

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.Scrollable = true;
        _list.FullRowSelect = true;
        _list.MultiSelect = false;
        _list.HideSelection = false;
        _list.Columns.Add(UiText.Get("账号"), 275);
        _list.Columns.Add(UiText.Get("登录方式"), 190);
        _list.Columns.Add(UiText.Get("令牌"), 100);
        _list.Columns.Add(UiText.Get("备注"), 245);
        _list.Columns.Add(UiText.Get("上次登录"), 190);
        _list.DoubleClick += (_, _) => OnEdit();
        _list.KeyDown += (_, e) =>
        {
            if ((e.Alt || e.Control) && e.KeyCode == Keys.Up) { OnMoveUp(); e.Handled = true; }
            else if ((e.Alt || e.Control) && e.KeyCode == Keys.Down) { OnMoveDown(); e.Handled = true; }
        };
        var menu = new ContextMenuStrip();
        menu.Items.AddRange(new ToolStripItem[]
        {
            new ToolStripMenuItem(UiText.Get("启动游戏"), null, OnLogin) { Tag = "启动游戏" },
            new ToolStripSeparator(),
            new ToolStripMenuItem(UiText.Get("编辑"), null, (_, _) => OnEdit()) { Tag = "编辑" },
            new ToolStripMenuItem(UiText.Get("↑ 上移"), null, (_, _) => OnMoveUp()) { Tag = "↑ 上移" },
            new ToolStripMenuItem(UiText.Get("↓ 下移"), null, (_, _) => OnMoveDown()) { Tag = "↓ 下移" },
            new ToolStripSeparator(),
            new ToolStripMenuItem(UiText.Get("获取令牌"), null, OnGetToken) { Tag = "获取令牌" },
            new ToolStripMenuItem(UiText.Get("检测令牌"), null, OnCheckToken) { Tag = "检测令牌" },
            new ToolStripSeparator(),
            new ToolStripMenuItem(UiText.Get("删除"), null, (_, _) => OnDelete()) { Tag = "删除" },
        });
        _list.ContextMenuStrip = menu;
        _list.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = _list.HitTest(e.Location);
                if (hit.Item != null) hit.Item.Selected = true;
            }
        };
        UiTheme.StyleList(_list);
        _list.SmallImageList = new ImageList(components) { ImageSize = new Size(1, 34), ColorDepth = ColorDepth.Depth32Bit };
        _list.SizeChanged += (_, _) =>
        {
            var width = Math.Max(940, _list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth);
            double[] weights = { .26, .24, .14, .18, .18 };
            for (int i = 0; i < weights.Length; i++) _list.Columns[i].Width = (int)(width * weights[i]);
        };
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 14) };
        var accountHeader = new Panel { Dock = DockStyle.Top, Height = 40, Margin = new Padding(0, 0, 0, 8) };
        var caption = new Label { Tag = "账号工作区", Text = UiText.Get("账号工作区"), Dock = DockStyle.Left, AutoSize = false,
            Width = 160, ForeColor = UiTheme.Ink, Font = new Font("Segoe UI", 11f, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
        var movePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        foreach (var (btn, text, handler) in new (Button, string, EventHandler)[]
        {
            (_btnMoveUp, "↑ 上移", (_, _) => OnMoveUp()),
            (_btnMoveDown, "↓ 下移", (_, _) => OnMoveDown()),
        })
        {
            btn.Text = UiText.Get(text);
            btn.Tag = text;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.BackColor = Color.FromArgb(248, 250, 252);
            btn.ForeColor = Color.FromArgb(51, 65, 85);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 240);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(203, 213, 225);
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            btn.Size = new Size(86, 30);
            btn.Margin = new Padding(6, 5, 0, 5);
            btn.Click += handler;
            movePanel.Controls.Add(btn);
        }
        accountHeader.Controls.Add(caption);
        accountHeader.Controls.Add(movePanel);
        card.Controls.Add(_list);
        card.Controls.Add(accountHeader);
        layout.Controls.Add(card, 0, 3);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        var logCard = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Navy, Padding = new Padding(12),
            Margin = new Padding(0, 0, 14, 0) };
        var logTitle = new Label { Tag = "活动日志", Text = UiText.Get("活动日志"), Dock = DockStyle.Top, Height = 26,
            ForeColor = Color.FromArgb(148, 163, 184) };
        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.BorderStyle = BorderStyle.None;
        _log.Font = new Font("Consolas", 9f);
        _log.BackColor = UiTheme.Navy;
        _log.ForeColor = Color.FromArgb(203, 213, 225);
        logCard.Controls.Add(_log);
        logCard.Controls.Add(logTitle);
        bottom.Controls.Add(logCard, 0, 0);

        _turnstilePanel.Dock = DockStyle.Fill;
        _turnstilePanel.Padding = new Padding(12);
        _turnstilePanel.BackColor = Color.White;
        _turnstilePanel.Margin = Padding.Empty;
        var verificationTitle = new Label { Tag = "Cloudflare 验证", Text = UiText.Get("Cloudflare 验证"), Dock = DockStyle.Top,
            Height = 26, ForeColor = UiTheme.Muted };
        _web.Dock = DockStyle.Fill;
        _turnstilePanel.Controls.Add(_web);
        _turnstilePanel.Controls.Add(verificationTitle);
        bottom.Controls.Add(_turnstilePanel, 1, 0);
        layout.Controls.Add(bottom, 0, 4);
        Controls.Add(layout);
        ResumeLayout(true);
    }
    private void ChangeLanguage()
    {
        var mode = _languagePicker.SelectedIndex == 1 ? "en" : "zh";
        if (mode == UiText.Mode) return;
        UiText.SetLanguage(mode);
        _settings.UiLanguage = mode;
        AccountStore.SaveSettings(_settings);
        SuspendLayout();
        TranslateControls(this);
        if (_list.ContextMenuStrip != null)
        {
            foreach (ToolStripItem item in _list.ContextMenuStrip.Items)
            {
                if (item.Tag is string key) item.Text = UiText.Get(key);
            }
        }
        UpdateToolbarLayout();
        Text = "Firestorm · " + UiText.Language + " · v1.2";
        string[] columns = { "账号", "登录方式", "令牌", "备注", "上次登录" };
        for (int i = 0; i < columns.Length; i++) _list.Columns[i].Text = UiText.Get(columns[i]);
        _status.Text = UiText.Get("请选择账号，使用令牌登录；没有令牌时先点击“获取令牌”。");
        RefreshList();
        ResumeLayout(true);
        _list.Invalidate();
    }

    private void UpdateToolbarLayout()
    {
        _toolbar.SuspendLayout();
        foreach (Button button in _toolbar.Controls)
        {
            button.MinimumSize = new Size(68, 38);
            button.Padding = new Padding(6, 0, 6, 0);
            button.Margin = new Padding(0, 0, 4, 8);
            _toolbar.SetFlowBreak(button, false);
        }
        _toolbar.ResumeLayout(true);
    }

    private static void TranslateControls(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child.Tag is string key) child.Text = UiText.Get(key);
            TranslateControls(child);
        }
    }

    private void RefreshList() => RefreshListWithSelection(null);

    private void RefreshListWithSelection(string? selectEmail)
    {
        var selectedEmail = selectEmail ?? Selected()?.Email ?? _settings.LastSelectedEmail;
        var topEmail = _list.Items.Count > 0 ? (_list.TopItem?.Tag as Account)?.Email : null;
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var a in _accounts)
        {
            // Resolve once so both columns describe the same token snapshot.
            string mode, tokenText;
            try
            {
                var token = TokenStore.Read(a.Email);
                if (string.IsNullOrWhiteSpace(token)) token = LauncherCreds.ReadSessionToken(a.Email);
                bool hasToken = !string.IsNullOrWhiteSpace(token);
                mode = hasToken ? UiText.Get("令牌登录") : UiText.Get("待获取令牌");
                tokenText = hasToken ? UiText.Get("有") : UiText.Get("无");
            }
            catch (Exception ex)
            {
                mode = UiText.Get("令牌读取失败");
                tokenText = UiText.Get("读取失败");
                AppendLog(a.Email + UiText.Get(" 令牌读取失败: ") + ex.Message);
            }
            var item = new ListViewItem(new[] { a.Email, mode, tokenText, a.Note, a.LastUsed }) { Tag = a };
            _list.Items.Add(item);
        }
        _list.EndUpdate();

        if (_accounts.Count > 0)
        {
            var idx = _accounts.FindIndex(a => a.Email.Equals(selectedEmail, StringComparison.OrdinalIgnoreCase));
            var targetIndex = Math.Max(0, idx);
            _list.Items[targetIndex].Selected = true;
            _list.Items[targetIndex].Focused = true;
            _list.EnsureVisible(targetIndex);
            var topIndex = _accounts.FindIndex(a => a.Email.Equals(topEmail, StringComparison.OrdinalIgnoreCase));
            if (topIndex >= 0 && selectEmail == null) _list.TopItem = _list.Items[topIndex];
        }
    }

    private Account? Selected() => _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as Account : null;

    private void OnAdd()
    {
        using var dlg = new AccountDialog(new Account(), settings: _settings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            if (_accounts.Any(a => a.Email.Equals(dlg.Result.Email, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, UiText.Get("该账号已存在。"), UiText.Get("提示"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!string.IsNullOrWhiteSpace(dlg.SessionToken)) TokenStore.Save(dlg.Result.Email, dlg.SessionToken);
            _accounts.Add(dlg.Result);
            AccountStore.Save(_accounts);
            RefreshList();
        }
    }

    private void OnEdit()
    {
        var acc = Selected();
        if (acc == null) { MessageBox.Show(this, UiText.Get("请先选择一个账号。")); return; }
        using var dlg = new AccountDialog(acc, isEdit: true, settings: _settings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            if (!acc.Email.Trim().Equals(dlg.Result.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                if (_accounts.Any(a => a != acc && a.Email.Trim().Equals(dlg.Result.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(this, UiText.Get("该账号已存在。"));
                    return;
                }
                // Keep the old token bound to its original account.
            }
            if (!string.IsNullOrWhiteSpace(dlg.SessionToken)) TokenStore.Save(dlg.Result.Email, dlg.SessionToken);
            int i = _accounts.IndexOf(acc);
            _accounts[i] = dlg.Result;
            AccountStore.Save(_accounts);
            RefreshList();
        }
    }

    private void OnDelete()
    {
        var acc = Selected();
        if (acc == null) { MessageBox.Show(this, UiText.Get("请先选择一个账号。")); return; }
        try
        {
            var deleted = AccountDeletion.Delete(_accounts, acc, step => MessageBox.Show(this,
                step == 1 ? UiText.Get($"确定删除 {acc.Email} ?") : acc.Email + Environment.NewLine
                    + UiText.Get("再次确认：确定要删除该账号吗？令牌缓存将保留。"),
                UiText.Get("确认"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes);
            if (deleted) RefreshList();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, UiText.Get("操作失败")); }
    }

    private void OnMoveUp()
    {
        if (_busy) return;
        var acc = Selected();
        if (acc == null) return;
        var index = _accounts.IndexOf(acc);
        if (index <= 0) return;

        _accounts.RemoveAt(index);
        _accounts.Insert(index - 1, acc);
        AccountStore.Save(_accounts);
        RefreshListWithSelection(acc.Email);
    }

    private void OnMoveDown()
    {
        if (_busy) return;
        var acc = Selected();
        if (acc == null) return;
        var index = _accounts.IndexOf(acc);
        if (index < 0 || index >= _accounts.Count - 1) return;

        _accounts.RemoveAt(index);
        _accounts.Insert(index + 1, acc);
        AccountStore.Save(_accounts);
        RefreshListWithSelection(acc.Email);
    }

    private void OnImportLauncher()
    {
        try
        {
            if (!File.Exists(LauncherCreds.AuthJsonPath))
            {
                MessageBox.Show(this, UiText.Get("未找到官方启动器账号文件:\n") + LauncherCreds.AuthJsonPath,
                    UiText.Get("导入"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            LauncherCreds.ImportResult result;
            try { result = LauncherCreds.ImportInto(_accounts); }
            finally { AccountStore.Save(_accounts); }
            RefreshList();

            var message = UiText.Get($"读取 {result.Found} 个账号，新增 {result.Added} 个。\n") +
                (result.TokensSaved > 0 ? UiText.Get($"令牌已保存：{result.TokensSaved} 个。") : UiText.Get("没有需要补充的令牌，已保存的令牌保持不变。"));
            AppendLog(message);
            MessageBox.Show(this, message, UiText.Get("导入完成"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppendLog(UiText.Get("导入启动器账号失败: ") + ex.Message);
            MessageBox.Show(this, ex.Message, UiText.Get("导入失败"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OnSettings()
    {
        using var dlg = new SettingsDialog(_settings);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _settings = dlg.Result;
            AccountStore.SaveSettings(_settings);
            FirestormApi.SetProxy(_settings.HttpProxy);
        }
        var reloaded = AccountStore.Load();
        _accounts.Clear(); _accounts.AddRange(reloaded); RefreshList();
    }

    private void OpenLog()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AccountStore.DebugLogPath,
                UseShellExecute = true,
            });
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message); }
    }

    private async void OnTestCaptcha(object? sender, EventArgs e)
    {
        if (_busy) return;
        SetBusy(true);
        try
        {
            SetStatus(UiText.Get("正在测试 Cloudflare 验证码..."));
            var token = await _turnstile.GetTokenAsync(TimeSpan.FromSeconds(120));
            AppendLog(UiText.Get($"验证码测试成功 (len={token.Length})"));
            SetStatus(UiText.Get("验证码测试成功。"));
            MessageBox.Show(this, UiText.Get("验证码获取成功，长度 ") + token.Length, UiText.Get("测试成功"));
        }
        catch (Exception ex)
        {
            AppendLog(UiText.Get("验证码测试失败: ") + ex.Message);
            SetStatus(UiText.Get("验证码测试失败: ") + ex.Message);
            MessageBox.Show(this, ex.Message, UiText.Get("测试失败"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { SetBusy(false); }
    }

    private async void OnCheckToken(object? sender, EventArgs e)
    {
        if (_busy) return;
        var account = Selected();
        if (account == null) { MessageBox.Show(this, UiText.Get("请先选择一个账号。")); return; }
        SetBusy(true);
        try
        {
            SetStatus(UiText.Get("正在检测令牌..."));
            var token = TokenStore.Read(account.Email);
            if (string.IsNullOrWhiteSpace(token)) token = LauncherCreds.ReadSessionToken(account.Email);
            var result = await TokenValidation.CheckAsync(token);
            var message = result.State switch
            {
                TokenCheckState.Valid => UiText.Get("令牌有效，可以用于启动游戏。"),
                TokenCheckState.Missing => UiText.Get("没有令牌，请先获取或导入令牌。"),
                _ => UiText.Get("检测失败，尚未确认令牌是否有效。") + Environment.NewLine + result.Error,
            };
            SetStatus(message);
            MessageBox.Show(this, message, UiText.Get("检测令牌"), MessageBoxButtons.OK,
                result.State == TokenCheckState.Valid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            var message = UiText.Get("检测失败，尚未确认令牌是否有效。") + Environment.NewLine + ex.Message;
            SetStatus(message);
            MessageBox.Show(this, message, UiText.Get("检测令牌"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { SetBusy(false); }
    }

    private async void OnGetToken(object? sender, EventArgs e)
    {
        if (_busy) return;
        var acc = Selected();
        if (acc == null) { MessageBox.Show(this, UiText.Get("请先选择一个账号。")); return; }
        SetBusy(true);
        try
        {
            if (string.IsNullOrEmpty(acc.Password))
            {
                var token = TokenStore.GetOrImport(acc.Email);
                if (string.IsNullOrWhiteSpace(token))
                    throw new Exception(UiText.Get("没有可导入的令牌，请先在“编辑”中填写密码，再点击“获取令牌”。"));
                SetStatus(UiText.Get("令牌已保存。"));
            }
            else
            {
                SetStatus(UiText.Get("正在获取 Cloudflare 验证码..."));
                var turnstile = await _turnstile.GetTokenAsync(TimeSpan.FromSeconds(120));
                SetStatus(UiText.Get("正在使用密码获取令牌..."));
                var login = await FirestormApi.LoginAsync(acc.Email, acc.Password, "", turnstile);
                if (string.IsNullOrWhiteSpace(login.SessionToken))
                    throw new Exception(UiText.Get("未能获取会话令牌，原有令牌已保留。"));
                TokenStore.Save(acc.Email, login.SessionToken);
                SetStatus(UiText.Get("令牌已保存。可以点击“启动游戏”。"));
            }
            RefreshList();
            MessageBox.Show(this, UiText.Get("令牌已保存。"), UiText.Get("获取令牌"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus(UiText.Get("获取令牌失败: ") + ex.Message);
            MessageBox.Show(this, ex.Message, UiText.Get("获取令牌失败"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { SetBusy(false); }
    }
    private async void OnLogin(object? sender, EventArgs e)
    {
        if (_busy) return;
        var acc = Selected();
        if (acc == null) { MessageBox.Show(this, UiText.Get("请先选择一个账号。")); return; }
        SetBusy(true);
        try
        {
            var sessionToken = TokenStore.GetOrImport(acc.Email);
            if (string.IsNullOrWhiteSpace(sessionToken))
                throw new Exception(UiText.Get("该账号没有令牌。请先点击“获取令牌”，或导入启动器令牌。"));
            AppendLog(UiText.Get("使用令牌免密登录；令牌已独立保存。"));
            RefreshList();
            SetStatus(UiText.Get("正在获取游戏登录票据..."));
            var ticket = await FirestormApi.GetWowLoginTicketAsync(sessionToken);
            if (string.IsNullOrEmpty(ticket.LoginTicket))
                throw new Exception(UiText.Get("未能解析到游戏登录票据。"));

            var portal = ticket.PortalAddress;
            if (string.IsNullOrEmpty(portal))
                portal = WowLauncher.ReadCurrentConnectionString() ?? "eu.sl.logon.firestorm-servers.com:1119";
            AppendLog(UiText.Get($"已获取游戏登录票据，门户: {portal}"));

            SetStatus(UiText.Get("正在写入登录信息并启动游戏..."));

            var exe = _settings.WowExe;
            if (!File.Exists(exe)) exe = LauncherCreds.ResolveWowExe() ?? exe;
            using var process = await WowLauncher.LaunchWithTicketAsync(ticket.LoginTicket, portal, exe, _settings.ExtraArgs);

            acc.LastUsed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _settings.LastSelectedEmail = acc.Email;
            AccountStore.Save(_accounts);
            AccountStore.SaveSettings(_settings);
            RefreshList();

            AppendLog(UiText.Get("已启动游戏。"));
            SetStatus(UiText.Get("完成，游戏已启动。"));
        }
        catch (Exception ex)
        {
            var msg = ex is ApiError { ErrorId: 403 }
                ? UiText.Get("服务器返回 403（拒绝访问）。常见原因：该账号被限制/封禁，或短时间内登录尝试过多被临时限流。\n请稍后重试，或换网络/VPN 再试；也可先用官方启动器确认账号能正常登录。")
                : ex.Message;
            AppendLog(UiText.Get("失败: ") + ex.Message);
            SetStatus(UiText.Get("失败: ") + ex.Message);
            MessageBox.Show(this, msg, UiText.Get("登录失败"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _languagePicker.Enabled = !busy;
        _btnTestCaptcha.Enabled = !busy;
        _list.Enabled = !busy;
        _btnGetToken.Enabled = !busy;
        _btnCheckToken.Enabled = !busy;
        _btnLogin.Enabled = !busy;
        _btnAdd.Enabled = !busy;
        _btnEdit.Enabled = !busy;
        _btnDelete.Enabled = !busy;
        _btnMoveUp.Enabled = !busy;
        _btnMoveDown.Enabled = !busy;
        _btnSettings.Enabled = !busy;
        _btnImport.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired) { BeginInvoke(() => SetStatus(text)); return; }
        _status.Text = text;
        AppendLog(text);
    }

    private void AppendLog(string text)
    {
        if (InvokeRequired) { BeginInvoke(() => AppendLog(text)); return; }
        _log.AppendText(text + Environment.NewLine);
    }
}
