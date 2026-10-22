import { useProductTools } from "@/features/admin/features/products/useProductTools.js";
import { ProductMediaEditor } from "@/features/admin/features/products/ProductMediaEditor.jsx";
import { ProductVariantsEditor } from "@/features/admin/features/products/ProductVariantsEditor.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export function ProductTools({ product, changed }) {
  const tools = useProductTools(product, changed);
  return (
    <section className="card">
      <div className="card-header">
        <h2 className="card-title">{product.name}</h2>
      </div>
      <div className="card-body">
        <MutationFeedback {...tools.mutation} />
        <ResourceState resource={tools.resource}>
          <ProductMediaEditor
            {...tools}
            activeProduct={product}
            media={tools.resource.data?.media || []}
            pending={tools.mutation.pending}
          />
          <ProductVariantsEditor
            {...tools}
            variants={tools.resource.data?.variants || []}
            pending={tools.mutation.pending}
          />
        </ResourceState>
      </div>
    </section>
  );
}
