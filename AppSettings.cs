namespace logingui;

public sealed class AppSettings
{
    public string WowExe { get; set; } = @"C:\Firestorm\The War Within\WoW 11.2.5 - Firestorm.exe";
    public string ExtraArgs { get; set; } = "";
    public string HttpProxy { get; set; } = "http://127.0.0.1:2080";
    public bool ShowPasswordByDefault { get; set; } = true;
    public bool ShowTokenByDefault { get; set; } = true;
    public string UiLanguage { get; set; } = "";
    public string LastSelectedEmail { get; set; } = "";
}
