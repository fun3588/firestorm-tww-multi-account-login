using System.Text.Json.Serialization;

namespace logingui;

public sealed class Account
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string AuthCode { get; set; } = "";
    public string Note { get; set; } = "";
    public string LastUsed { get; set; } = "";

    [JsonIgnore]
    public string Display => string.IsNullOrWhiteSpace(Note) ? Email : $"{Email}  ({Note})";
}
