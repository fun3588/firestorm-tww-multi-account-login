using logingui;
using System.Text.Json;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
if (args.Contains("language"))
{
    var launch = UiText.Get("启动游戏");
    var note = UiText.Get("备注");
#if UI_EN
    Check(launch == "Launch Game" && note == "Note", "English labels");
#else
    Check(launch == "启动游戏" && note == "备注", "Chinese labels");
#endif
    var defaultMode = UiText.Mode;
    UiText.SetLanguage("en");
    Check(UiText.Get("显示令牌") == "Show Token", "runtime English");
    UiText.SetLanguage("zh");
    Check(UiText.Get("启动游戏") == "启动游戏", "runtime Chinese");
    UiText.SetLanguage("bilingual");
    Check(UiText.Mode == "zh" && UiText.Get("启动游戏") == "启动游戏", "removed language migrates to Chinese");
    UiText.SetLanguage(defaultMode);
    Console.WriteLine("PASS: " + UiText.Language + " translations and runtime switching");
    return;
}
if (args.Contains("reload"))
{
    Check(TokenStore.Read("EXISTING@example.test") == "existing-token", "restart read");
    Check(TokenStore.Read("new@example.test") == "new-token", "restart new read");
    Console.WriteLine("PASS: separate process reload");
    return;
}
Directory.CreateDirectory(Path.GetDirectoryName(AccountStore.AccountsPath)!);
File.Delete(TokenStore.TokensPath);
var accounts = new List<Account> { new() { Email = "existing@example.test", Password = "password", Note = "keep" } };
var source = new[] {
    new LauncherCreds.LauncherAccount { Email = "EXISTING@example.test" },
    new LauncherCreds.LauncherAccount { Email = "new@example.test" },
    new LauncherCreds.LauncherAccount { Email = "NEW@example.test" }
};
var result = LauncherCreds.MergeAccounts(accounts, source, null, TokenStore.Read,
    email => email.StartsWith("existing", StringComparison.OrdinalIgnoreCase) ? "existing-token" : "new-token", TokenStore.Save);
Check(result.Added == 1 && result.TokensSaved == 2 && accounts.Count == 2, "existing account backfill and duplicate merge");
Check(accounts[0].Password == "password" && accounts[0].Note == "keep", "preserve account fields");
result = LauncherCreds.MergeAccounts(accounts, source, null, TokenStore.Read, _ => throw new Exception("should not read external"), TokenStore.Save);
Check(result.Added == 0 && result.TokensSaved == 0, "repeat import preserves local");
result = LauncherCreds.MergeAccounts(accounts, Array.Empty<LauncherCreds.LauncherAccount>(), null, TokenStore.Read, _ => null, TokenStore.Save);
Check(accounts.Count == 2 && TokenStore.Read(accounts[0].Email) == "existing-token", "external accounts removed");
TokenStore.Save("password@example.test", "password-login-token");
Check(TokenStore.GetOrImport("password@example.test") == "password-login-token", "local token priority");
var beforeCheck = File.ReadAllText(TokenStore.TokensPath);
var valid = await TokenValidation.CheckAsync("test-token", _ => Task.FromResult(new TicketOutcome { LoginTicket = "FS-test-ticket" }));
Check(valid.State == TokenCheckState.Valid, "valid ticket");
var missing = await TokenValidation.CheckAsync(" ", _ => throw new Exception("should not call server"));
Check(missing.State == TokenCheckState.Missing, "missing token does not call server");
var empty = await TokenValidation.CheckAsync("test-token", _ => Task.FromResult(new TicketOutcome()));
Check(empty.State == TokenCheckState.Failed, "empty ticket is inconclusive");
var network = await TokenValidation.CheckAsync("test-token", _ => Task.FromException<TicketOutcome>(new System.Net.Http.HttpRequestException("network unavailable")));
Check(network.State == TokenCheckState.Failed, "network failure does not imply expiry");
var denied = await TokenValidation.CheckAsync("test-token", _ => Task.FromException<TicketOutcome>(new ApiError(403, "access denied")));
Check(denied.State == TokenCheckState.Failed, "403 does not imply expiry");
Check(File.ReadAllText(TokenStore.TokensPath) == beforeCheck, "validation preserves token cache");
Console.WriteLine("PASS: token validation success, missing token, malformed result, network failure, server rejection, cache preservation");
var raw = File.ReadAllText(TokenStore.TokensPath);
Check(!raw.Contains("existing-token") && !raw.Contains("password-login-token"), "encrypted storage");
var original = File.ReadAllText(TokenStore.TokensPath);
File.WriteAllText(TokenStore.TokensPath, "{broken");
Check(!TokenStore.Has("existing@example.test"), "corrupt file status does not crash");
bool refused = false;
try { TokenStore.Save("another@example.test", "another-token"); }
catch (JsonException) { refused = true; }
Check(refused && File.ReadAllText(TokenStore.TokensPath) == "{broken", "corrupt file is not overwritten");
File.WriteAllText(TokenStore.TokensPath, original);
Parallel.For(0, 20, i => TokenStore.Save($"parallel{i}@example.test", $"token-{i}"));
for (int i = 0; i < 20; i++) Check(TokenStore.Read($"parallel{i}@example.test") == $"token-{i}", "concurrent saves preserve all accounts");
Console.WriteLine("PASS: corruption handling and concurrent saves");
Console.WriteLine("PASS: backfill, duplicates, repeat import, external removal, local priority, encrypted persistence");
namespace logingui
{
    public static class AccountStore
    {
        public static string AccountsPath => Path.Combine(AppContext.BaseDirectory, "isolated", "accounts.json");
        public static void Log(string message) { }
    }
}
