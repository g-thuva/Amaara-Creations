import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { orderApi } from "../services/orderApi";

const Orders = () => {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [orders, setOrders] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  // Fetch user orders
  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login", { state: { from: "/orders" } });
      return;
    }

    const fetchOrders = async () => {
      setIsLoading(true);
      try {
        const ordersData = await orderApi.getUserOrders();
        setOrders(Array.isArray(ordersData) ? ordersData : []);
      } catch (err) {
        console.error("Error fetching orders:", err);
        alert("Failed to load orders. Please try again.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchOrders();
  }, [isAuthenticated, navigate]);

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
      <div className="container" style={{ padding: "2rem 0" }}>
        <div style={{ textAlign: 'center' }}>
          <p>Loading orders...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="container" style={{ padding: "2rem 0" }}>
      <h2 style={{ fontSize: "2rem", marginBottom: "1rem" }}>My Orders</h2>

      {orders.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '3rem', background: '#fff', borderRadius: '8px' }}>
          <p>You have no orders yet.</p>
          <button 
            onClick={() => navigate('/products')} 
            style={{ marginTop: '1rem', padding: '0.75rem 1.5rem', background: '#007bff', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}
          >
            Start Shopping
          </button>
        </div>
      ) : (
        orders.map((order) => (
          <div 
            key={order.id} 
            style={{ 
              background: "#fff", 
              padding: "1.5rem", 
              marginBottom: "1rem", 
              borderRadius: "8px",
              boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
              cursor: 'pointer'
            }}
            onClick={() => navigate(`/orders/${order.id}`)}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1rem' }}>
              <h4 style={{ margin: 0 }}>Order #{order.orderNumber || order.id}</h4>
              <span style={{ 
                padding: '0.25rem 0.75rem', 
                borderRadius: '4px', 
                background: getStatusColor(order.status),
                color: 'white',
                fontSize: '0.85rem'
              }}>
                {order.status}
              </span>
            </div>
            <p><strong>Date:</strong> {new Date(order.orderDate).toLocaleDateString()}</p>
            <p><strong>Total:</strong> Rs. {order.total?.toLocaleString()}</p>
            {order.orderItems && order.orderItems.length > 0 && (
              <>
                <p><strong>Items ({order.orderItems.length}):</strong></p>
                <ul style={{ marginLeft: "1rem", marginTop: '0.5rem' }}>
                  {order.orderItems.slice(0, 3).map((item, idx) => (
                    <li key={idx}>
                      {item.productName || item.name} × {item.quantity}
                    </li>
                  ))}
                  {order.orderItems.length > 3 && (
                    <li>... and {order.orderItems.length - 3} more item(s)</li>
                  )}
                </ul>
              </>
            )}
            <p style={{ marginTop: '0.5rem', fontSize: '0.9rem', color: '#666' }}>
              Click to view details
            </p>
          </div>
        ))
      )}
    </div>
  );
};

export default Orders;
