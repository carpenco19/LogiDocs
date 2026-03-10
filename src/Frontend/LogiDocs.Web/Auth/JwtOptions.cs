namespace LogiDocs.Web.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "LogiDocs";
    public string Audience { get; set; } = "LogiDocs.Api";
    public string Key { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 120;
}