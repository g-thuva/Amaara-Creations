namespace be.Domain.Common;
public static class Money
{
    public const string DefaultCurrency = "CHF";

    // Explicitly supported ISO currency exponents; unsupported currencies fail closed.
    public static int Exponent(string currency) => currency switch
    {
        "LKR" or "CHF" or "EUR" or "USD" or "GBP" => 2,
        "JPY" => 0,
        "KWD" or "BHD" => 3,
        _ => throw new CommerceException("CURRENCY_UNSUPPORTED", "This currency is not configured.")};
    public static decimal Round(decimal amount, string currency) => decimal.Round(amount, Exponent(currency), MidpointRounding.AwayFromZero);
    public static long ToMinor(decimal amount, string currency)
    {
        if (amount < 0)
            throw new CommerceException("AMOUNT_INVALID", "Amount cannot be negative.");
        var factor = Factor(currency);
        return checked((long)(Round(amount, currency) * factor));
    }

    public static decimal FromMinor(long amount, string currency) => amount / Factor(currency);
    private static decimal Factor(string currency) => Exponent(currency) switch
    {
        0 => 1m,
        2 => 100m,
        3 => 1000m,
        _ => throw new InvalidOperationException()};
}
