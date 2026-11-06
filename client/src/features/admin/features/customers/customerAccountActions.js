import { can } from '@/app/config/permissions.js';

export const customerActions = {
  Active: {
    label: 'Reactivate customer',
    description: 'The customer can sign in again. They must start a new session; previous sessions stay revoked.',
    success: 'Customer reactivated. They can sign in again.',
    variant: 'primary',
  },
  Suspended: {
    label: 'Suspend customer',
    description: 'Temporarily stop account access until an administrator reactivates the customer. Existing sessions end immediately.',
    success: 'Customer suspended. Account access has been stopped.',
    variant: 'secondary',
  },
  Blocked: {
    label: 'Block customer',
    description: 'Block sign-in and authenticated customer actions until an administrator explicitly reactivates the account. Existing sessions end immediately.',
    success: 'Customer blocked. Account access has been stopped.',
    variant: 'danger',
  },
  Removed: {
    label: 'Remove customer',
    description: 'End all sessions, disable account access and remove this account from the normal customer list. Orders, payments, reviews and account history are retained. Find the account using the Removed filter if it needs to be restored.',
    acknowledgment: 'I understand this removes customer access and retains order, payment and account history.',
    success: 'Customer removed. Historical records are retained; find the account with the Removed filter.',
    variant: 'danger',
  },
  PendingDeletion: {
    label: 'Request account removal',
    description: 'Disable account access and open an owner-reviewed privacy request. Retention and outstanding orders must be reviewed before any separate data removal process.',
    acknowledgment: 'I understand this requests a retention review and preserves historical business records.',
    success: 'Account-removal review requested. Customer access has been stopped.',
    variant: 'danger',
  },
};

export function availableCustomerActions(customer, user, { includeReview = false } = {}) {
  if (!can(user, 'ManageCustomerAccess') || !customer.canManageAccess || customer.id === user?.id) return [];
  const owner = can(user, 'RequestCustomerRemoval');
  if (customer.accountStatus === 'PendingDeletion') return owner ? ['Active'] : [];
  if (customer.accountStatus === 'Removed') return includeReview && owner ? ['Active', 'PendingDeletion'] : ['Active'];
  if (!['Active', 'Suspended', 'Blocked'].includes(customer.accountStatus)) return [];
  return ['Active', 'Suspended', 'Blocked', 'Removed', ...(includeReview && owner ? ['PendingDeletion'] : [])]
    .filter(status => status !== customer.accountStatus);
}
