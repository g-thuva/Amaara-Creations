import React from "react";

const Orders = () => {
  const orders = [
    {
      id: "ORD123",
      date: "2025-08-25",
      total: 850,
      status: "Delivered",
      items: [
        { name: "Wedding Sticker A", qty: 2 },
        { name: "Custom Sticker C", qty: 1 }
      ]
    },
    {
      id: "ORD124",
      date: "2025-08-20",
      total: 300,
      status: "Processing",
      items: [
        { name: "Car Sticker B", qty: 1 }
      ]
    }
  ];

  return (
    <div className="container" style={{ padding: "2rem 0" }}>
      <h2 style={{ fontSize: "2rem", marginBottom: "1rem" }}>My Orders</h2>

      {orders.map((order) => (
        <div key={order.id} style={{ background: "#fff", padding: "1.2rem", marginBottom: "1rem", borderRadius: "8px" }}>
          <h4 style={{ marginBottom: "0.5rem" }}>Order #{order.id}</h4>
          <p><strong>Date:</strong> {order.date}</p>
          <p><strong>Status:</strong> {order.status}</p>
          <p><strong>Total:</strong> Rs. {order.total}</p>
          <p><strong>Items:</strong></p>
          <ul style={{ marginLeft: "1rem" }}>
            {order.items.map((item, idx) => (
              <li key={idx}>{item.name} × {item.qty}</li>
            ))}
          </ul>
        </div>
      ))}
    </div>
  );
};

export default Orders;
