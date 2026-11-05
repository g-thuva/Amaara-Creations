export const privilegedRoles = ['Admin', 'SuperAdmin', 'CatalogManager', 'ProductionStaff', 'CustomerSupport', 'Marketing', 'Analyst'];

export function acceptsIdentity(user, management = false) {
  const roles = Array.isArray(user?.roles) ? user.roles : user?.role ? [user.role] : [];
  if (!user?.id || roles.some(role => role !== 'Customer' && !privilegedRoles.includes(role))) return false;
  const privileged = roles.some(role => privilegedRoles.includes(role));
  return management ? privileged : !privileged && roles.includes('Customer');
}
