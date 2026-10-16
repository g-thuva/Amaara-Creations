namespace be.Application.Features.Cart
{
    public class CartResponse
    {
        public List<CartItemResponse> Items { get; set; } = new();
        public string? Currency { get; set; }
        public bool HasMixedCurrencies { get; set; }
        public bool CanCheckout { get; set; }
        public decimal Total { get; set; } // Sum of all item subtotals
        public int TotalItems { get; set; } // Total quantity of all items
    }
}
