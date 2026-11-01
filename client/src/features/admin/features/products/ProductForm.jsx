import { useRef, useState, useCallback } from "react";
import { Link } from "react-router-dom";

function MediaDropZone({ files, onChange }) {
  const inputRef = useRef(null);
  const [dragging, setDragging] = useState(false);

  const addFiles = useCallback((incoming) => {
    const valid = Array.from(incoming).filter((f) =>
      f.type.startsWith("image/") || f.type.startsWith("video/")
    );
    if (!valid.length) return;
    onChange((prev) => [
      ...prev,
      ...valid.map((file) => ({ file, altText: "", preview: URL.createObjectURL(file) })),
    ]);
  }, [onChange]);

  const onDrop = (e) => {
    e.preventDefault();
    setDragging(false);
    addFiles(e.dataTransfer.files);
  };

  const removeFile = (idx) => {
    onChange((prev) => {
      URL.revokeObjectURL(prev[idx].preview);
      return prev.filter((_, i) => i !== idx);
    });
  };

  const updateAlt = (idx, value) => {
    onChange((prev) => prev.map((item, i) => i === idx ? { ...item, altText: value } : item));
  };

  return (
    <div className="pf-media-section">
      <div
        className={`pf-dropzone${dragging ? " pf-dropzone--over" : ""}`}
        onDragOver={(e) => { e.preventDefault(); setDragging(true); }}
        onDragLeave={() => setDragging(false)}
        onDrop={onDrop}
        onClick={() => inputRef.current?.click()}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => e.key === "Enter" && inputRef.current?.click()}
        aria-label="Upload product images or videos"
      >
        <input
          ref={inputRef}
          type="file"
          multiple
          accept="image/jpeg,image/png,image/webp,image/gif,video/mp4,video/webm,video/quicktime"
          style={{ display: "none" }}
          onChange={(e) => addFiles(e.target.files)}
        />
        <span className="pf-dropzone-icon">📁</span>
        <p className="pf-dropzone-label">
          <strong>Drag &amp; drop photos / videos here</strong>
          <br />
          <span>or click to browse &nbsp;·&nbsp; JPG, PNG, WebP, GIF, MP4, MOV</span>
        </p>
      </div>

      {files.length > 0 && (
        <div className="pf-preview-grid">
          {files.map((item, idx) => (
            <div key={idx} className="pf-preview-card">
              {item.file.type.startsWith("video/") ? (
                <video src={item.preview} className="pf-preview-thumb" muted playsInline />
              ) : (
                <img src={item.preview} alt={item.altText || "preview"} className="pf-preview-thumb" />
              )}
              {idx === 0 && <span className="pf-badge-primary">Cover</span>}
              <input
                className="pf-alt-input"
                placeholder="Caption / alt text"
                value={item.altText}
                onChange={(e) => updateAlt(idx, e.target.value)}
              />
              <button
                type="button"
                className="pf-remove-btn"
                aria-label="Remove"
                onClick={(e) => { e.stopPropagation(); removeFile(idx); }}
              >✕</button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

export function ProductForm({
  editingProduct,
  formData,
  setFormData,
  categories,
  collections,
  saveProduct,
  resetForm,
  pending,
  pendingMediaFiles,
  setPendingMediaFiles,
}) {
  return (
    <div className="card pf-card">
      <div className="card-header">
        <h2 className="card-title">
          {editingProduct ? "✏️ Edit Product" : "➕ New Product"}
        </h2>
        {editingProduct && (
          <span className="pf-editing-badge">Editing: {editingProduct.name}</span>
        )}
      </div>

      <form className="pf-form" onSubmit={saveProduct}>
        {/* ── Two-column layout ── */}
        <div className="pf-layout">

          {/* LEFT: core fields */}
          <div className="pf-left">
            <section className="pf-section">
              <h3 className="pf-section-title">Basic info</h3>
              <label className="form-group">
                Product name *
                <input
                  className="form-control"
                  value={formData.name}
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
                  placeholder="One-line summary shown in search results"
                  onChange={(e) => setFormData({ ...formData, shortDescription: e.target.value })}
                />
              </label>
              <label className="form-group">
                Full description *
                <textarea
                  className="form-control pf-textarea"
                  required
                  rows={4}
                  value={formData.description}
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
                    placeholder="auto-generated"
                    onChange={(e) => setFormData({ ...formData, slug: e.target.value })}
                  />
                </label>
                <label className="form-group">
                  SKU
                  <input
                    className="form-control"
                    value={formData.baseSku}
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
                  Currency *
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
                  Base price *
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
                <label className="form-group">
                  Stock
                  <input
                    className="form-control"
                    type="number"
                    min="0"
                    disabled={Boolean(editingProduct)}
                    value={formData.stock}
                    required
                    onChange={(e) => setFormData({ ...formData, stock: e.target.value })}
                  />
                  {editingProduct && (
                    <small>Adjust stock via <Link to="/admin/inventory">Inventory</Link></small>
                  )}
                </label>
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
                <label className="form-group">
                  Collections
                  <select
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
                  <small>Hold Ctrl / ⌘ to select multiple</small>
                </label>
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
          </div>

          {/* RIGHT: media */}
          <div className="pf-right">
            <section className="pf-section">
              <h3 className="pf-section-title">Photos &amp; Videos</h3>
              <p className="pf-section-hint">
                The first image becomes the cover photo. You can reorder after saving.
              </p>
              <MediaDropZone files={pendingMediaFiles ?? []} onChange={setPendingMediaFiles} />
            </section>
          </div>

        </div>

        {/* Actions */}
        <div className="pf-actions">
          <button type="button" className="btn-cancel" onClick={resetForm}>
            Cancel
          </button>
          <button type="submit" className="btn-submit pf-submit-btn" disabled={pending}>
            {pending
              ? "Saving…"
              : editingProduct
              ? "Save changes"
              : `Create product${(pendingMediaFiles?.length ?? 0) > 0 ? ` + upload ${pendingMediaFiles.length} photo${pendingMediaFiles.length > 1 ? "s" : ""}` : ""}`}
          </button>
        </div>
      </form>
    </div>
  );
}
