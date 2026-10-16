namespace be.Domain.Identity;
public static class AppRoles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";
    public const string CatalogManager = "CatalogManager";
    public const string ProductionStaff = "ProductionStaff";
    public const string CustomerSupport = "CustomerSupport";
    public const string Marketing = "Marketing";
    public const string Analyst = "Analyst";
    public static IReadOnlyList<string> AdministrativeRoles { get; } = Array.AsReadOnly(new[] { Admin, SuperAdmin, CatalogManager, ProductionStaff, CustomerSupport, Marketing, Analyst });
}
