import React, { useCallback, useEffect, useState } from "react";
import { adminApi } from "../../services/adminApi";
import { resolveMediaUrl } from "../../services/config";
import "./AdminStyles.css";

const emptyForm = {
  name: "",
  slug: "",
  shortDescription: "",
  description: "",
  basePrice: "",
  baseSku: "",
  categoryId: "",
  stock: "0",
  isActive: true,
  isFeatured: false,
  collectionIds: [],
};

const AdProducts = () => {
  const [products, setProducts] = useState([]);
  const [categories, setCategories] = useState([]);
  const [collections, setCollections] = useState([]);
  const [pageInfo, setPageInfo] = useState({ page: 1, pageSize: 20, totalPages: 1, totalItems: 0 });
  const [query, setQuery] = useState({ page: 1, pageSize: 20, search: "", isActive: "" });
  const [formData, setFormData] = useState(emptyForm);
  const [editingProduct, setEditingProduct] = useState(null);
  const [activeProduct, setActiveProduct] = useState(null);
  const [variants, setVariants] = useState([]);
  const [media, setMedia] = useState([]);
  const [variantForm, setVariantForm] = useState({ sku: "", name: "", priceOverride: "", stockQuantity: "0", isActive: true });
  const [mediaFile, setMediaFile] = useState(null);
  const [mediaAltText, setMediaAltText] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  const loadLookups = useCallback(async () => {
    const [categoryData, collectionData] = await Promise.all([
      adminApi.getCategories(),
      adminApi.getCollections(),
    ]);
    setCategories(categoryData);
    setCollections(collectionData);
  }, []);

  const loadProducts = useCallback(async () => {
    setIsLoading(true);
    setError("");
    try {
      const data = await adminApi.getProducts({
        ...query,
        isActive: query.isActive === "" ? undefined : query.isActive === "true",
      });
      setProducts(data.items || []);
      setPageInfo({
        page: data.page,
        pageSize: data.pageSize,
        totalPages: data.totalPages,
        totalItems: data.totalItems,
      });
    } catch (err) {
      setError(err.response?.data?.title || err.response?.data?.message || "Failed to load products");
    } finally {
      setIsLoading(false);
    }
  }, [query]);

  useEffect(() => {
    loadLookups().catch(() => setError("Failed to load catalogue lookups"));
  }, [loadLookups]);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  const resetForm = () => {
    setEditingProduct(null);
    setFormData(emptyForm);
  };

  const startEdit = (product) => {
    setEditingProduct(product);
    setFormData({
      name: product.name || "",
      slug: product.slug || "",
      shortDescription: product.shortDescription || "",
      description: product.description || "",
      basePrice: product.basePrice?.toString() || "",
      baseSku: product.baseSku || "",
      categoryId: product.categoryId?.toString() || "",
      stock: product.stock?.toString() || "0",
      isActive: product.isActive,
      isFeatured: product.isFeatured,
      collectionIds: product.collectionIds || [],
    });
  };

  const saveProduct = async (event) => {
    event.preventDefault();
    const payload = {
      ...formData,
      basePrice: Number(formData.basePrice),
      stock: Number(formData.stock),
      categoryId: formData.categoryId ? Number(formData.categoryId) : null,
      collectionIds: formData.collectionIds.map(Number),
    };

    if (editingProduct) {
      await adminApi.updateProduct(editingProduct.id, payload);
    } else {
      await adminApi.createProduct(payload);
    }
    resetForm();
    await loadProducts();
  };

  const toggleArchive = async (product) => {
    if (product.isActive) {
      await adminApi.archiveProduct(product.id);
    } else {
      await adminApi.reactivateProduct(product.id);
    }
    await loadProducts();
  };

  const openProductTools = async (product) => {
    setActiveProduct(product);
    const [variantData, mediaData] = await Promise.all([
      adminApi.getProductVariants(product.id),
      adminApi.getProductMedia(product.id),
    ]);
    setVariants(variantData);
    setMedia(mediaData);
  };

  const saveVariant = async (event) => {
    event.preventDefault();
    await adminApi.createProductVariant(activeProduct.id, {
      ...variantForm,
      priceOverride: variantForm.priceOverride === "" ? null : Number(variantForm.priceOverride),
      stockQuantity: Number(variantForm.stockQuantity),
    });
    setVariantForm({ sku: "", name: "", priceOverride: "", stockQuantity: "0", isActive: true });
    setVariants(await adminApi.getProductVariants(activeProduct.id));
    await loadProducts();
  };

  const uploadMedia = async (event) => {
    event.preventDefault();
    if (!mediaFile) return;
    await adminApi.uploadProductMedia(activeProduct.id, mediaFile, {
      altText: mediaAltText,
      isPrimary: media.length === 0,
      sortOrder: media.length,
    });
    setMediaFile(null);
    setMediaAltText("");
    setMedia(await adminApi.getProductMedia(activeProduct.id));
    await loadProducts();
  };

  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Product Management</h2>
            <p>{pageInfo.totalItems} products</p>
          </div>
          <button className="btn-add-product" onClick={resetForm}>
            <i className="fa-solid fa-plus" /> New Product
          </button>
        </div>
      </div>

      {error && <div className="out-of-stock-alert">{error}</div>}

      <div className="card">
        <div className="card-body">
          <div className="form-row">
            <input className="form-control" value={query.search} onChange={(e) => setQuery({ ...query, page: 1, search: e.target.value })} placeholder="Search name, slug, SKU" />
            <select className="form-control" value={query.isActive} onChange={(e) => setQuery({ ...query, page: 1, isActive: e.target.value })}>
              <option value="">All statuses</option>
              <option value="true">Active</option>
              <option value="false">Archived</option>
            </select>
          </div>
        </div>
      </div>

      <div className="products-table-container">
        <table className="admin-table">
          <thead>
            <tr>
              <th>Image</th>
              <th>Name</th>
              <th>Category</th>
              <th>Price</th>
              <th>Stock</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr><td colSpan="7">Loading products...</td></tr>
            ) : products.length === 0 ? (
              <tr><td colSpan="7">No products found.</td></tr>
            ) : products.map((product) => (
              <tr key={product.id}>
                <td><img className="product-thumbnail" src={resolveMediaUrl(product.imageUrl)} alt={product.name} /></td>
                <td>
                  <div className="product-name-cell">
                    <strong>{product.name}</strong>
                    <small>{product.baseSku || product.slug}</small>
                  </div>
                </td>
                <td>{product.categoryName || product.category}</td>
                <td>Rs. {Number(product.basePrice).toLocaleString()}</td>
                <td>{product.stock}</td>
                <td><span className={`badge ${product.isActive ? "badge-success" : "badge-warning"}`}>{product.isActive ? "Active" : "Archived"}</span></td>
                <td>
                  <div className="action-buttons">
                    <button className="btn-edit" title="Edit" onClick={() => startEdit(product)}><i className="fa-solid fa-pen" /></button>
                    <button className="btn-edit" title="Media and variants" onClick={() => openProductTools(product)}><i className="fa-solid fa-layer-group" /></button>
                    <button className="btn-delete" title={product.isActive ? "Archive" : "Reactivate"} onClick={() => toggleArchive(product)}><i className={`fa-solid ${product.isActive ? "fa-box-archive" : "fa-rotate-left"}`} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="form-actions">
        <button className="btn-cancel" disabled={pageInfo.page <= 1} onClick={() => setQuery({ ...query, page: query.page - 1 })}>Previous</button>
        <span>Page {pageInfo.page} of {pageInfo.totalPages || 1}</span>
        <button className="btn-submit" disabled={pageInfo.page >= pageInfo.totalPages} onClick={() => setQuery({ ...query, page: query.page + 1 })}>Next</button>
      </div>

      <div className="card">
        <div className="card-header"><h3 className="card-title">{editingProduct ? "Edit Product" : "Create Product"}</h3></div>
        <form className="add-product-form" onSubmit={saveProduct}>
          <div className="form-row">
            <label className="form-group">Name<input className="form-control" value={formData.name} required onChange={(e) => setFormData({ ...formData, name: e.target.value })} /></label>
            <label className="form-group">Slug<input className="form-control" value={formData.slug} onChange={(e) => setFormData({ ...formData, slug: e.target.value })} /></label>
          </div>
          <div className="form-row">
            <label className="form-group">Base price<input className="form-control" type="number" min="0" step="0.01" value={formData.basePrice} required onChange={(e) => setFormData({ ...formData, basePrice: e.target.value })} /></label>
            <label className="form-group">Stock<input className="form-control" type="number" min="0" value={formData.stock} required onChange={(e) => setFormData({ ...formData, stock: e.target.value })} /></label>
          </div>
          <div className="form-row">
            <label className="form-group">SKU<input className="form-control" value={formData.baseSku} onChange={(e) => setFormData({ ...formData, baseSku: e.target.value })} /></label>
            <label className="form-group">Category<select className="form-control" value={formData.categoryId} onChange={(e) => setFormData({ ...formData, categoryId: e.target.value })}>
              <option value="">None</option>
              {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
            </select></label>
          </div>
          <label className="form-group">Short description<input className="form-control" value={formData.shortDescription} onChange={(e) => setFormData({ ...formData, shortDescription: e.target.value })} /></label>
          <label className="form-group">Description<textarea className="form-control" required value={formData.description} onChange={(e) => setFormData({ ...formData, description: e.target.value })} /></label>
          <label className="form-group">Collections<select multiple className="form-control" value={formData.collectionIds.map(String)} onChange={(e) => setFormData({ ...formData, collectionIds: Array.from(e.target.selectedOptions).map((option) => option.value) })}>
            {collections.map((collection) => <option key={collection.id} value={collection.id}>{collection.name}</option>)}
          </select></label>
          <div className="form-row">
            <label><input type="checkbox" checked={formData.isActive} onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })} /> Active</label>
            <label><input type="checkbox" checked={formData.isFeatured} onChange={(e) => setFormData({ ...formData, isFeatured: e.target.checked })} /> Featured</label>
          </div>
          <div className="form-actions">
            <button type="button" className="btn-cancel" onClick={resetForm}>Clear</button>
            <button type="submit" className="btn-submit">{editingProduct ? "Save Product" : "Create Product"}</button>
          </div>
        </form>
      </div>

      {activeProduct && (
        <div className="card">
          <div className="card-header"><h3 className="card-title">{activeProduct.name}</h3></div>
          <div className="card-body">
            <form onSubmit={uploadMedia} className="form-row">
              <input className="form-control" type="file" accept="image/jpeg,image/png,image/webp" onChange={(e) => setMediaFile(e.target.files?.[0] || null)} />
              <input className="form-control" value={mediaAltText} onChange={(e) => setMediaAltText(e.target.value)} placeholder="Alt text" />
              <button className="btn btn-primary" type="submit">Upload</button>
            </form>
            <div className="table-responsive">
              <table>
                <tbody>{media.map((item) => (
                  <tr key={item.id}>
                    <td><img className="product-thumbnail" src={resolveMediaUrl(item.url)} alt={item.altText || activeProduct.name} /></td>
                    <td>{item.altText || item.originalFileName}</td>
                    <td>{item.width} x {item.height}</td>
                    <td>{item.isPrimary ? "Primary" : <button className="btn btn-sm btn-outline-primary" onClick={() => adminApi.updateProductMedia(activeProduct.id, item.id, { isPrimary: true }).then(() => openProductTools(activeProduct))}>Make primary</button>}</td>
                  </tr>
                ))}</tbody>
              </table>
            </div>
            <form onSubmit={saveVariant} className="form-row">
              <input className="form-control" value={variantForm.sku} required placeholder="Variant SKU" onChange={(e) => setVariantForm({ ...variantForm, sku: e.target.value })} />
              <input className="form-control" value={variantForm.name} required placeholder="Variant name" onChange={(e) => setVariantForm({ ...variantForm, name: e.target.value })} />
              <input className="form-control" type="number" min="0" value={variantForm.priceOverride} placeholder="Price override" onChange={(e) => setVariantForm({ ...variantForm, priceOverride: e.target.value })} />
              <input className="form-control" type="number" min="0" value={variantForm.stockQuantity} onChange={(e) => setVariantForm({ ...variantForm, stockQuantity: e.target.value })} />
              <button className="btn btn-primary" type="submit">Add Variant</button>
            </form>
            <table>
              <tbody>{variants.map((variant) => (
                <tr key={variant.id}><td>{variant.sku}</td><td>{variant.name}</td><td>{variant.stockQuantity}</td><td>{variant.isActive ? "Active" : "Archived"}</td></tr>
              ))}</tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdProducts;
