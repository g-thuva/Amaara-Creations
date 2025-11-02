import React, { useState, useEffect } from "react";
import { useParams, useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { orderApi } from "../services/orderApi";
import "./OrderDetails.css";

const OrderDetails = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated } = useAuth();
  const [order, setOrder] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login", { state: { from: `/orders/${id}` } });
      return;
    }

    const fetchOrder = async () => {
      setIsLoading(true);
      setError("");
      try {
        const orderData = await orderApi.getOrderById(id);
        setOrder(orderData);
      } catch (err) {
        console.error("Error fetching order:", err);
        setError("Failed to load order details");
      } finally {
        setIsLoading(false);
      }
    };

    if (id) {
      fetchOrder();
    }
  }, [id, isAuthenticated, navigate]);

  const formatPrice = (price) => {
    return price.toLocaleString('en-IN');
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

  if (isLoading) {
    return (
      <div className="order-details-container">
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading order details...</p>
        </div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="order-details-container">
        <div style={{ textAlign: 'center', padding: '3rem', color: 'red' }}>
          <p>{error || "Order not found"}</p>
          <button onClick={() => navigate("/orders")} className="btn btn-primary" style={{ marginTop: '1rem' }}>
            Back to Orders
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="order-details-container">
      <div className="order-header">
        <button onClick={() => navigate("/orders")} className="back-button">
          ← Back to Orders
        </button>
        <h1>Order Details</h1>
        {location.state?.message && (
          <div className="success-message">{location.state.message}</div>
        )}
      </div>

      <div className="order-content">
        <div className="order-info-card">
          <h2>Order Information</h2>
          <div className="info-grid">
            <div className="info-item">
              <span className="info-label">Order Number:</span>
              <span className="info-value">{order.orderNumber || `#${order.id}`}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Order Date:</span>
              <span className="info-value">{new Date(order.orderDate).toLocaleDateString()}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Status:</span>
              <span
                className="info-value"
                style={{
                  background: getStatusColor(order.status),
                  color: 'white',
                  padding: '0.25rem 0.75rem',
                  borderRadius: '4px',
                  fontSize: '0.9rem'
                }}
              >
                {order.status}
              </span>
            </div>
            <div className="info-item">
              <span className="info-label">Total:</span>
              <span className="info-value" style={{ fontSize: '1.2rem', fontWeight: '600' }}>
                Rs. {formatPrice(order.total)}
              </span>
            </div>
          </div>
        </div>

        <div className="order-info-card">
          <h2>Shipping Information</h2>
          <div className="info-grid">
            <div className="info-item">
              <span className="info-label">Address:</span>
              <span className="info-value">{order.shippingAddress || 'N/A'}</span>
            </div>
            <div className="info-item">
              <span className="info-label">City:</span>
              <span className="info-value">{order.shippingCity || 'N/A'}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Postal Code:</span>
              <span className="info-value">{order.shippingPostalCode || 'N/A'}</span>
            </div>
            <div className="info-item">
              <span className="info-label">Country:</span>
              <span className="info-value">{order.shippingCountry || 'N/A'}</span>
            </div>
          </div>
          {order.notes && (
            <div className="info-item" style={{ marginTop: '1rem' }}>
              <span className="info-label">Notes:</span>
              <span className="info-value">{order.notes}</span>
            </div>
          )}
        </div>

        <div className="order-items-card">
          <h2>Order Items</h2>
          <table className="order-items-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>Quantity</th>
                <th>Price</th>
                <th>Subtotal</th>
              </tr>
            </thead>
            <tbody>
              {order.orderItems && order.orderItems.length > 0 ? (
                order.orderItems.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <div className="order-item-product">
                        <img
                          src={item.productImageUrl || 'https://via.placeholder.com/60'}
                          alt={item.productName || item.name}
                          className="order-item-image"
                          onError={(e) => {
                            e.target.onerror = null;
                            e.target.src = 'https://via.placeholder.com/60?text=No+Image';
                          }}
                        />
                        <div>
                          <strong>{item.productName || item.name}</strong>
                        </div>
                      </div>
                    </td>
                    <td>{item.quantity}</td>
                    <td>Rs. {formatPrice(item.price)}</td>
                    <td>Rs. {formatPrice(item.subtotal)}</td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan="4" style={{ textAlign: 'center', padding: '2rem' }}>
                    No items found
                  </td>
                </tr>
              )}
            </tbody>
            <tfoot>
              <tr>
                <td colSpan="3" style={{ textAlign: 'right', fontWeight: '600' }}>
                  Total:
                </td>
                <td style={{ fontWeight: '600', fontSize: '1.1rem' }}>
                  Rs. {formatPrice(order.total)}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>
    </div>
  );
};

export default OrderDetails;

