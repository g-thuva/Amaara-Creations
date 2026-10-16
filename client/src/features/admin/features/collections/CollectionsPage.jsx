import { useCollections } from "@/features/admin/features/collections/useCollections.js";
import { CollectionsTable } from "@/features/admin/features/collections/CollectionsTable.jsx";
import { CollectionForm } from "@/features/admin/features/collections/CollectionForm.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export default function CollectionsPage() {
  const feature = useCollections();
  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Collections</h2>
            <p>{feature.items.length} collections</p>
          </div>
        </div>
      </div>
      <MutationFeedback {...feature.mutation} />
      <ResourceState resource={feature.resource}>
        <CollectionsTable {...feature} />
      </ResourceState>
      <CollectionForm {...feature} pending={feature.mutation.pending} />
    </div>
  );
}
