import { StatusBadge } from "../../shared/components/StatusBadge.jsx";
import AppIcon from "../../../components/AppIcon.jsx";
export function CategoriesTable({ items, edit, toggle }) {
  return (
    <div className="products-table-container">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">Name</th>
            <th scope="col">Slug</th>
            <th scope="col">Parent</th>
            <th scope="col">Sort</th>
            <th scope="col">Status</th>
            <th scope="col">Actions</th>
          </tr>
        </thead>
        <tbody>
          {items.map((category) => (
            <tr key={category.id}>
              <td>{category.name}</td>
              <td>{category.slug}</td>
              <td>
                {items.find((item) => item.id === category.parentCategoryId)
                  ?.name || ""}
              </td>
              <td>{category.sortOrder}</td>
              <td>
                <StatusBadge
                  status={category.isActive ? "Active" : "Archived"}
                  tone={category.isActive ? "success" : "warning"}
                />
              </td>
              <td>
                <div className="action-buttons">
                  <button
                    className="btn-edit"
                    aria-label={`Edit ${category.name}`}
                    onClick={() => edit(category)}
                  >
                    <AppIcon name="edit" />
                  </button>
                  <button
                    className="btn-delete"
                    aria-label={`${category.isActive ? "Archive" : "Reactivate"} ${category.name}`}
                    onClick={() => toggle(category)}
                  >
                    <AppIcon name={category.isActive ? "archive" : "refresh"} />
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
