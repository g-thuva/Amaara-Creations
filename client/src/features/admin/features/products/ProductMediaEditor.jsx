import { useRef, useState } from "react";
import { resolveMediaUrl } from "@/shared/api/requestConfig.js";

export function ProductMediaEditor({
  activeProduct,
  media,
  uploadMedia,
  setMediaFile,
  mediaAltText,
  setMediaAltText,
  makePrimary,
  pending,
}) {
  const inputRef = useRef(null);
  const [dragging, setDragging] = useState(false);
  const [localFile, setLocalFile] = useState(null);
  const [previewUrl, setPreviewUrl] = useState(null);

  const pickFile = (file) => {
    if (!file) return;
    if (previewUrl) URL.revokeObjectURL(previewUrl);
    setLocalFile(file);
    setPreviewUrl(URL.createObjectURL(file));
    setMediaFile(file);
  };

  const onDrop = (e) => {
    e.preventDefault();
    setDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) pickFile(file);
  };

  const handleSubmit = async (e) => {
    await uploadMedia(e);
    setLocalFile(null);
    if (previewUrl) { URL.revokeObjectURL(previewUrl); setPreviewUrl(null); }
  };

  return (
    <div className="pf-media-section">
      <h3 className="pf-section-title" style={{ marginBottom: 12 }}>
        📷 Photos &amp; Videos — {activeProduct.name}
      </h3>

      {/* Existing media grid */}
      {media.length > 0 && (
        <div className="pf-preview-grid" style={{ marginBottom: 20 }}>
          {media.map((item) => (
            <div key={item.id} className="pf-preview-card">
              {item.mediaType === "video" ? (
                <video src={resolveMediaUrl(item.url)} className="pf-preview-thumb" muted playsInline />
              ) : (
                <img
                  src={resolveMediaUrl(item.url)}
                  alt={item.altText || activeProduct.name}
                  className="pf-preview-thumb"
                />
              )}
              {item.isPrimary && <span className="pf-badge-primary">Cover</span>}
              <div className="pf-alt-input" style={{ fontSize: 11, color: "var(--text-muted)" }}>
                {item.altText || item.originalFileName || "—"}
              </div>
              {!item.isPrimary && (
                <button
                  type="button"
                  style={{ fontSize: 11, border: "none", background: "none", color: "var(--navy-600)", cursor: "pointer", padding: "4px 8px" }}
                  disabled={pending}
                  onClick={() => makePrimary(item)}
                >
                  ★ Set as cover
                </button>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Upload new */}
      <form onSubmit={handleSubmit}>
        <div
          className={`pf-dropzone${dragging ? " pf-dropzone--over" : ""}`}
          style={{ padding: "24px 16px" }}
          onDragOver={(e) => { e.preventDefault(); setDragging(true); }}
          onDragLeave={() => setDragging(false)}
          onDrop={onDrop}
          onClick={() => inputRef.current?.click()}
          role="button"
          tabIndex={0}
          onKeyDown={(e) => e.key === "Enter" && inputRef.current?.click()}
        >
          <input
            ref={inputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp,video/mp4,video/webm,video/quicktime"
            style={{ display: "none" }}
            onChange={(e) => pickFile(e.target.files?.[0])}
          />
          {previewUrl ? (
            localFile?.type?.startsWith("video/") ? (
              <video src={previewUrl} style={{ maxHeight: 120, borderRadius: 6 }} muted playsInline />
            ) : (
              <img src={previewUrl} alt="preview" style={{ maxHeight: 120, borderRadius: 6, objectFit: "cover" }} />
            )
          ) : (
            <>
              <span className="pf-dropzone-icon">📁</span>
              <p className="pf-dropzone-label">
                <strong>Drag &amp; drop or click to add photo / video</strong>
                <span>JPG, PNG, WebP, MP4, MOV</span>
              </p>
            </>
          )}
        </div>

        {localFile && (
          <div style={{ display: "flex", gap: 8, marginTop: 10, alignItems: "center" }}>
            <input
              className="form-control"
              style={{ flex: 1 }}
              placeholder="Caption / alt text (optional)"
              value={mediaAltText}
              onChange={(e) => setMediaAltText(e.target.value)}
            />
            <button className="btn btn-primary" type="submit" disabled={pending}>
              {pending ? "Uploading…" : "Upload"}
            </button>
            <button
              type="button"
              className="btn-cancel"
              onClick={() => {
                setLocalFile(null);
                if (previewUrl) URL.revokeObjectURL(previewUrl);
                setPreviewUrl(null);
                setMediaFile(null);
              }}
            >
              Cancel
            </button>
          </div>
        )}
        {media.length === 0 && !localFile && (
          <p style={{ fontSize: 12, color: "var(--text-muted)", marginTop: 8 }}>
            No photos yet. Upload the first one above.
          </p>
        )}
      </form>
    </div>
  );
}
