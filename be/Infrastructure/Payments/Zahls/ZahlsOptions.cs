namespace be.Infrastructure.Payments.Zahls;
public sealed class ZahlsOptions
{
    public bool Enabled { get; set; }
    public string Mode { get; set; } = "TEST";
    // The public zahls.ch site currently illustrates v1.16. Keep this explicit until
    // an authenticated TEST merchant request confirms the full reference contract.
    public string ApiBaseUrl { get; set; } = "https://api.zahls.ch";
    public string ApiVersion { get; set; } = "v1.16";
    public string InstanceName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSigningKey { get; set; } = string.Empty;
    public string Currency { get; set; } = "CHF";
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    // Optional Gateway pm restriction. Empty means omit pm, never bypass readiness.
    // Nonempty identifiers must be verified against the merchant's enabled methods.
    public string[] PaymentMethods { get; set; } = [];
    public bool TestProviderConfigured { get; set; }
    public bool ChfSupportedByTestProvider { get; set; }
    public string LookAndFeelProfile { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
}
