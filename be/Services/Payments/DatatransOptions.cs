namespace be.Services.Payments;

public sealed class DatatransOptions
{
    public bool Enabled { get; set; }
    public string Environment { get; set; } = "Sandbox";
    public string ApiVersion { get; set; } = "v1";
    public string MerchantId { get; set; } = string.Empty;
    public string ApiPassword { get; set; } = string.Empty;
    public string HmacKey { get; set; } = string.Empty;
    public bool AutoSettle { get; set; } = true;
    // Only populate after verifying currency support in the sandbox merchant account.
    public string[] VerifiedCurrencies { get; set; } = [];
    public string[] PaymentMethods { get; set; } = [];
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
    public int SignatureToleranceSeconds { get; set; } = 300;
    public Dictionary<string, string> PhoneCallingCodes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sri Lanka"] = "94", ["LK"] = "94", ["Switzerland"] = "41", ["CH"] = "41"
    };
}
