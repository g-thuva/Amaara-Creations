import { Link } from "react-router-dom";
export function ProductForm({
  editingProduct,
  formData,
  setFormData,
  categories,
  collections,
  saveProduct,
  resetForm,
  pending,
}) {
  return (
    <div className="card">
      <div className="card-header">
        <h2 className="card-title">
          {editingProduct ? "Edit Product" : "Create Product"}
        </h2>
      </div>
      <form className="add-product-form" onSubmit={saveProduct}>
        <div className="form-row">
          <label className="form-group">
            Name
            <input
              className="form-control"
              value={formData.name}
              required
              onChange={(e) =>
                setFormData({ ...formData, name: e.target.value })
              }
            />
          </label>
          <label className="form-group">
            Slug
            <input
              className="form-control"
              value={formData.slug}
              onChange={(e) =>
                setFormData({ ...formData, slug: e.target.value })
              }
            />
          </label>
        </div>
        <div className="form-row">
          <label className="form-group">
            Currency
            <select
              className="form-control"
              value={formData.currency}
              disabled={Boolean(editingProduct)}
              required
              onChange={(e) =>
                setFormData({ ...formData, currency: e.target.value })
              }
            >
              <option value="">Choose currency</option>
              {["LKR", "CHF", "USD", "EUR", "GBP"].map((c) => (
                <option key={c}>{c}</option>
              ))}
            </select>
          </label>
          <label className="form-group">
            Base price
            <input
              className="form-control"
              type="number"
              min="0"
              step="0.01"
              value={formData.basePrice}
              required
              onChange={(e) =>
                setFormData({ ...formData, basePrice: e.target.value })
              }
            />
          </label>
          <p>
            Existing balances are adjusted in{" "}
            <Link to="/admin/inventory">Inventory</Link> with a required reason.
          </p>
          <label className="form-group">
            Stock
            <input
              className="form-control"
              type="number"
              min="0"
              disabled={Boolean(editingProduct)}
              value={formData.stock}
              required
              onChange={(e) =>
                setFormData({ ...formData, stock: e.target.value })
              }
            />
          </label>
        </div>
        <div className="form-row">
          <label className="form-group">
            SKU
            <input
              className="form-control"
              value={formData.baseSku}
              onChange={(e) =>
                setFormData({ ...formData, baseSku: e.target.value })
              }
            />
          </label>
          <label className="form-group">
            Category
            <select
              className="form-control"
              value={formData.categoryId}
              onChange={(e) =>
                setFormData({ ...formData, categoryId: e.target.value })
              }
            >
              <option value="">None</option>
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>
        </div>
        <label className="form-group">
          Short description
          <input
            className="form-control"
            value={formData.shortDescription}
            onChange={(e) =>
              setFormData({ ...formData, shortDescription: e.target.value })
            }
          />
        </label>
        <label className="form-group">
          Description
          <textarea
            className="form-control"
            required
            value={formData.description}
            onChange={(e) =>
              setFormData({ ...formData, description: e.target.value })
            }
          />
        </label>
        <label className="form-group">
          Collections
          <select
            multiple
            className="form-control"
            value={formData.collectionIds.map(String)}
            onChange={(e) =>
              setFormData({
                ...formData,
                collectionIds: Array.from(e.target.selectedOptions).map(
                  (option) => option.value,
                ),
              })
            }
          >
            {collections.map((collection) => (
              <option key={collection.id} value={collection.id}>
                {collection.name}
              </option>
            ))}
          </select>
        </label>
        <div className="form-row">
          <label>
            <input
              type="checkbox"
              checked={formData.isActive}
              onChange={(e) =>
                setFormData({ ...formData, isActive: e.target.checked })
              }
            />{" "}
            Active
          </label>
          <label>
            <input
              type="checkbox"
              checked={formData.isFeatured}
              onChange={(e) =>
                setFormData({ ...formData, isFeatured: e.target.checked })
              }
            />{" "}
            Featured
          </label>
        </div>
        <div className="form-actions">
          <button type="button" className="btn-cancel" onClick={resetForm}>
            Clear
          </button>
          <button type="submit" className="btn-submit" disabled={pending}>
            {editingProduct ? "Save Product" : "Create Product"}
          </button>
        </div>
      </form>
    </div>
  );
}
