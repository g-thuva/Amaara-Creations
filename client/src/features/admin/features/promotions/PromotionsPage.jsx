import { AdminFormDialog } from "@/features/admin/shared/components/AdminFormDialog.jsx";
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
      <PageHeader title="Promotions" description="Create and manage offer rules, limits and committed redemptions." actions={<Button variant="accent" onClick={() => { mutation.reset(); setEditing(blank); }}>Create promotion</Button>} />
      <MutationFeedback {...mutation} />
      {editing && <AdminFormDialog title={editing.id ? 'Edit Promotion' : 'New Promotion'} description="Set offer rules, dates and redemption limits."
        onClose={() => setEditing(null)} pending={mutation.pending} error={mutation.error} submitLabel={editing.id ? 'Save Changes' : 'Create Promotion'}>
        {formId => <PromotionForm formId={formId} editing={editing} submit={promotions.submit} pending={mutation.pending} />}
      </AdminFormDialog>}
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
