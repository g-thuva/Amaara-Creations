using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Address
{
    public class AddressResponse
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string? DistrictOrProvince { get; set; }
        public string? PostalCode { get; set; }
        public string Country { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SaveAddressRequest
    {
        [Required]
        [StringLength(80)]
        public string Label { get; set; } = "Default";

        [Required]
        [StringLength(120)]
        public string RecipientName { get; set; } = string.Empty;

        [StringLength(40)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DistrictOrProvince { get; set; }

        [StringLength(30)]
        public string? PostalCode { get; set; }

        [Required]
        [StringLength(80)]
        public string Country { get; set; } = "Sri Lanka";

        public bool IsDefault { get; set; }
    }
}
