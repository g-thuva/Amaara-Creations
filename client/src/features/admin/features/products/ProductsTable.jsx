import { StatusBadge } from "@/features/admin/shared/components/StatusBadge.jsx";
import { resolveMediaUrl } from "@/shared/api/requestConfig.js";
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
import AppIcon from "@/shared/components/ui/Icon.jsx";
export function ProductsTable({
  products,
  isLoading,
  startEdit,
  openProductTools,
  toggleArchive,
  pending,
}) {
  return (
    <div className="products-table-container">
      <table className="admin-table">
        <thead>
          <tr>
            <th scope="col">Image</th>
            <th scope="col">Name</th>
            <th scope="col">Category</th>
            <th scope="col">Price</th>
            <th scope="col">Stock</th>
            <th scope="col">Status</th>
            <th scope="col">Actions</th>
          </tr>
        </thead>
        <tbody>
          {isLoading ? (
            <tr>
              <td colSpan="7">Loading products...</td>
            </tr>
          ) : products.length === 0 ? (
            <tr>
              <td colSpan="7">No products found.</td>
            </tr>
          ) : (
            products.map((product) => (
              <tr key={product.id}>
                <td>
                  <img
                    className="product-thumbnail"
                    src={resolveMediaUrl(product.imageUrl)}
                    alt={product.name}
                  />
                </td>
                <td>
                  <div className="product-name-cell">
                    <strong>{product.name}</strong>
                    <small>{product.baseSku || product.slug}</small>
                  </div>
                </td>
                <td>{product.categoryName || product.category}</td>
                <td>{checkoutMoney(product.basePrice, product.currency)}</td>
                <td>{product.stock}</td>
                <td>
                  <StatusBadge
                    status={product.isActive ? "Active" : "Archived"}
                    tone={product.isActive ? "success" : "warning"}
                  />
                </td>
                <td>
                  <div className="action-buttons">
                    <button
                      className="btn-edit"
                      disabled={pending}
                      title="Edit"
                      onClick={() => startEdit(product)}
                    >
                      <AppIcon name="edit" />
                    </button>
                    <button
                      className="btn-edit"
                      disabled={pending}
                      title="Media and variants"
                      onClick={() => openProductTools(product)}
                    >
                      <AppIcon name="layers" />
                    </button>
                    <button
                      className="btn-delete"
                      disabled={pending}
                      title={product.isActive ? "Archive" : "Reactivate"}
                      onClick={() => toggleArchive(product)}
                    >
                      <AppIcon
                        name={product.isActive ? "archive" : "refresh"}
                      />
                    </button>
                  </div>
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}
