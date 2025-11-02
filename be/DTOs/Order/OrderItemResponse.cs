namespace be.DTOs.Order
{
    public class OrderItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; } // Price at time of order
        public decimal Subtotal { get; set; } // Price * Quantity
    }
}

