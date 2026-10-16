import { formatDate } from "../../shared/utils/formatDateTime.js";
import { Link } from "react-router-dom";
import { checkoutMoney } from "../../../services/checkoutApi.js";
import { StatusBadge } from "../../shared/components/StatusBadge.jsx";
export function RecentOrdersTable({ recentOrders }) {
  const getStatusColor = (status) => {
    const colors = {
      Pending: "pending",
      Processing: "processing",
      Shipped: "shipped",
      Delivered: "completed",
      Cancelled: "pending",
    };
    return colors[status] || "pending";
  };
  return (
    <div className="dashboard-card">
      <div className="card-header">
        <h3>Recent Orders</h3>
        <Link to="/admin/orders" className="view-all">
          View All
        </Link>
      </div>
      <div className="table-responsive">
        <table>
          <thead>
            <tr>
              <th scope="col">Order ID</th>
              <th scope="col">Customer</th>
              <th scope="col">Date</th>
              <th scope="col">Amount</th>
              <th scope="col">Status</th>
            </tr>
          </thead>
          <tbody>
            {recentOrders.length === 0 ? (
              <tr>
                <td
                  colSpan="5"
                  style={{ textAlign: "center", padding: "2rem" }}
                >
                  No orders yet
                </td>
              </tr>
            ) : (
              recentOrders.map((order) => (
                <tr key={order.id}>
                  <td>
                    <Link
                      to={`/admin/orders/${order.id}`}
                      className="order-link"
                    >
                      {order.orderNumber || `#${order.id}`}
                    </Link>
                  </td>
                  <td>{order.customerName || order.customerEmail}</td>
                  <td>{formatDate(order.orderDate)}</td>
                  <td>{checkoutMoney(order.total, order.currency || "LKR")}</td>
                  <td>
                    <StatusBadge
                      status={order.status}
                      className={`status-badge ${getStatusColor(order.status)}`}
                    />
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
