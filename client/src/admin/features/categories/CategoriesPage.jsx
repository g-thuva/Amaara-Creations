import { useCategories } from "./useCategories.js";
import { CategoriesTable } from "./CategoriesTable.jsx";
import { CategoryForm } from "./CategoryForm.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { MutationFeedback } from "../../shared/components/MutationFeedback.jsx";
export default function CategoriesPage() {
  const feature = useCategories();
  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Categories</h2>
            <p>{feature.items.length} categories</p>
          </div>
        </div>
      </div>
      <MutationFeedback {...feature.mutation} />
      <ResourceState resource={feature.resource}>
        <CategoriesTable {...feature} />
      </ResourceState>
      <CategoryForm {...feature} pending={feature.mutation.pending} />
    </div>
  );
}
