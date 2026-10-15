const all = ['Admin', 'SuperAdmin'];
export const permissionRoles = {
  AdminAccess: [...all, 'CatalogManager', 'ProductionStaff', 'CustomerSupport', 'Marketing', 'Analyst'],
  ManageCustomers: [...all, 'CustomerSupport'], ViewOrders: [...all, 'ProductionStaff', 'CustomerSupport'],
  ManageOrders: [...all, 'ProductionStaff'], ManageCatalog: [...all, 'CatalogManager'],
  ManageContent: [...all, 'Marketing'], ManageReviews: [...all, 'CustomerSupport', 'Marketing'],
  ManageInventory: [...all, 'CatalogManager', 'ProductionStaff'], ManagePromotions: [...all, 'Marketing'],
  ViewReports: [...all, 'Analyst'], ManagePayments: all, RefundPayments: all, ViewAudit: all,
  ManageSettings: ['SuperAdmin'], ManageStaff: ['SuperAdmin'],
};
export function can(user, permission) {
  const roles = user?.roles || (user?.role ? [user.role] : []);
  return (permissionRoles[permission] || []).some(role => roles.includes(role));
}
export const adminRoutes = {
  dashboard: 'ViewReports', products: 'ManageCatalog', categories: 'ManageCatalog', collections: 'ManageCatalog', media: 'ManageCatalog',
  content: 'ManageContent', settings: 'ManageSettings', reviews: 'ManageReviews', customers: 'ManageCustomers', orders: 'ViewOrders',
  shipping: 'ManageOrders', 'custom-builder': 'ManageCatalog', 'custom-designs': 'ManageOrders', inventory: 'ManageInventory',
  promotions: 'ManagePromotions', reports: 'ViewReports', audit: 'ViewAudit', staff: 'ManageStaff', payments: 'ManagePayments',
};
export function adminStart(user) {
  const preference = ['dashboard', 'customers', 'orders', 'inventory', 'products', 'content', 'reports', 'reviews'];
  return '/admin/' + (preference.find(route => can(user, adminRoutes[route])) || 'orders');
}
