import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { FiEye, FiRefreshCw } from "react-icons/fi";
import { orderApi } from "../../services/orderApi";
import "./AdminStyles.css";

const AdOrders = () => {
  const navigate = useNavigate();
  const [orders, setOrders] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState({
    status: "",
    search: "",
    pageNumber: 1,
    pageSize: 20
  });

  useEffect(() => {
    fetchOrders();
  }, [filters.status, filters.search, filters.pageNumber]);

  const fetchOrders = async () => {
    setIsLoading(true);
    try {
      const response = await orderApi.getAllOrders({
        status: filters.status || undefined,
        search: filters.search || undefined,
        pageNumber: filters.pageNumber,
        pageSize: filters.pageSize
      });
      setOrders(response.orders || response || []);
    } catch (err) {
      console.error("Error fetching orders:", err);
      alert("Failed to load orders");
    } finally {
      setIsLoading(false);
    }
  };

  const handleStatusUpdate = async (orderId, newStatus) => {
    try {
      await orderApi.updateOrderStatus(orderId, newStatus);
      alert("Order status updated successfully!");
      fetchOrders();
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to update order status";
      alert(errorMessage);
    }
  };

  const getStatusColor = (status) => {
    const colors = {
      'Pending': '#ffa500',
      'Processing': '#2196F3',
      'Shipped': '#9C27B0',
      'Delivered': '#4CAF50',
      'Cancelled': '#f44336'
    };
    return colors[status] || '#666';
  };

  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Order Management</h2>
            <p>View and manage all customer orders</p>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div style={{ marginBottom: '1.5rem', display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
        <input
          type="text"
          placeholder="Search by order number or customer..."
          value={filters.search}
          onChange={(e) => setFilters({ ...filters, search: e.target.value, pageNumber: 1 })}
          style={{ flex: 1, minWidth: '200px', padding: '0.75rem', borderRadius: '4px', border: '1px solid #ddd' }}
        />
        <select
          value={filters.status}
          onChange={(e) => setFilters({ ...filters, status: e.target.value, pageNumber: 1 })}
          style={{ padding: '0.75rem', borderRadius: '4px', border: '1px solid #ddd' }}
        >
          <option value="">All Statuses</option>
          <option value="Pending">Pending</option>
          <option value="Processing">Processing</option>
          <option value="Shipped">Shipped</option>
          <option value="Delivered">Delivered</option>
          <option value="Cancelled">Cancelled</option>
        </select>
        <button onClick={fetchOrders} className="btn btn-outline" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <FiRefreshCw /> Refresh
        </button>
      </div>

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading orders...</p>
        </div>
      ) : (
        <div className="table-responsive">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Order Number</th>
                <th>Customer</th>
                <th>Date</th>
                <th>Total</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {orders.length === 0 ? (
                <tr>
                  <td colSpan="6" style={{ textAlign: 'center', padding: '2rem' }}>
                    No orders found
                  </td>
                </tr>
              ) : (
                orders.map((order) => (
                  <tr key={order.id}>
                    <td>
                      <strong>{order.orderNumber || `#${order.id}`}</strong>
                    </td>
                    <td>
                      <div>
                        <div>{order.userName || order.customerName || 'N/A'}</div>
                        <small style={{ color: '#666' }}>{order.userEmail || order.customerEmail || ''}</small>
                      </div>
                    </td>
                    <td>{new Date(order.orderDate).toLocaleDateString()}</td>
                    <td>Rs. {order.total?.toLocaleString()}</td>
                    <td>
                      <select
                        value={order.status}
                        onChange={(e) => handleStatusUpdate(order.id, e.target.value)}
                        style={{
                          padding: '0.5rem',
                          borderRadius: '4px',
                          border: '1px solid #ddd',
                          background: getStatusColor(order.status),
                          color: 'white',
                          fontWeight: '500'
                        }}
                      >
                        <option value="Pending">Pending</option>
                        <option value="Processing">Processing</option>
                        <option value="Shipped">Shipped</option>
                        <option value="Delivered">Delivered</option>
                        <option value="Cancelled">Cancelled</option>
                      </select>
                    </td>
                    <td>
                      <button
                        className="btn-edit"
                        onClick={() => navigate(`/admin/orders/${order.id}`)}
                        title="View Details"
                      >
                        <FiEye /> View
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default AdOrders;
