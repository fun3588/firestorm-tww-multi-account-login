<div align="center">

# 🎮 Firestorm TWW Multi-Account Login Tool
### Firestorm (The War Within 11.2.5) 多账号管理与一键免密/令牌登录器

[![Release](https://img.shields.io/github/v/release/fun3588/firestorm-tww-multi-account-login?color=2563eb&style=flat-square)](https://github.com/fun3588/firestorm-tww-multi-account-login/releases)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512bd4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078d4?style=flat-square&logo=windows)](https://microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-10b981?style=flat-square)](LICENSE)

[**简体中文**](#-简体中文) &nbsp; | &nbsp; [**English**](#-english) &nbsp; | &nbsp; [**快速下载 / Download**](https://github.com/fun3588/firestorm-tww-multi-account-login/releases)

</div>

---

## 📖 简体中文

### 🌟 项目简介

面向 **Firestorm（World of Warcraft: The War Within 11.2.5）** 的专业多账号管理与一键自动登录工具。
复刻官方 `FirestormLauncher` 完整登录与票据交换流程，帮助玩家摆脱频繁输入密码、手动验证码以及单账号切换的繁琐操作。

> 💡 **核心优势**：
> - **单文件绿色版**：体积仅约 1.35 MB，无需复杂安装。
> - **一键免密启动**：复用官方会话令牌（Token），跳过密码与 Cloudflare 验证码，秒级进游戏。
> - **中英双语一体**：界面支持中文与 English 无缝即时切换。
> - **本地数据隔离**：账号及令牌完全保存在本地，通过 Windows DPAPI 本机加密，绝不上云。

---

### ✨ 主要特性

| 功能 | 说明 |
| :--- | :--- |
| **多账号统一管理** | 支持添加、编辑、删除账号，自由上下调整账号顺序（`↑` / `↓`、右键菜单、`Ctrl/Alt+Up/Down` 快捷键）。 |
| **一键令牌登录** | 选中账号直接使用会话令牌获取游戏登录票据，自动写入注册表并启动客户端，免输入账号密码。 |
| **官方启动器一键导入** | 自动读取官方 `FirestormLauncher` 存储的账号列表及 Windows 凭据管理器中的有效 Token。 |
| **令牌有效性检测** | 提供独立“检测令牌”功能，实时检验令牌可用状态，无需启动游戏或发起登录尝试。 |
| **自动化 CLI 支持** | 提供 `--list-tokens`、`--token-login <email> [--launch]` 等命令行指令，便于编写多开脚本。 |
| **数据安全与隐私** | 敏感 Token 采用本机 Windows DPAPI（用户专属密钥）加密存储，`.gitignore` 严格忽略所有凭证。 |

---

### 🚀 快速使用

1. 前往 [Releases](https://github.com/fun3588/firestorm-tww-multi-account-login/releases) 下载最新版本的 `logingui.exe`。
2. 双击运行（环境要求：Windows 10/11，安装有 [.NET 8 桌面运行时](https://dotnet.microsoft.com/download/dotnet/8.0) 和 WebView2）。
3. **方式一（已有官方启动器）**：
   - 点击顶部 **「导入账号」**，程序将自动读取官方启动器账号及凭据。
   - 选中账号，点击 **「启动游戏」**（蓝色主按钮）即可一键进入游戏。
4. **方式二（手动添加账号）**：
   - 点击 **「添加」** 输入邮箱与密码；
   - 选中账号后点击 **「获取令牌」**（完成首次 Cloudflare 验证并保存会话令牌）；
   - 后续即可永久免密直接点击 **「启动游戏」**。

---

### 💻 命令行模式 (CLI)

程序本身为绿色单文件，同时完全支持终端交互与批量脚本调用：

```powershell
# 1. 列出当前本地及启动器所有已缓存的账号与令牌状态
.\logingui.exe --list-tokens

# 2. 为指定账号免密换取游戏登录票据（不启动游戏）
.\logingui.exe --token-login player@example.com

# 3. 免密取票并立即启动 WoW 客户端
.\logingui.exe --token-login player@example.com --launch

# 4. 批量多开示例脚本
foreach ($acc in @('user1@example.com', 'user2@example.com')) {
    Start-Process .\logingui.exe -ArgumentList '--token-login', $acc, '--launch'
    Start-Sleep -Seconds 20
}
```

---

### 🛠️ 本地编译与构建

```powershell
# 1. 克隆代码仓库
git clone https://github.com/fun3588/firestorm-tww-multi-account-login.git
cd firestorm-tww-multi-account-login

# 2. 执行自动化编译脚本（生成精简独立单文件）
powershell -ExecutionPolicy Bypass -File .\build-v1.ps1

# 3. 运行自动化测试套件
dotnet test tests/token-store/tests.csproj -c Release
dotnet run --project tests/ui-interactions/tests.csproj -c Release
```
构建产物输出位置：`bin/v1.2/logingui.exe`。

---

## 🌐 English

### 🌟 Overview

A modern multi-account manager and one-click token launcher tailored for **Firestorm (World of Warcraft: The War Within 11.2.5)**.
By fully reverse-engineering and replicating the official `FirestormLauncher` authentication flow, this tool enables instant login and game launch without re-entering credentials or solving Cloudflare CAPTCHAs.

> 💡 **Key Highlights**:
> - **Lightweight Single-File Binary**: ~1.35 MB standalone executable, zero clutter.
> - **Passwordless One-Click Launch**: Reuses stored session tokens to bypass passwords and CAPTCHAs.
> - **Live Bilingual UI**: Dynamically switch between English and 简体中文 with one click.
> - **Local Data Isolation**: Credentials and tokens stay on your local machine, secured via Windows DPAPI.

---

### ✨ Features

- **Account Management**: Add, edit, remove accounts, and reorder accounts with ease (`↑` / `↓` buttons, context menu, `Ctrl/Alt + Up/Down` shortcuts).
- **One-Click Token Launch**: Automatically fetches game tickets using saved tokens, writes them to the Windows registry, and launches `WoW 11.2.5 - Firestorm.exe -launcherlogin`.
- **Official Launcher Sync**: Import accounts from `%APPDATA%\com.firestorm.launcher\auth.json` and session tokens from Windows Credential Manager.
- **Token Health Check**: Verify token validity with a dedicated "Check Token" button without launching the game.
- **CLI Automation**: Full headless / CLI mode (`--list-tokens`, `--token-login <email> [--launch]`) for automated multi-boxing scripts.
- **Privacy & Security**: Tokens are encrypted using Windows DPAPI (bound to current OS user). Sensitive data files are strictly excluded from git.

---

### 🚀 Getting Started

1. Download the latest `logingui.exe` from [Releases](https://github.com/fun3588/firestorm-tww-multi-account-login/releases).
2. Run the executable (Requires Windows 10/11, [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0), and WebView2).
3. **If you already use FirestormLauncher**:
   - Click **"Import Accounts"** on the toolbar to import saved accounts and tokens.
   - Select an account and hit **"Launch Game"** (Blue button).
4. **If adding manually**:
   - Click **"Add"** to enter your account credentials.
   - Click **"Get Token"** once to fetch and store a session token.
   - From then on, simply click **"Launch Game"** to sign in instantly.

---

### 💻 Command Line Interface (CLI)

```powershell
# List available accounts and token statuses
.\logingui.exe --list-tokens

# Exchange session token for a WoW login ticket (headless)
.\logingui.exe --token-login player@example.com

# Exchange ticket and launch the game directly
.\logingui.exe --token-login player@example.com --launch
```

---

### 🛠️ Building from Source

```powershell
git clone https://github.com/fun3588/firestorm-tww-multi-account-login.git
cd firestorm-tww-multi-account-login

# Build single-file executable
powershell -ExecutionPolicy Bypass -File .\build-v1.ps1

# Run test suites
dotnet test tests/token-store/tests.csproj -c Release
dotnet run --project tests/ui-interactions/tests.csproj -c Release
```
Output executable is generated at `bin/v1.2/logingui.exe`.

---

## 🔒 Security & Privacy / 安全与隐私说明

- **明文凭据风险 / Plaintext Notice**: 本工具中的本地账号信息存储于本地 `data/accounts.json`，会话令牌采用 Windows DPAPI 加密存储于 `data/tokens.json`。
- **开源安全保证 / Git Safety**: 本仓库的 `.gitignore` 严格排除了所有 `data/*.json` 凭据文件，请勿将个人账号文件上传或提交至任何公共平台。
- **免责声明 / Disclaimer**: 本项目仅供技术研究与个人便利使用，请遵守游戏官方服务条款。
