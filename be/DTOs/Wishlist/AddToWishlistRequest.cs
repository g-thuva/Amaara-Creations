using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Wishlist
{
    public class AddToWishlistRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Product ID must be valid")]
        public int ProductId { get; set; }
    }
}

