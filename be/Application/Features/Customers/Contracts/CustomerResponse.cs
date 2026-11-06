namespace be.Application.Features.Customers
{
    public class CustomerResponse
    {
        public string AccountStatus { get; set; } = "Active";
        public string Version { get; set; } = string.Empty;
        public bool CanManageAccess { get; set; }
        public int OrderCount { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
