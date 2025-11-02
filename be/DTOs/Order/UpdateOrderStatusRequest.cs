using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Order
{
    public class UpdateOrderStatusRequest
    {
        [Required]
        [RegularExpression("^(Pending|Processing|Shipped|Delivered|Cancelled)$", ErrorMessage = "Status must be one of: Pending, Processing, Shipped, Delivered, Cancelled")]
        public string Status { get; set; } = string.Empty;
    }
}

