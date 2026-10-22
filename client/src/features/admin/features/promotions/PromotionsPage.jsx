import { usePromotions } from "@/features/admin/features/promotions/usePromotions.js";
import { blank } from "@/features/admin/features/promotions/promotionForm.js";
import { PromotionForm } from "@/features/admin/features/promotions/PromotionForm.jsx";
import { PromotionsTable } from "@/features/admin/features/promotions/PromotionsTable.jsx";
import { PromotionRedemptionsTable } from "@/features/admin/features/promotions/PromotionRedemptionsTable.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
import { PageHeader, Button } from '@/features/admin/shared/components/AdminUi.jsx';
export default function PromotionsPage() {
  const promotions = usePromotions();
  const { resource, history, editing, setEditing, setHistoryId, mutation } =
    promotions;
  return (
    <div className="admin-page">
      <PageHeader title="Promotions" description="Create and manage offer rules, limits and committed redemptions." actions={<Button variant="accent" onClick={() => setEditing(blank)}>Create promotion</Button>} />
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
          create={() => setEditing(blank)}
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
