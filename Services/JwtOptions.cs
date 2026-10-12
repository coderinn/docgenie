namespace LeadsPortal.Services;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "DocGenie";
    public string Audience { get; set; } = "DocGenie.Client";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
}
