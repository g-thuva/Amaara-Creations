import { useReviews } from "@/features/admin/features/reviews/useReviews.js";
import { ReviewsTable } from "@/features/admin/features/reviews/ReviewsTable.jsx";
import { ConfirmDialog } from "@/features/admin/shared/components/ConfirmDialog.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
import { PageHeader, FilterBar } from '@/features/admin/shared/components/AdminUi.jsx';
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export default function ReviewsPage() {
  const reviews = useReviews();
  const { resource, mutation, intent, setIntent, setReason } = reviews;
  const review = (next, reason) => {
    setIntent(next);
    setReason(reason);
  };
  return (
    <div className="admin-page">
      <PageHeader title="Review moderation" description="Review customer feedback and record a reason for each moderation decision." />
      <p>
        Verified purchase comes from eligible fulfilled order history. New and
        edited reviews require moderation before publication.
      </p>
      <FilterBar><label>
        Moderation state
        <select
          className="form-control"
          value={reviews.status}
          onChange={(e) => {
            reviews.setStatus(e.target.value);
            reviews.setPage(1);
          }}
        >
          <option value="">All</option>
          {["Pending", "Published", "Hidden", "Deleted"].map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
      </label></FilterBar>
      <MutationFeedback {...mutation} />
      <ResourceState resource={resource}>
        <ReviewsTable
          resource={resource}
          pending={mutation.pending}
          review={review}
        />
        <Pagination
          data={resource.data}
          page={reviews.page}
          setPage={reviews.setPage}
          total={resource.data?.totalCount || 0}
          size={20}
        />
      </ResourceState>
      <ConfirmDialog
        open={Boolean(intent)}
        onClose={() => setIntent(null)}
        title="Confirm review moderation"
        pending={mutation.pending}
        onConfirm={reviews.confirm}
      >
        {intent?.status}: {reviews.reason}
      </ConfirmDialog>
    </div>
  );
}
