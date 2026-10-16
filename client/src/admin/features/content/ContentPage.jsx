import { useContent } from "./useContent.js";
import { ContentPagesTable } from "./ContentPagesTable.jsx";
import { ContentForm } from "./ContentForm.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { MutationFeedback } from "../../shared/components/MutationFeedback.jsx";
export default function ContentPage() {
  const feature = useContent();
  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Content</h2>
            <p>{feature.pages.length} pages</p>
          </div>
        </div>
      </div>
      <MutationFeedback {...feature.mutation} />
      <ResourceState resource={feature.resource}>
        <ContentPagesTable {...feature} />
      </ResourceState>
      <ContentForm {...feature} pending={feature.mutation.pending} />
    </div>
  );
}
