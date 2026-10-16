import { useCollections } from "./useCollections.js";
import { CollectionsTable } from "./CollectionsTable.jsx";
import { CollectionForm } from "./CollectionForm.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { MutationFeedback } from "../../shared/components/MutationFeedback.jsx";
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
