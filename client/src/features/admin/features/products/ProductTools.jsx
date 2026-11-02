import { AdminFormDialog } from "@/features/admin/shared/components/AdminFormDialog.jsx";
import { useProductTools } from "@/features/admin/features/products/useProductTools.js";
import { ProductMediaEditor } from "@/features/admin/features/products/ProductMediaEditor.jsx";
import { ProductVariantsEditor } from "@/features/admin/features/products/ProductVariantsEditor.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export function ProductTools({ product, changed, onClose }) {
  const tools = useProductTools(product, changed);
  const dirty = Boolean(tools.mediaFile || tools.mediaAltText || tools.variantForm.sku || tools.variantForm.name
    || tools.variantForm.priceOverride || Number(tools.variantForm.stockQuantity) || !tools.variantForm.isActive);
  return (
    <AdminFormDialog title={`${product.name}: Media and variants`} description="Manage existing media, cover images and product variants."
      width="wide" onClose={onClose} dirty={dirty} pending={tools.mutation.pending} error={tools.mutation.error} submitLabel={null}>
      {() => <>
        <MutationFeedback pending={tools.mutation.pending} success={tools.mutation.success} />
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
      </>}
    </AdminFormDialog>
  );
}
