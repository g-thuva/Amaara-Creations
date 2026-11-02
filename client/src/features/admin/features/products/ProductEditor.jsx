import { useResource } from '@/shared/hooks/useResource.js';
import { AdminFormDialog } from '@/features/admin/shared/components/AdminFormDialog.jsx';
import { ResourceState } from '@/features/admin/shared/components/ResourceState.jsx';
import { categoriesApi } from '@/features/admin/features/categories/categoriesApi.js';
import { collectionsApi } from '@/features/admin/features/collections/collectionsApi.js';
import { productsApi } from './productsApi.js';
import { ProductForm } from './ProductForm.jsx';
import { useProductForm } from './useProductForm.js';

export function ProductEditor({ product, onClose, onSaved }) {
  const lookups = useResource(async signal => {
    const [categories, collections] = await Promise.all([categoriesApi.getCategories(signal), collectionsApi.getCollections(signal)]);
    return { categories, collections };
  }, 'product-lookups');
  const media = useResource(signal => productsApi.getProductMedia(product.id, signal), String(product?.id), Boolean(product));
  const form = useProductForm(product, onSaved, media.data?.length || 0);
  const loading = lookups.loading || (Boolean(product) && media.loading);
  const unavailable = lookups.error || media.error;
  return <AdminFormDialog title={product ? 'Edit Product' : 'New Product'}
    description={product ? 'Update product details, availability and media.' : 'Add a new product to your store catalogue.'}
    width="wide" onClose={() => { if (form.savedProduct) onSaved(form.savedProduct); else onClose(); }} dirty={form.dirty} pending={form.mutation.pending}
    submitDisabled={loading || Boolean(unavailable)} error={form.mutation.error}
    submitLabel={form.savedProduct ? 'Retry media upload' : product ? 'Save Changes' : 'Create Product'}>
    {formId => <>
      <ResourceState resource={lookups} />
      {product && <ResourceState resource={media} />}
      {form.savedProduct && form.pendingMediaFiles.length > 0 && <p className="admin-callout" role="status">Product details are saved. Retry the remaining uploads without creating another product.</p>}
      <ProductForm {...form} formId={formId} editingProduct={product} pending={form.mutation.pending} readOnly={Boolean(form.savedProduct)}
        categories={lookups.data?.categories || []} collections={lookups.data?.collections || []} existingMedia={media.data || []} />
    </>}
  </AdminFormDialog>;
}
