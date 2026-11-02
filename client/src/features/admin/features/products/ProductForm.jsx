import { Link } from "react-router-dom";
import { resolveMediaUrl } from "@/shared/api/requestConfig.js";
import { ProductMediaPicker } from "./ProductMediaPicker.jsx";

export function ProductForm({
  editingProduct,
  formData,
  setFormData,
  categories,
  collections,
  saveProduct,
  pending,
  readOnly,
  existingMedia = [],
  formId,
  pendingMediaFiles,
  setPendingMediaFiles,
}) {
  return (
    <form id={formId} className="pf-form" onSubmit={saveProduct}>
      <fieldset className="admin-form-fields" disabled={pending}>
        {/* ── Two-column layout ── */}
        <div className="pf-layout">

          {/* LEFT: core fields */}
          <fieldset className="pf-left admin-form-fields" disabled={readOnly}>
            <section className="pf-section">
              <h3 className="pf-section-title">Basic info</h3>
              <label className="form-group">
                Product name <span aria-hidden="true" className="admin-required" />
                <input
                  className="form-control"
                  value={formData.name}
                  data-dialog-autofocus
                  maxLength={200}
                  required
                  placeholder="e.g. Arunrajh & Shaadsajini Wedding Sign"
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                />
              </label>
              <label className="form-group">
                Short description
                <input
                  className="form-control"
                  value={formData.shortDescription}
                  maxLength={300}
                  placeholder="One-line summary shown in search results"
                  onChange={(e) => setFormData({ ...formData, shortDescription: e.target.value })}
                />
              </label>
              <label className="form-group">
                Full description <span aria-hidden="true" className="admin-required" />
                <textarea
                  className="form-control pf-textarea"
                  required
                  rows={4}
                  value={formData.description}
                  maxLength={500}
                  placeholder="Describe materials, size options, personalisation details…"
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                />
              </label>
              <div className="form-row">
                <label className="form-group">
                  URL slug
                  <input
                    className="form-control"
                    value={formData.slug}
                  maxLength={220}
                    placeholder="auto-generated"
                    onChange={(e) => setFormData({ ...formData, slug: e.target.value })}
                  />
                </label>
                <label className="form-group">
                  SKU
                  <input
                    className="form-control"
                    value={formData.baseSku}
                  maxLength={64}
                    placeholder="e.g. AC-SIGN-001"
                    onChange={(e) => setFormData({ ...formData, baseSku: e.target.value })}
                  />
                </label>
              </div>
            </section>

            <section className="pf-section">
              <h3 className="pf-section-title">Pricing &amp; stock</h3>
              <div className="form-row">
                <label className="form-group">
                  Currency <span aria-hidden="true" className="admin-required" />
                  <select
                    className="form-control"
                    value={formData.currency}
                    disabled={Boolean(editingProduct)}
                    required
                    onChange={(e) => setFormData({ ...formData, currency: e.target.value })}
                  >
                    <option value="">Choose currency</option>
                    {["LKR", "CHF", "USD", "EUR", "GBP"].map((c) => (
                      <option key={c}>{c}</option>
                    ))}
                  </select>
                </label>
                <label className="form-group">
                  Base price <span aria-hidden="true" className="admin-required" />
                  <input
                    className="form-control"
                    type="number"
                    min="0"
                    step="0.01"
                    value={formData.basePrice}
                    required
                    placeholder="0.00"
                    onChange={(e) => setFormData({ ...formData, basePrice: e.target.value })}
                  />
                </label>
                <div className="form-group">
                  <label htmlFor={`${formId}-stock`}>Stock {!editingProduct && <span aria-hidden="true" className="admin-required" />}</label>
                  <input
                    id={`${formId}-stock`}
                    aria-describedby={editingProduct ? `${formId}-stock-help` : undefined}
                    className="form-control"
                    type="number"
                    min="0"
                    disabled={Boolean(editingProduct)}
                    value={formData.stock}
                    required
                    onChange={(e) => setFormData({ ...formData, stock: e.target.value })}
                  />
                  {editingProduct && (
                    <small id={`${formId}-stock-help`}>Adjust stock via <Link to="/admin/inventory">Inventory</Link></small>
                  )}
                </div>
              </div>
            </section>

            <section className="pf-section">
              <h3 className="pf-section-title">Catalogue</h3>
              <div className="form-row">
                <label className="form-group">
                  Category
                  <select
                    className="form-control"
                    value={formData.categoryId}
                    onChange={(e) => setFormData({ ...formData, categoryId: e.target.value })}
                  >
                    <option value="">— None —</option>
                    {categories.map((cat) => (
                      <option key={cat.id} value={cat.id}>{cat.name}</option>
                    ))}
                  </select>
                </label>
                <div className="form-group">
                  <label htmlFor={`${formId}-collections`}>Collections</label>
                  <select
                    id={`${formId}-collections`}
                    aria-describedby={`${formId}-collections-help`}
                    multiple
                    className="form-control"
                    value={formData.collectionIds.map(String)}
                    onChange={(e) =>
                      setFormData({
                        ...formData,
                        collectionIds: Array.from(e.target.selectedOptions).map((o) => o.value),
                      })
                    }
                  >
                    {collections.map((col) => (
                      <option key={col.id} value={col.id}>{col.name}</option>
                    ))}
                  </select>
                  <small id={`${formId}-collections-help`}>Hold Ctrl / ⌘ to select multiple</small>
                </div>
              </div>
            </section>

            <section className="pf-section">
              <h3 className="pf-section-title">Visibility</h3>
              <div className="pf-toggle-row">
                <label className="pf-toggle">
                  <input
                    type="checkbox"
                    checked={formData.isActive}
                    onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  />
                  <span className="pf-toggle-label">Active — visible in shop</span>
                </label>
                <label className="pf-toggle">
                  <input
                    type="checkbox"
                    checked={formData.isFeatured}
                    onChange={(e) => setFormData({ ...formData, isFeatured: e.target.checked })}
                  />
                  <span className="pf-toggle-label">Featured — shown on home page</span>
                </label>
              </div>
            </section>
          </fieldset>

          {/* RIGHT: media */}
          <div className="pf-right">
            <section className="pf-section">
              <h3 className="pf-section-title">Photos &amp; Videos</h3>
              <p className="pf-section-hint">
                Arrange new uploads before saving. Existing media and cover images are kept.
              </p>
              {existingMedia.length > 0 && <div className="pf-existing-media" aria-label="Existing product media">
                {existingMedia.map(media => <figure key={media.id}>
                  {media.mediaType === 'video' ? <video controls playsInline src={resolveMediaUrl(media.url)} aria-label={media.altText || 'Product video'} /> : <img src={resolveMediaUrl(media.url)} alt={media.altText || 'Existing product image'} />}
                  {media.isPrimary && <figcaption>Cover photo</figcaption>}
                </figure>)}
              </div>}
              <ProductMediaPicker disabled={pending} files={pendingMediaFiles ?? []} onChange={setPendingMediaFiles} />
            </section>
          </div>

        </div>

      </fieldset>
    </form>
  );
}
