namespace logingui;

internal enum TokenCheckState { Valid, Missing, Failed }

internal sealed record TokenCheckResult(TokenCheckState State, string Error = "");

internal static class TokenValidation
{
    public static Task<TokenCheckResult> CheckAsync(string? token) =>
        CheckAsync(token, FirestormApi.GetWowLoginTicketAsync);

    internal static async Task<TokenCheckResult> CheckAsync(
        string? token, Func<string, Task<TicketOutcome>> getTicket)
    {
        if (string.IsNullOrWhiteSpace(token)) return new(TokenCheckState.Missing);
        try
        {
            var ticket = await getTicket(token).ConfigureAwait(false);
            return !string.IsNullOrWhiteSpace(ticket.LoginTicket)
                ? new(TokenCheckState.Valid)
                : new(TokenCheckState.Failed, UiText.Get("未能解析到游戏登录票据。"));
        }
        catch (Exception ex)
        {
            // Network, account restrictions and server failures do not prove token expiry.
            return new(TokenCheckState.Failed, ex.Message);
        }
    }
}
