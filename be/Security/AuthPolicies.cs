namespace be.Security
{
    public static class AppRoles
    {
        public const string Customer = "Customer";
        public const string Admin = "Admin";
        public const string SuperAdmin = "SuperAdmin";

        public static readonly string[] AdministrativeRoles = { Admin, SuperAdmin };
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
    }
}
