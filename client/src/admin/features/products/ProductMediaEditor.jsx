import { resolveMediaUrl } from "../../../services/config.js";
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
      <div className="table-responsive">
        <table>
          <caption>Product images</caption>
          <thead>
            <tr>
              <th scope="col">Preview</th>
              <th scope="col">Description</th>
              <th scope="col">Dimensions</th>
              <th scope="col">Primary image</th>
            </tr>
          </thead>
          <tbody>
            {media.map((item) => (
              <tr key={item.id}>
                <td>
                  <img
                    className="product-thumbnail"
                    src={resolveMediaUrl(item.url)}
                    alt={item.altText || activeProduct.name}
                  />
                </td>
                <td>{item.altText || item.originalFileName}</td>
                <td>
                  {item.width} x {item.height}
                </td>
                <td>
                  {item.isPrimary ? (
                    "Primary"
                  ) : (
                    <button
                      className="btn btn-sm btn-outline-primary"
                      disabled={pending}
                      onClick={() => makePrimary(item)}
                    >
                      Make primary
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
