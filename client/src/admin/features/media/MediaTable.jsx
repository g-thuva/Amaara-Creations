import { resolveMediaUrl } from "../../../services/config.js";
export function MediaTable({ items }) {
  return (
    <div className="products-table-container">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">Preview</th>
            <th scope="col">File</th>
            <th scope="col">Alt text</th>
            <th scope="col">Size</th>
            <th scope="col">Dimensions</th>
            <th scope="col">Provider</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>
                <img
                  className="product-thumbnail"
                  src={resolveMediaUrl(item.url)}
                  alt={item.altText || item.originalFileName}
                />
              </td>
              <td>{item.originalFileName || item.storageKey}</td>
              <td>{item.altText}</td>
              <td>{Math.round(item.fileSize / 1024)} KB</td>
              <td>
                {item.width} x {item.height}
              </td>
              <td>{item.storageProvider}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
