import AppIcon from '@/shared/components/ui/Icon.jsx';
const groups = {
  success: ['Paid', 'Confirmed', 'Completed', 'Delivered', 'Published', 'Approved', 'Active', 'Ready', 'Consumed', 'Verified', 'Responded'],
  warning: ['Pending', 'PendingPayment', 'PendingReview', 'AwaitingProof', 'AwaitingApproval', 'AwaitingCustomerApproval', 'AwaitingCustomer', 'PaymentReview', 'PartiallyRefunded', 'Reserved', 'InReview', 'Requested', 'Queued', 'Preparing', 'ChangesRequested', 'ReadyForPickup'],
  danger: ['Failed', 'Cancelled', 'Rejected', 'Declined', 'Expired', 'Overdue', 'Deleted', 'Locked'],
  info: ['Authorized', 'Processing', 'Printing', 'Finishing', 'QualityCheck', 'Shipped', 'InTransit', 'Refunded', 'Submitted', 'ProofUploaded', 'Ordered', 'Packed'],
  neutral: ['Draft', 'Disabled', 'Closed', 'Hidden', 'Archived', 'Released', 'NotStarted', 'NotRequired'],
};
const icons = { success: 'check', warning: 'pause', danger: 'x', info: 'refresh', neutral: 'minus' };
export function StatusChip({ status, tone, className = '' }) {
  const value = String(status ?? 'Unspecified');
  const selected = Object.hasOwn(groups, tone) ? tone : Object.keys(groups).find(key => groups[key].includes(value)) || 'neutral';
  return <span className={`badge status-chip badge-${selected} ${className}`.trim()}><AppIcon name={icons[selected]} /><span>{value}</span></span>;
}
