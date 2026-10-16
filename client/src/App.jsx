import { Routes, Route, Navigate, useLocation } from "react-router-dom";
import { lazy, Suspense } from "react";
import { StoreProvider } from "./contexts/StoreContext";
import AccountLayout from "./components/storefront/AccountLayout";
import ErrorBoundary from "./components/storefront/ErrorBoundary";
import { Skeleton } from "./components/storefront/UI";
import ContentPage from "./pages/ContentPage";
import NotFound from "./pages/NotFound";
import Seo from "./components/Seo";
import Navbar from "./components/Navbar";
import Footer from "./components/Footer";
import "./App.css";
import Home from "./pages/Home";
const Products = lazy(() => import("./pages/Products"));
const CustomBuilder = lazy(() => import("./pages/CustomBuilder"));
const Wishlist = lazy(() => import("./pages/Wishlisst"));
const Cart = lazy(() => import("./pages/Cart"));
const Checkout = lazy(() => import("./pages/Checkout"));
const PaymentReturn = lazy(() => import("./pages/PaymentReturn"));
const Profile = lazy(() => import("./pages/Profile"));
const Orders = lazy(() => import("./pages/Orders"));
const OrderDetails = lazy(() => import("./pages/OrderDetails"));
const Login = lazy(() => import("./pages/Login"));
const Register = lazy(() => import("./pages/Register"));
const ProductDetails = lazy(() => import("./pages/ProductDetails"));
import ProtectedRoute from "./components/ProtectedRoute";
const ForgotPassword = lazy(() => import("./pages/ForgotPassword"));
const ResetPassword = lazy(() => import("./pages/ResetPassword"));
const VerifyEmail = lazy(() => import("./pages/VerifyEmail"));
const AccountSecurity = lazy(() => import("./pages/AccountSecurity"));
const Addresses = lazy(() => import("./pages/Addresses"));
const SavedDesigns = lazy(() => import("./pages/SavedDesigns"));
const ProofReview = lazy(() => import("./pages/ProofReview"));

const AdminRoutes = lazy(() => import("./admin/app/AdminRoutes.jsx"));

function App() {
  const location = useLocation();
  const isAdminRoute = location.pathname.startsWith("/admin");
  // Route metadata is owned by Seo so product metadata stays current.

  return (
    <StoreProvider>
      <div
        className={`app-container ${isAdminRoute ? "admin-app" : "storefront"}`}
      >
        <Seo />
        {isAdminRoute && (
          <a
            className="s-skip"
            href="#store-main"
            onClick={(e) => {
              e.preventDefault();
              document.getElementById("store-main")?.focus();
            }}
          >
            Skip to main content
          </a>
        )}
        {!isAdminRoute && <Navbar />}
        <main
          id="store-main"
          tabIndex="-1"
          className={isAdminRoute ? "admin-main-content" : "main-content"}
        >
          <ErrorBoundary
            key={
              /^\/custom(?:\/\d+)?$/.test(location.pathname)
                ? "/custom"
                : location.pathname
            }
          >
            <Suspense fallback={<Skeleton />}>
              <Routes>
                <Route path="/" element={<Home />} />
                <Route path="/products" element={<Products />} />
                <Route path="/custom/:id?" element={<CustomBuilder />} />
                <Route
                  path="/wishlist"
                  element={
                    <ProtectedRoute>
                      <Wishlist />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/cart"
                  element={
                    <ProtectedRoute>
                      <Cart />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/checkout"
                  element={
                    <ProtectedRoute>
                      <Checkout />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/checkout/payment/:outcome"
                  element={
                    <ProtectedRoute>
                      <PaymentReturn />
                    </ProtectedRoute>
                  }
                />
                <Route
                  element={
                    <ProtectedRoute>
                      <AccountLayout />
                    </ProtectedRoute>
                  }
                >
                  <Route path="/profile" element={<Profile />} />
                  <Route
                    path="/account/security"
                    element={<AccountSecurity />}
                  />
                  <Route path="/addresses" element={<Addresses />} />
                  <Route path="/orders" element={<Orders />} />
                  <Route path="/orders/:id" element={<OrderDetails />} />
                  <Route path="/account/designs" element={<SavedDesigns />} />
                  <Route
                    path="/account/designs/:id/proof"
                    element={<ProofReview />}
                  />
                </Route>
                <Route
                  path="/account"
                  element={<Navigate to="/profile" replace />}
                />
                <Route
                  path="/account/profile"
                  element={<Navigate to="/profile" replace />}
                />
                <Route
                  path="/account/addresses"
                  element={<Navigate to="/addresses" replace />}
                />
                <Route
                  path="/account/orders"
                  element={<Navigate to="/orders" replace />}
                />
                <Route path="/about" element={<ContentPage page="about" />} />
                <Route
                  path="/contact"
                  element={<ContentPage page="contact" />}
                />
                <Route path="/pages/:slug" element={<ContentPage />} />
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />
                <Route path="/forgot-password" element={<ForgotPassword />} />
                <Route path="/reset-password" element={<ResetPassword />} />
                <Route path="/verify-email" element={<VerifyEmail />} />
                <Route path="/products/:id" element={<ProductDetails />} />

                <Route path="/admin/*" element={<AdminRoutes />} />
                <Route path="*" element={<NotFound />} />
              </Routes>
            </Suspense>
          </ErrorBoundary>
        </main>
        {!isAdminRoute && <Footer />}
      </div>
    </StoreProvider>
  );
}

export default App;
