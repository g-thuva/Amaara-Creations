namespace be.Application.Features.Checkout;
public sealed class CheckoutOptions
{
    public string Currency { get; set; } = "LKR";
    public int InventoryReservationMinutes { get; set; } = 35;
    public int ReconciliationSeconds { get; set; } = 30;
    public decimal TaxRate { get; set; }
    public bool PricesIncludeTax { get; set; }
    public bool TaxShipping { get; set; }
    public decimal PaymentFeeFixed { get; set; }
    public decimal PaymentFeePercentage { get; set; }
    public bool AllowDevelopmentShipping { get; set; }
}
