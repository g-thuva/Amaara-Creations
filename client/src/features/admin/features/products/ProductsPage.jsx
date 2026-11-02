import { useState } from "react";
import AppIcon from "@/shared/components/ui/Icon.jsx";
import { useProducts } from "@/features/admin/features/products/useProducts.js";
import { ProductEditor } from "./ProductEditor.jsx";
import { useToast } from "@/features/admin/shared/components/Toast.jsx";
import { ProductsTable } from "@/features/admin/features/products/ProductsTable.jsx";
import { ProductTools } from "@/features/admin/features/products/ProductTools.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
export default function ProductsPage() {
  const products = useProducts();
  const { query, setQuery, resource, mutation } = products;
  const [editor, setEditor] = useState(null);
  const [savedProduct, setSavedProduct] = useState(null);
  const notify = useToast();
  const saved = product => {
    setEditor(null); setSavedProduct(product); resource.reload();
    notify?.(`${product.name} ${editor?.product ? 'updated' : 'created'}.`);
  };
  const [activeProduct, setActiveProduct] = useState(null);
  return (
    <div className="admin-page admin-products-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h1>Product Management</h1>
            <p>{resource.data ? `${resource.data.totalCount ?? resource.data.totalItems ?? resource.data.items?.length ?? 0} products` : 'Manage products, prices and availability.'}</p>
          </div>
          <button className="btn-add-product" onClick={() => setEditor({ product: null })}>
            <AppIcon name="plus" /> New Product
          </button>
        </div>
      </div>
      <MutationFeedback {...mutation} />
      {savedProduct && <p className="admin-callout" role="status">{savedProduct.name} saved. Your catalogue filters have been kept.{' '}
        <button type="button" className="btn btn-outline-primary" onClick={() => setQuery({ ...query, page: 1, search: savedProduct.name, isActive: '' })}>Find saved product</button>
      </p>}
      <div className="card card-body form-row">
        <input
          aria-label="Search products"
          className="form-control"
          value={query.search}
          onChange={(e) =>
            setQuery({ ...query, page: 1, search: e.target.value })
          }
          placeholder="Search name, slug, SKU"
        />
        <select
          aria-label="Product status"
          className="form-control"
          value={query.isActive}
          onChange={(e) =>
            setQuery({ ...query, page: 1, isActive: e.target.value })
          }
        >
          <option value="">All statuses</option>
          <option value="true">Active</option>
          <option value="false">Archived</option>
        </select>
      </div>
      <ResourceState resource={resource}>
        <ProductsTable
          products={resource.data?.items || []}
          isLoading={resource.loading}
          startEdit={product => setEditor({ product })}
          openProductTools={setActiveProduct}
          toggleArchive={products.toggleArchive}
          pending={mutation.pending}
        />
        <Pagination
          page={query.page}
          setPage={(page) => setQuery({ ...query, page })}
          data={resource.data}
          size={20}
        />
      </ResourceState>
      {editor && <ProductEditor product={editor.product} onClose={() => setEditor(null)} onSaved={saved} />}
      {activeProduct && (
        <ProductTools
          key={activeProduct.id}
          product={activeProduct}
          changed={resource.reload}
          onClose={() => setActiveProduct(null)}
        />
      )}
    </div>
  );
}
