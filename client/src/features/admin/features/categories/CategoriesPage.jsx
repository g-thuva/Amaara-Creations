import { useCategories } from "@/features/admin/features/categories/useCategories.js";
import { CategoriesTable } from "@/features/admin/features/categories/CategoriesTable.jsx";
import { CategoryForm } from "@/features/admin/features/categories/CategoryForm.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
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
