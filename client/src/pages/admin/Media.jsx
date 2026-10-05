import React, { useEffect, useState } from "react";
import { adminApi } from "../../services/adminApi";
import { resolveMediaUrl } from "../../services/config";
import "./AdminStyles.css";

const Media = () => {
  const [items, setItems] = useState([]);

  useEffect(() => {
    adminApi.getMedia().then(setItems);
  }, []);

  return (
    <div className="admin-page">
      <div className="page-header"><div className="header-content"><div><h2>Media Library</h2><p>{items.length} assets</p></div></div></div>
      <div className="products-table-container">
        <table className="admin-table">
          <thead><tr><th>Preview</th><th>File</th><th>Alt text</th><th>Size</th><th>Dimensions</th><th>Provider</th></tr></thead>
          <tbody>{items.map((item) => (
            <tr key={item.id}>
              <td><img className="product-thumbnail" src={resolveMediaUrl(item.url)} alt={item.altText || item.originalFileName} /></td>
              <td>{item.originalFileName || item.storageKey}</td>
              <td>{item.altText}</td>
              <td>{Math.round(item.fileSize / 1024)} KB</td>
              <td>{item.width} x {item.height}</td>
              <td>{item.storageProvider}</td>
            </tr>
          ))}</tbody>
        </table>
      </div>
    </div>
  );
};

export default Media;
