import React from "react";
import { Link, Outlet } from "react-router-dom";

const AdminLayout = () => {
  return (
    <div style={{ display: "flex", minHeight: "100vh" }}>
      <aside style={{
        width: "200px",
        backgroundColor: "#333",
        color: "#fff",
        padding: "1rem"
      }}>
        <h3 style={{ marginBottom: "1rem" }}>Admin Panel</h3>
        <nav style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          <Link to="/admin/dashboard" style={{ color: "#fff" }}>Dashboard</Link>
          <Link to="/admin/products" style={{ color: "#fff" }}>Products</Link>
          <Link to="/admin/reviews" style={{ color: "#fff" }}>Reviews</Link>
          <Link to="/admin/customers" style={{ color: "#fff" }}>Customers</Link>
          <Link to="/admin/orders" style={{ color: "#fff" }}>Orders</Link>
        </nav>
      </aside>

      <main style={{ flex: 1, padding: "2rem" }}>
        <Outlet />
      </main>
    </div>
  );
};

export default AdminLayout;
