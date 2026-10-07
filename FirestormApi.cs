using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace logingui;

public sealed class ApiError : Exception
{
    public int ErrorId { get; }
    public bool IsBindingError { get; }
    public ApiError(int id, string msg, bool binding = false) : base(UiText.Get($"服务器返回错误 {id}: {msg}"))
    {
        ErrorId = id;
        IsBindingError = binding;
    }
}

public sealed class LoginOutcome
{
    public string SessionToken { get; set; } = "";
    public string Raw { get; set; } = "";
}

public sealed class TicketOutcome
{
    public string LoginTicket { get; set; } = "";
    public string PortalAddress { get; set; } = "";
    public string Raw { get; set; } = "";
}

public static class FirestormApi
{
    private const string Url = "https://api.firestorm-servers.com/cgi-bin/processACT";
    private static HttpClient Http = BuildClient(null);

    private static HttpClient BuildClient(string? proxyUrl)
    {
        var handler = new HttpClientHandler();
        if (!string.IsNullOrWhiteSpace(proxyUrl))
            handler.Proxy = new System.Net.WebProxy(proxyUrl) { BypassProxyOnLocal = true };
        else
            handler.UseProxy = false;

        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Firestorm/2.0");
        return client;
    }

    public static void SetProxy(string? proxyUrl)
    {
        var previous = Http;
        Http = BuildClient(string.IsNullOrWhiteSpace(proxyUrl) ? null : proxyUrl);
        previous.Dispose();
        AccountStore.Log("api proxy set to: " + (string.IsNullOrWhiteSpace(proxyUrl) ? "(none/direct)" : proxyUrl));
    }

    private static async Task<JsonElement> CallAsync(string method, object parameters)
    {
        var body = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = parameters,
            ["id"] = 1,
        });

        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var resp = await Http.PostAsync(Url, content).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        AccountStore.Log($"{method}: HTTP {(int)resp.StatusCode}, response length={text.Length}");
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var err))
        {
            if (err.ValueKind == JsonValueKind.Object && err.TryGetProperty("ErrorID", out var eid))
                throw new ApiError(eid.GetInt32(), GetString(err, "ErrorMsg"));

            var msg = err.ValueKind == JsonValueKind.String ? err.GetString() ?? "" : err.ToString();
            throw new ApiError(-1, msg, LooksLikeBinding(msg));
        }

        if (!root.TryGetProperty("result", out var result))
            throw new ApiError(-1, UiText.Get("返回内容缺少 result"));

        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("ErrorID", out var rid))
        {
            var id = rid.GetInt32();
            if (id != 0)
                throw new ApiError(id, GetString(result, "ErrorMsg"));
        }

        return result.Clone();
    }

    private static bool LooksLikeBinding(string msg)
        => msg.Contains("Expecting", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("Accessed JObject", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("Named parameter", StringComparison.OrdinalIgnoreCase)
        || msg.Contains("parameters", StringComparison.OrdinalIgnoreCase);

    /// <summary>Tries several parameter shapes until one is accepted by the server.</summary>
    private static async Task<JsonElement> CallAnyShapeAsync(string method, params object[] shapes)
    {
        ApiError? last = null;
        foreach (var shape in shapes)
        {
            try
            {
                return await CallAsync(method, shape).ConfigureAwait(false);
            }
            catch (ApiError e) when (e.IsBindingError)
            {
                last = e;
            }
        }
        throw last ?? new ApiError(-1, method + UiText.Get(" 调用失败"));
    }

    private static string GetString(JsonElement obj, string name)
        => obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    public static async Task<LoginOutcome> LoginAsync(string email, string password, string authCode, string turnstileToken)
    {
        // Confirmed parameter order and names:
        //   p_AccountName, p_AccountPassword, p_AuthenticatorCode, p_TurnstileToken
        var shapes = new object[]
        {
            new object?[] { email, password, authCode ?? "", turnstileToken },
            new Dictionary<string, object?>
            {
                ["p_AccountName"] = email,
                ["p_AccountPassword"] = password,
                ["p_AuthenticatorCode"] = authCode ?? "",
                ["p_TurnstileToken"] = turnstileToken,
            },
        };
        var result = await CallAnyShapeAsync("AuthentificateLauncher", shapes).ConfigureAwait(false);
        return new LoginOutcome { SessionToken = FindSessionToken(result), Raw = result.GetRawText() };
    }

    public static async Task<TicketOutcome> GetWowLoginTicketAsync(string sessionToken)
    {
        // The server's binder is quirky for this method: it needs the token in the
        // first two slots plus a trailing numeric. Verified shape: [token, token, 0].
        var shapes = new object[]
        {
            new object?[] { sessionToken, sessionToken, 0 },
            new object?[] { sessionToken, sessionToken, 1 },
            new Dictionary<string, object?> { ["p_Token"] = sessionToken, ["p_Token2"] = sessionToken },
        };
        var result = await CallAnyShapeAsync("GetWowLoginTicket", shapes).ConfigureAwait(false);
        return new TicketOutcome
        {
            LoginTicket = FindTicket(result),
            PortalAddress = FindPortal(result),
            Raw = result.GetRawText(),
        };
    }

    // ---- response field heuristics -------------------------------------------------

    private static string FindSessionToken(JsonElement el)
    {
        var byName = FindByKey(el, new[] { "session", "token", "sessionToken", "p_Token", "Session", "sessionId", "SessionId" });
        if (!string.IsNullOrEmpty(byName)) return byName;

        string? found = null;
        Walk(el, v =>
        {
            if (found == null && v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString()!;
                if (s.Length == 40 && s.All(Uri.IsHexDigit)) found = s;
            }
        });
        return found ?? "";
    }

    private static string FindTicket(JsonElement el)
    {
        var byName = FindByKey(el, new[] { "LoginTicket", "loginTicket", "login_ticket", "Ticket", "ticket" });
        if (!string.IsNullOrEmpty(byName)) return byName;

        string? found = null;
        Walk(el, v =>
        {
            if (found == null && v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString()!;
                if (s.StartsWith("FS-", StringComparison.Ordinal)) found = s;
            }
        });
        return found ?? "";
    }

    private static string FindPortal(JsonElement el)
    {
        var byName = FindByKey(el, new[] { "PortalAddress", "portalAddress", "portal", "ConnectionString", "connectionString" });
        if (!string.IsNullOrEmpty(byName)) return byName;

        string? found = null;
        Walk(el, v =>
        {
            if (found == null && v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString()!;
                var colon = s.LastIndexOf(':');
                if (colon > 0 && colon < s.Length - 1 && int.TryParse(s[(colon + 1)..], out _))
                    found = s;
            }
        });
        return found ?? "";
    }

    private static string FindByKey(JsonElement el, string[] keys)
    {
        string? res = null;
        WalkObject(el, (name, value) =>
        {
            if (res == null && value.ValueKind == JsonValueKind.String && keys.Contains(name, StringComparer.OrdinalIgnoreCase))
                res = value.GetString();
        });
        return res ?? "";
    }

    private static void Walk(JsonElement el, Action<JsonElement> visit)
    {
        visit(el);
        if (el.ValueKind == JsonValueKind.Object)
            foreach (var p in el.EnumerateObject()) Walk(p.Value, visit);
        else if (el.ValueKind == JsonValueKind.Array)
            foreach (var p in el.EnumerateArray()) Walk(p, visit);
    }

    private static void WalkObject(JsonElement el, Action<string, JsonElement> visit)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in el.EnumerateObject())
            {
                visit(p.Name, p.Value);
                WalkObject(p.Value, visit);
            }
        }
        else if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in el.EnumerateArray()) WalkObject(p, visit);
        }
    }
}
