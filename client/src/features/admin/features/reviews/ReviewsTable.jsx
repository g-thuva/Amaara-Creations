import { Link } from 'react-router-dom';
import { DataTable, StatusChip, Button } from '@/features/admin/shared/components/AdminUi.jsx';
import { publicBase } from '@/shared/utils/publicBase.js';
import { formatDateTime } from '@/features/admin/shared/utils/format.js';
export function ReviewsTable({ resource, pending, moderate, history, canViewCustomers }) {
  const storefront = publicBase(import.meta.env.VITE_PUBLIC_SITE_URL);
  return <DataTable caption="Customer reviews" rows={resource.data?.reviews || []} empty="No reviews match these filters" emptyDescription="Submitted customer reviews appear here. Try changing the search or moderation filters." columns={[
    { key: 'productName', label: 'Product', render: row => <div className="admin-review-identity">{storefront ? <a href={storefront + '/products/' + row.productId} target="_blank" rel="noreferrer">{row.productName}</a> : row.productName}</div> },
    { key: 'userName', label: 'Customer', render: row => <div className="admin-review-identity">{canViewCustomers ? <Link to={'/admin/customers/' + row.userId}>{row.userName}</Link> : row.userName}</div> },
    { key: 'rating', label: 'Rating', render: row => row.rating + ' / 5' },
    { key: 'comment', label: 'Review', render: row => <div className="admin-review-comment">{row.comment || 'No written comment.'}</div> },
    { key: 'verifiedPurchase', label: 'Purchase', render: row => row.verifiedPurchase ? 'Verified purchase' : 'Unverified' },
    { key: 'moderationStatus', label: 'State', render: row => <StatusChip status={row.moderationStatus} /> },
    { key: 'moderationReason', label: 'Last reason' },
    { key: 'createdAt', label: 'Submitted', render: row => formatDateTime(row.createdAt) },
    { key: 'actions', label: 'Actions', render: row => <div className="action-buttons admin-review-actions">
      {row.moderationStatus === 'Published' && <Button size="small" variant="secondary" disabled={pending} onClick={() => moderate(row, 'Hidden')}>Hide review</Button>}
      {['Hidden', 'Pending', 'Removed'].includes(row.moderationStatus) && <Button size="small" variant="secondary" disabled={pending} onClick={() => moderate(row, 'Published')}>Restore review</Button>}
      {!['Deleted', 'Removed'].includes(row.moderationStatus) && <Button size="small" variant="danger" disabled={pending} onClick={() => moderate(row, 'Removed')}>Remove review</Button>}
      <Button size="small" variant="ghost" onClick={() => history(row)}>View history</Button>
    </div> },
  ]} />;
}
