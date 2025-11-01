namespace be.DTOs.Cart
{
    public class CartResponse
    {
        public List<CartItemResponse> Items { get; set; } = new();
        public decimal Total { get; set; } // Sum of all item subtotals
        public int TotalItems { get; set; } // Total quantity of all items
    }
}

