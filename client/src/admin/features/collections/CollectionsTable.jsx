import { StatusBadge } from "../../shared/components/StatusBadge.jsx";
import AppIcon from "../../../components/AppIcon.jsx";
export function CollectionsTable({ items, edit, toggle }) {
  return (
    <div className="products-table-container">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">Name</th>
            <th scope="col">Slug</th>
            <th scope="col">Products</th>
            <th scope="col">Status</th>
            <th scope="col">Actions</th>
          </tr>
        </thead>
        <tbody>
          {items.map((collection) => (
            <tr key={collection.id}>
              <td>{collection.name}</td>
              <td>{collection.slug}</td>
              <td>{collection.productCount}</td>
              <td>
                <StatusBadge
                  status={collection.isActive ? "Active" : "Archived"}
                  tone={collection.isActive ? "success" : "warning"}
                />
              </td>
              <td>
                <div className="action-buttons">
                  <button
                    className="btn-edit"
                    aria-label={`Edit ${collection.name}`}
                    onClick={() => edit(collection)}
                  >
                    <AppIcon name="edit" />
                  </button>
                  <button
                    className="btn-delete"
                    aria-label={`${collection.isActive ? "Archive" : "Reactivate"} ${collection.name}`}
                    onClick={() => toggle(collection)}
                  >
                    <AppIcon
                      name={collection.isActive ? "archive" : "refresh"}
                    />
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
