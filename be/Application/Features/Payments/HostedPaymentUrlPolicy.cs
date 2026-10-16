using be.Domain.Common;

namespace be.Application.Features.Payments;
public static class HostedPaymentUrlPolicy
{
    public static string Validate(string url, string provider)
    {
        if (string.IsNullOrWhiteSpace(url) || url != url.Trim() || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.Port != 443 || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment) || uri.HostNameType != UriHostNameType.Dns)
            throw Invalid();
        var host = uri.IdnHost;
        var allowed = provider switch
        {
            "Zahls" => host.Equals("zahls.ch", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".zahls.ch", StringComparison.OrdinalIgnoreCase),
            "Datatrans" => host.Equals("pay.sandbox.datatrans.com", StringComparison.OrdinalIgnoreCase),
            // Test adapters still require a secure absolute URL. Production registrations
            // are explicitly restricted above.
            _ => true
        };
        if (!allowed)
            throw Invalid();
        return uri.AbsoluteUri;
    }

    private static CommerceException Invalid() => new("PAYMENT_REDIRECT_INVALID", "The secure payment destination could not be verified.", 502);
}
