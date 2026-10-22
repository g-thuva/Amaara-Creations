import { adminNavigation } from "@/features/admin/app/adminNavigation.js";
const all = ["Admin", "SuperAdmin"];
export const permissionRoles = {
  AdminAccess: [
    ...all,
    "CatalogManager",
    "ProductionStaff",
    "CustomerSupport",
    "Marketing",
    "Analyst",
  ],
  ManageCustomers: [...all, "CustomerSupport"],
  ViewOrders: [...all, "ProductionStaff", "CustomerSupport"],
  ManageOrders: [...all, "ProductionStaff"],
  ManageCatalog: [...all, "CatalogManager"],
  ManageContent: [...all, "Marketing"],
  ManageReviews: [...all, "CustomerSupport", "Marketing"],
  ManageInventory: [...all, "CatalogManager", "ProductionStaff"],
  ManagePromotions: [...all, "Marketing"],
  ViewReports: [...all, "Analyst"],
  ManagePayments: all,
  RefundPayments: all,
  ViewAudit: all,
  ManageSettings: ["SuperAdmin"],
  ManageStaff: ["SuperAdmin"],
};
export function can(user, permission) {
  if (Array.isArray(user?.permissions)) return user.permissions.includes(permission);
  const roles = user?.roles || (user?.role ? [user.role] : []);
  return (permissionRoles[permission] || []).some((role) =>
    roles.includes(role),
  );
}
export const adminRoutes = Object.fromEntries(
  adminNavigation.map((route) => [route.path.split("/")[0], route.permission]),
);
export function adminStart(user) {
  const preference = [
    "dashboard",
    "customers",
    "orders",
    "inventory",
    "products",
    "content",
    "reports",
    "reviews",
  ];
  return (
    "/admin/" +
    (preference.find((route) => can(user, adminRoutes[route])) || "orders")
  );
}
