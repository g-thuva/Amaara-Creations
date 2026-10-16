import { useContent } from "@/features/admin/features/content/useContent.js";
import { ContentPagesTable } from "@/features/admin/features/content/ContentPagesTable.jsx";
import { ContentForm } from "@/features/admin/features/content/ContentForm.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
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
