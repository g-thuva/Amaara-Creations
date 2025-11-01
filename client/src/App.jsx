import { Routes, Route, Navigate, useLocation } from "react-router-dom";
import Navbar from "./components/Navbar";
import Footer from "./components/Footer";
import "./App.css";
import Home from "./pages/Home";
import Products from "./pages/Products";
import CustomBuilder from "./pages/CustomBuilder";
import Wishlist from "./pages/Wishlisst";
import Cart from "./pages/Cart";
import Profile from "./pages/Profile";
import Orders from "./pages/Orders";
import Login from "./pages/Login";
import Register from "./pages/Register";
import ProductDetails from "./pages/ProductDetails";


//admin
import AdminLayout from "./pages/admin/AdminLayout";
import Dashboard from "./pages/admin/Dashboard";
import AdProducts from "./pages/admin/AdProducts";
import Reviews from "./pages/admin/Reviews";
import Customers from "./pages/admin/Customers";
import AdOrders from "./pages/admin/AdOrders";


function App() {
  const location = useLocation();
  const isAdminRoute = location.pathname.startsWith('/admin');

  return (
    <div className="app-container">
      {!isAdminRoute && <Navbar />}
      <main className={isAdminRoute ? "admin-main-content" : "main-content"}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/products" element={<Products />} />
          <Route path="/custom" element={<CustomBuilder />} />
          <Route path="/wishlist" element={<Wishlist />} />
          <Route path="/cart" element={<Cart />} />
          <Route path="/profile" element={<Profile />} />
          <Route path="/orders" element={<Orders />} />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/products/:id" element={<ProductDetails />} />

          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<Navigate to="/admin/dashboard" replace />} />
            <Route path="dashboard" element={<Dashboard />} />
            <Route path="products" element={<AdProducts />} />
            <Route path="reviews" element={<Reviews />} />
            <Route path="customers" element={<Customers />} />
            <Route path="orders" element={<AdOrders />} />
          </Route>
        </Routes>
      </main>
      {!isAdminRoute && <Footer />}
    </div>
  );
}

export default App;
