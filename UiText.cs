namespace logingui;

internal static class UiText
{
#if UI_EN
    public static string Mode { get; private set; } = "en";
#else
    public static string Mode { get; private set; } = "zh";
#endif
    public static string Language => Mode == "en" ? "English" : "中文";

    public static void SetLanguage(string mode)
    {
        if (mode == "bilingual") mode = "zh"; // Migrate the removed language option.
        if (mode is not ("zh" or "en")) return;
        Mode = mode;
    }
    private static readonly Dictionary<string, string> Texts = new()
    {
        ["添加"] = "Add", ["编辑"] = "Edit", ["删除"] = "Delete",
        ["▲ 上移"] = "▲ Move Up", ["▼ 下移"] = "▼ Move Down",
        ["↑ 上移"] = "↑ Move Up", ["↓ 下移"] = "↓ Move Down",
        ["上移"] = "Move Up", ["下移"] = "Move Down",
        ["获取令牌"] = "Get Token", ["启动游戏"] = "Launch Game",
        ["导入账号"] = "Import Accounts", ["设置"] = "Settings",
        ["测试CF"] = "Test CF", ["日志"] = "Logs",
        ["账号"] = "Account", ["登录方式"] = "Login Method", ["令牌"] = "Token",
        ["备注"] = "Note", ["上次登录"] = "Last Login",
        ["有"] = "Yes", ["无"] = "No", ["读取失败"] = "Read Error",
        ["令牌登录"] = "Token Login", ["待获取令牌"] = "Token Required",
        ["令牌读取失败"] = "Token Read Error",
        ["添加账号"] = "Add Account", ["编辑账号"] = "Edit Account",
        ["邮箱/账号:"] = "Email / Account:", ["密码:"] = "Password:",
        ["令牌:"] = "Token:", ["备注:"] = "Note:",
        ["隐藏密码"] = "Hide Password", ["显示密码"] = "Show Password",
        ["检测令牌"] = "Check Token",
        ["正在检测令牌..."] = "Checking token...",
        ["令牌有效，可以用于启动游戏。"] = "The token is valid and can be used to launch the game.",
        ["没有令牌，请先获取或导入令牌。"] = "No token is available. Get or import a token first.",
        ["检测失败，尚未确认令牌是否有效。"] = "Token check failed. Token validity could not be confirmed.",
        ["显示令牌"] = "Show Token",
        ["默认显示密码"] = "Show Password by Default",
        ["默认显示令牌"] = "Show Token by Default",
        ["确定"] = "OK", ["取消"] = "Cancel",
        ["游戏程序:"] = "Game Program:", ["额外参数:"] = "Extra Arguments:",
        ["API 代理:"] = "API Proxy:", ["浏览..."] = "Browse...",
        ["选择 WoW 客户端"] = "Select WoW Client",
        ["可执行文件 (*.exe)|*.exe"] = "Executable files (*.exe)|*.exe",
        ["留空表示直连。修改代理后需重启程序。"] = "Leave blank for a direct connection. Restart after changing the proxy.",
        ["启动参数会自动附带 -launcherlogin。"] = "The -launcherlogin argument is added automatically.",
        ["可填写登录令牌，或使用“获取令牌”按钮。"] = "Enter a session token or use Get Token.",
        ["邮箱/账号不能为空。"] = "Email / account cannot be empty.",
        ["该账号已存在。"] = "This account already exists.",
        ["请先选择一个账号。"] = "Select an account first.",
        ["提示"] = "Information", ["确认"] = "Confirm", ["导入"] = "Import",
        ["导入完成"] = "Import Complete", ["导入失败"] = "Import Failed",
        ["请选择账号，使用令牌登录；没有令牌时先点击“获取令牌”。"] = "Select an account to launch. Use Get Token first if no token is available.",
        ["多账号管理 · 令牌登录"] = "Account Management · Token Login",
        ["账号工作区"] = "Account Workspace",
        ["活动日志"] = "Activity Log",
        ["Cloudflare 验证"] = "Cloudflare Verification",
        ["Cloudflare 验证码（自动获取，无需操作）:"] = "Cloudflare verification (automatic):",
        ["没有需要补充的令牌，已保存的令牌保持不变。"] = "No missing tokens to import. Saved tokens are preserved.",
        ["正在测试 Cloudflare 验证码..."] = "Testing Cloudflare verification...",
        ["验证码测试成功。"] = "Cloudflare verification succeeded.",
        ["测试成功"] = "Test Successful", ["测试失败"] = "Test Failed",
        ["令牌已保存。"] = "Token saved.",
        ["没有可导入的令牌，请先在“编辑”中填写密码，再点击“获取令牌”。"] = "No token is available to import. Enter a password in Edit, then use Get Token.",
        ["正在获取 Cloudflare 验证码..."] = "Obtaining Cloudflare verification...",
        ["正在使用密码获取令牌..."] = "Authenticating to obtain a token...",
        ["未能获取会话令牌，原有令牌已保留。"] = "No session token was returned. The existing token is preserved.",
        ["令牌已保存。可以点击“启动游戏”。"] = "Token saved. You can now launch the game.",
        ["获取令牌失败"] = "Token Acquisition Failed",
        ["该账号没有令牌。请先点击“获取令牌”，或导入启动器令牌。"] = "No token is available. Use Get Token or import launcher accounts first.",
        ["使用令牌免密登录；令牌已独立保存。"] = "Signing in with a token. The token is saved independently.",
        ["正在获取游戏登录票据..."] = "Obtaining a game login ticket...",
        ["未能解析到游戏登录票据。"] = "The game login ticket could not be read.",
        ["正在写入登录信息并启动游戏..."] = "Preparing login information and launching the game...",
        ["已启动游戏。"] = "Game launched.", ["完成，游戏已启动。"] = "Done. The game is running.",
        ["登录失败"] = "Login Failed", ["启动失败"] = "Startup Failed", ["操作失败"] = "Operation Failed",
        ["获取验证码超时"] = "Cloudflare verification timed out.",
        ["账号和令牌不能为空。"] = "Account and token cannot be empty.",
        ["令牌文件正在使用，请稍后重试。"] = "The token file is busy. Try again shortly.",
        ["令牌文件内容无效。"] = "The token file is invalid.",
        ["账号文件内容无效。"] = "The account file is invalid.",
        ["返回内容缺少 result"] = "The server response is missing result.",
        ["无法读取 Firestorm Launcher 账号文件，未执行导入。"] = "Unable to read Firestorm Launcher accounts. Import was not performed.",
        ["服务器返回 403（拒绝访问）。常见原因：该账号被限制/封禁，或短时间内登录尝试过多被临时限流。\n请稍后重试，或换网络/VPN 再试；也可先用官方启动器确认账号能正常登录。"] =
            "The server returned 403 (access denied). The account may be restricted or requests may be rate limited.\nTry again later or verify the account in Firestorm Launcher.",
    };

    private static readonly (string Chinese, string English)[] Fragments =
    {
        ("确定删除 ", "Delete account "), ("未找到官方启动器账号文件:", "Launcher account file not found:"),
        ("导入启动器账号失败: ", "Account import failed: "),
        ("验证码测试成功 (len=", "Verification succeeded (length="),
        ("验证码获取成功，长度 ", "Verification token length: "),
        ("验证码测试失败: ", "Verification failed: "), ("获取令牌失败: ", "Token acquisition failed: "),
        ("已获取游戏登录票据，门户: ", "Login ticket obtained. Portal: "),
        ("失败: ", "Failed: "), ("就绪。账号文件: ", "Ready. Account file: "),
        ("读取 ", "Read "), (" 个账号，新增 ", " accounts; added "), (" 个。", "."),
        ("令牌已保存：", "Tokens saved: "), (" 个。", "."),
        (" 令牌读取失败: ", " Token read failed: "),
        ("无法读取账号文件，已保留原文件，请检查: ", "Unable to read accounts; original file preserved: "),
        ("Turnstile 报错: ", "Turnstile error: "),
        ("服务器返回错误 ", "Server error "), (" 调用失败", " call failed"),
        ("找不到游戏可执行文件: ", "Game executable not found: "),
        ("无法创建注册表项: ", "Unable to create registry key: "),
    };

    public static string Get(string chinese)
    {
        if (Mode == "zh") return chinese;
        if (!Texts.TryGetValue(chinese, out var english))
        {
            english = chinese;
            foreach (var pair in Fragments) english = english.Replace(pair.Chinese, pair.English, StringComparison.Ordinal);
        }
        return english;
    }
}
