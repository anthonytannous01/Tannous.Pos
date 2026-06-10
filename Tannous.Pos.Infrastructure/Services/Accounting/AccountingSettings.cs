namespace Tannous.Pos.Infrastructure.Services.Accounting;

public class AccountingSettings
{
    public const string Section = "Accounting";

    public QuickBooksSettings QuickBooks { get; set; } = new();
    public XeroSettings       Xero       { get; set; } = new();
    /// <summary>The full base URL of this API (e.g. "https://api.tannouspos.com") — used to build OAuth callback URLs.</summary>
    public string BaseUrl { get; set; } = "http://localhost:7000";
}

public class QuickBooksSettings
{
    public string ClientId     { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Sandbox      { get; set; } = "true";
}

public class XeroSettings
{
    public string ClientId     { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
