using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Order
{
    public class CreateOrderRequest
    {
        [StringLength(500)]
        public string? ShippingAddress { get; set; }

        [StringLength(100)]
        public string? ShippingCity { get; set; }

        [StringLength(50)]
        public string? ShippingPostalCode { get; set; }

        [StringLength(100)]
        public string? ShippingCountry { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}

