import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useResource } from '@/shared/hooks/useResource.js';
import { Dialog } from '@/shared/components/ui/Dialog.jsx';
import { ResourceState } from '@/features/admin/shared/components/ResourceState.jsx';
import { DataTable, Pagination } from '@/features/admin/shared/components/AdminUi.jsx';
import { AuditDecision } from '@/features/admin/shared/components/AuditDecision.jsx';
import { formatDateTime } from '@/features/admin/shared/utils/format.js';
import { reviewsApi } from './reviewsApi.js';
import { StatusChip } from '@/features/admin/shared/components/StatusChip.jsx';
import { publicBase } from '@/shared/utils/publicBase.js';
export function ReviewHistoryDialog({ review, canViewCustomers, onClose }) {
  const { id } = review;
  const storefront = publicBase(import.meta.env.VITE_PUBLIC_SITE_URL);
  const [page, setPage] = useState(1);
  const resource = useResource(signal => reviewsApi.history(id, page, signal), JSON.stringify([id, page]));
  return <Dialog open title={'Review ' + id + ' history'} description="Publication and moderation decisions from the audit trail." onClose={onClose} dismissOnBackdrop className="admin-form-dialog admin-form-dialog--wide">
    <div className="admin-form-dialog-body">
      <section className="admin-form-section" aria-label="Review details">
        <h2>Review details</h2>
        <p>{storefront ? <a href={storefront + '/products/' + review.productId} target="_blank" rel="noreferrer">{review.productName}</a> : review.productName} · {canViewCustomers ? <Link to={'/admin/customers/' + review.userId}>{review.userName}</Link> : review.userName}</p>
        <p>{review.rating} out of 5 · {review.verifiedPurchase ? 'Verified purchase' : 'Unverified purchase'} · <StatusChip status={review.moderationStatus} /></p>
        <p>Submitted {formatDateTime(review.createdAt)}</p>
        <p className="admin-review-comment">{review.comment || 'No written comment.'}</p>
        {review.moderationReason && <p>Last moderation reason: {review.moderationReason}</p>}
      </section>
      <ResourceState resource={resource}>
      <DataTable caption="Review history" rows={resource.data?.items || []} columns={[
        { key: 'createdAt', label: 'Date', render: row => formatDateTime(row.createdAt) }, { key: 'actorName', label: 'Operator' }, { key: 'action', label: 'Action' },
        { key: 'oldValuesJson', label: 'Previous state', render: row => <AuditDecision value={row.oldValuesJson} /> },
        { key: 'newValuesJson', label: 'Decision', render: row => <AuditDecision value={row.newValuesJson} /> },
      ]} />
      <Pagination data={resource.data} page={page} setPage={setPage} total={resource.data?.totalCount || 0} size={20} />
    </ResourceState></div>
  </Dialog>;
}
