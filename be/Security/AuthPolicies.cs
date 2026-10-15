namespace be.Security
{
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

        public static readonly string[] AdministrativeRoles = { Admin, SuperAdmin, CatalogManager, ProductionStaff, CustomerSupport, Marketing, Analyst };
    }

    public static class AppPolicies
    {
        public const string AdminAccess = "AdminAccess";
        public const string ManageCustomers = "ManageCustomers";
        public const string ManageOrders = "ManageOrders";
        public const string ManageCatalog = "ManageCatalog";
        public const string ManageContent = "ManageContent";
        public const string ManageReviews = "ManageReviews";
        public const string ViewReports = "ViewReports";
        public const string ViewOrders = "ViewOrders";
        public const string ManageInventory = "ManageInventory";
        public const string ManagePayments = "ManagePayments";
        public const string RefundPayments = "RefundPayments";
        public const string ManageSettings = "ManageSettings";
        public const string ManageStaff = "ManageStaff";
        public const string ViewAudit = "ViewAudit";
        public const string ManagePromotions = "ManagePromotions";

        public static readonly IReadOnlyDictionary<string, string[]> Roles = new Dictionary<string, string[]>
        {
            [AdminAccess] = AppRoles.AdministrativeRoles,
            [ManageCustomers] = [AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.CustomerSupport],
            [ViewOrders] = [AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.ProductionStaff, AppRoles.CustomerSupport],
            [ManageOrders] = [AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.ProductionStaff],
            [ManageCatalog] = [AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.CatalogManager],
            [ManageContent] = [AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.Marketing],
            [ManageReviews] = [AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.CustomerSupport, AppRoles.Marketing],
            [ViewReports] = [AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.Analyst],
            [ManageInventory] = [AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.CatalogManager, AppRoles.ProductionStaff],
            [ManagePromotions] = [AppRoles.Admin, AppRoles.SuperAdmin, AppRoles.Marketing],
            [ManagePayments] = [AppRoles.Admin, AppRoles.SuperAdmin],
            [RefundPayments] = [AppRoles.Admin, AppRoles.SuperAdmin],
            [ManageSettings] = [AppRoles.SuperAdmin],
            [ManageStaff] = [AppRoles.SuperAdmin],
            [ViewAudit] = [AppRoles.Admin, AppRoles.SuperAdmin]
        };
    }
}
