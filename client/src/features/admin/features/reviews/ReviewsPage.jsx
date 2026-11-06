import { useState } from 'react';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { can } from '@/app/config/permissions.js';
import { useReviews } from './useReviews.js';
import { ReviewsTable } from './ReviewsTable.jsx';
import { ReviewModerationForm } from './ReviewModerationForm.jsx';
import { ReviewHistoryDialog } from './ReviewHistoryDialog.jsx';
import { AdminFormDialog } from '@/features/admin/shared/components/AdminFormDialog.jsx';
import { ResourceState } from '@/features/admin/shared/components/ResourceState.jsx';
import { Pagination } from '@/features/admin/shared/components/Pagination.jsx';
import { PageHeader, FilterBar } from '@/features/admin/shared/components/AdminUi.jsx';
import { MutationFeedback } from '@/features/admin/shared/components/MutationFeedback.jsx';
export default function ReviewsPage() {
  const reviews = useReviews();
  const { user } = useAuth();
  const [history, setHistory] = useState(null);
  const { resource, mutation, intent, setIntent } = reviews;
  const action = intent?.status === 'Hidden' ? 'Hide review' : intent?.status === 'Removed' ? 'Remove review' : 'Restore review';
  return <div className="admin-page">
    <PageHeader title="Review moderation" description="Customer reviews publish directly. Hide, restore or remove feedback with a recorded reason." />
    <p>Verified purchase is calculated from fulfilled, paid order history. Only published reviews contribute to public ratings.</p>
    <FilterBar>
      <label>Search reviews<input type="search" className="form-control" value={reviews.search} onChange={e => reviews.setFilters({ search: e.target.value })} /></label>
      <label>Moderation state<select className="form-control" value={reviews.status} onChange={e => reviews.setFilters({ status: e.target.value })}><option value="">All</option>{['Published', 'Hidden', 'Removed', 'Pending', 'Deleted'].map(status => <option key={status}>{status}</option>)}</select></label>
      <label>Rating<select className="form-control" value={reviews.rating} onChange={e => reviews.setFilters({ rating: e.target.value })}><option value="">All ratings</option>{[5, 4, 3, 2, 1].map(rating => <option key={rating} value={rating}>{rating} out of 5</option>)}</select></label>
      <label>Product ID<input type="number" min="1" step="1" className="form-control" value={reviews.productId} onChange={e => reviews.setFilters({ productId: e.target.value })} /></label>
    </FilterBar>
    <MutationFeedback {...mutation} />
    <ResourceState resource={resource}>
      <ReviewsTable resource={resource} pending={mutation.pending || resource.loading || Boolean(resource.error)} moderate={reviews.moderate} history={setHistory} canViewCustomers={can(user, 'ManageCustomers')} />
      <Pagination data={resource.data} page={reviews.page} setPage={reviews.setPage} total={resource.data?.totalCount || 0} size={20} />
    </ResourceState>
    {intent && <AdminFormDialog title={action} description="Confirm this moderation decision and explain the reason." onClose={() => setIntent(null)} dirty={Boolean(reviews.reason)} pending={mutation.pending} error={mutation.error} submitLabel={action} submitVariant={intent.status === 'Removed' ? 'danger' : 'primary'} submitDisabled={!reviews.reason.trim() || resource.loading || Boolean(resource.error)} width="compact">
      {formId => <ReviewModerationForm {...reviews} formId={formId} pending={mutation.pending} />}
    </AdminFormDialog>}
    {history != null && <ReviewHistoryDialog review={history} canViewCustomers={can(user, 'ManageCustomers')} onClose={() => setHistory(null)} />}
  </div>;
}
