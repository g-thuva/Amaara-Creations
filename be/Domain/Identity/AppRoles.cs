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

    // A shared credential page accepts either account type. Session scope is
    // chosen from current server roles after all required factors pass.
    public static bool CanBeginSignIn(IEnumerable<string> roles, bool managementEntry = false)
    {
        var memberships = roles.ToArray();
        return CanSignIn(memberships, true) || (!managementEntry && CanSignIn(memberships, false));
    }

    public static bool CanSignIn(IEnumerable<string> roles, bool management)
    {
        var memberships = roles.ToArray();
        if (memberships.Any(role => role != Customer && !AdministrativeRoles.Contains(role))) return false;
        var privileged = memberships.Any(AdministrativeRoles.Contains);
        return management ? privileged : !privileged && memberships.Contains(Customer);
    }
}
