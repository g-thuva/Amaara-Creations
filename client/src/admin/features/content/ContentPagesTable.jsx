import { StatusBadge } from "../../shared/components/StatusBadge.jsx";
import { formatDate } from "../../shared/utils/formatDateTime.js";
import AppIcon from "../../../components/AppIcon.jsx";
export function ContentPagesTable({ pages, edit }) {
  return (
    <div className="products-table-container">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">Title</th>
            <th scope="col">Slug</th>
            <th scope="col">Status</th>
            <th scope="col">Updated</th>
            <th scope="col">Actions</th>
          </tr>
        </thead>
        <tbody>
          {pages.map((page) => (
            <tr key={page.id}>
              <td>{page.title}</td>
              <td>{page.slug}</td>
              <td>
                <StatusBadge
                  status={page.isPublished ? "Published" : "Draft"}
                  tone={page.isPublished ? "success" : "warning"}
                />
              </td>
              <td>{formatDate(page.updatedAt)}</td>
              <td>
                <button
                  className="btn-edit"
                  aria-label={`Edit ${page.title}`}
                  onClick={() => edit(page)}
                >
                  <AppIcon name="edit" />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
