import { useProductTools } from "./useProductTools.js";
import { ProductMediaEditor } from "./ProductMediaEditor.jsx";
import { ProductVariantsEditor } from "./ProductVariantsEditor.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { MutationFeedback } from "../../shared/components/MutationFeedback.jsx";
export function ProductTools({ product, changed }) {
  const tools = useProductTools(product, changed);
  return (
    <section className="card">
      <div className="card-header">
        <h3 className="card-title">{product.name}</h3>
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
