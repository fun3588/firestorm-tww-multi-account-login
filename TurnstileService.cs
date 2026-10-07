using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace logingui;

/// <summary>
/// Renders the Cloudflare Turnstile widget inside a hidden WebView2 and returns its token.
/// The widget's sitekey is the same one the official Firestorm launcher uses.
/// </summary>
public sealed class TurnstileService
{
    private const string SiteKey = "0x4AAAAAADNyMKanQHYt3Goj";
    private const string VirtualHost = "tauri.localhost";

    private readonly WebView2 _web;
    private readonly string _webDir;
    private TaskCompletionSource<string>? _tcs;
    private bool _initialized;

    public TurnstileService(WebView2 web)
    {
        _web = web;
        _webDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "logingui", "web");
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        Directory.CreateDirectory(_webDir);
        File.WriteAllText(Path.Combine(_webDir, "turnstile.html"), BuildHtml());

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "logingui", "webview");
        var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await _web.EnsureCoreWebView2Async(env);

        var core = _web.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.SetVirtualHostNameToFolderMapping(
            VirtualHost, _webDir, CoreWebView2HostResourceAccessKind.Allow);
        core.WebMessageReceived += OnWebMessage;

        _initialized = true;
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            string payload;
            try { payload = e.TryGetWebMessageAsString(); }
            catch { payload = e.WebMessageAsJson; }

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;
            if (!root.TryGetProperty("type", out var type)) return;

            if (type.GetString() == "token")
            {
                var token = root.GetProperty("token").GetString() ?? "";
                _tcs?.TrySetResult(token);
            }
            else if (type.GetString() == "error")
            {
                var code = root.TryGetProperty("code", out var c) ? c.GetString() : "?";
                _tcs?.TrySetException(new Exception(UiText.Get("Turnstile 报错: ") + code));
            }
        }
        catch { }
    }

    /// <summary>Fetches a fresh Turnstile token (loads the page again so it is single-use safe).</summary>
    public async Task<string> GetTokenAsync(TimeSpan timeout)
    {
        await InitializeAsync();
        _tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Reload to force a brand new challenge/token every time.
        _web.CoreWebView2.Navigate($"https://{VirtualHost}/turnstile.html");

        var completed = await Task.WhenAny(_tcs.Task, Task.Delay(timeout));
        if (completed != _tcs.Task)
            throw new TimeoutException(UiText.Get("获取验证码超时"));
        return await _tcs.Task;
    }

    internal static string BuildHtml() => $$"""
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<script src="https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit" async defer></script>
<style>body{background:#0e0e10;margin:0;padding:8px}</style>
</head>
<body>
<div id="w"></div>
<script>
window.__token = null;
function render(){
  if(!window.turnstile){ setTimeout(render, 150); return; }
  try {
    turnstile.render('#w', {
      sitekey: '{{SiteKey}}',
      theme: 'dark',
      callback: function(t){ window.__token = t;
        window.chrome.webview.postMessage(JSON.stringify({type:'token', token:t})); },
      'error-callback': function(e){
        window.chrome.webview.postMessage(JSON.stringify({type:'error', code:String(e)})); }
    });
  } catch(e){
    window.chrome.webview.postMessage(JSON.stringify({type:'error', code:String(e)}));
  }
}
window.onload = render;
</script>
</body>
</html>
""";
}
