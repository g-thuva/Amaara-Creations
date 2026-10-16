import { usePromotions } from "@/features/admin/features/promotions/usePromotions.js";
import { blank } from "@/features/admin/features/promotions/promotionForm.js";
import { PromotionForm } from "@/features/admin/features/promotions/PromotionForm.jsx";
import { PromotionsTable } from "@/features/admin/features/promotions/PromotionsTable.jsx";
import { PromotionRedemptionsTable } from "@/features/admin/features/promotions/PromotionRedemptionsTable.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export default function PromotionsPage() {
  const promotions = usePromotions();
  const { resource, history, editing, setEditing, setHistoryId, mutation } =
    promotions;
  return (
    <div className="admin-page">
      <h1>Promotions</h1>
      <button className="btn btn-primary" onClick={() => setEditing(blank)}>
        Create promotion
      </button>
      <MutationFeedback {...mutation} />
      {editing && (
        <PromotionForm
          editing={editing}
          setEditing={setEditing}
          submit={promotions.submit}
          pending={mutation.pending}
        />
      )}
      <ResourceState resource={resource}>
        <PromotionsTable
          resource={resource}
          setEditing={setEditing}
          setHistoryId={setHistoryId}
        />
        <Pagination
          data={resource.data}
          page={promotions.page}
          setPage={promotions.setPage}
          total={resource.data?.totalCount || 0}
          size={30}
        />
      </ResourceState>
      {promotions.historyId && (
        <ResourceState resource={history}>
          <PromotionRedemptionsTable history={history} />
        </ResourceState>
      )}
    </div>
  );
}
