import { resolveMediaUrl } from "@/shared/api/requestConfig.js";
import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
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
  return (
    <>
      <form onSubmit={uploadMedia} className="form-row">
        <input
          className="form-control"
          type="file"
          aria-label="Product image"
          accept="image/jpeg,image/png,image/webp"
          onChange={(e) => setMediaFile(e.target.files?.[0] || null)}
        />
        <input
          className="form-control"
          aria-label="Alt text"
          value={mediaAltText}
          onChange={(e) => setMediaAltText(e.target.value)}
          placeholder="Alt text"
        />
        <button className="btn btn-primary" type="submit" disabled={pending}>
          Upload
        </button>
      </form>
      <DataTable caption="Product images" rows={media} empty="No product images yet" emptyDescription="Upload an image with helpful alt text using the form above." columns={[
        { key: 'preview', label: 'Preview', render: item => <img className="product-thumbnail" src={resolveMediaUrl(item.url)} alt={item.altText || activeProduct.name} /> },
        { key: 'description', label: 'Description', render: item => item.altText || item.originalFileName },
        { key: 'dimensions', label: 'Dimensions', render: item => `${item.width} × ${item.height}` },
        { key: 'primary', label: 'Primary image', render: item => item.isPrimary ? 'Primary' : <button className="btn btn-sm btn-outline-primary" disabled={pending} onClick={() => makePrimary(item)}>Make primary</button> },
      ]} />
    </>
  );
}
