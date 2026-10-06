import { Routes, Route, Navigate, useLocation } from "react-router-dom";
import { lazy, Suspense, useEffect } from 'react';
import { StoreProvider } from './contexts/StoreContext';
import AccountLayout from './components/storefront/AccountLayout';
import ErrorBoundary from './components/storefront/ErrorBoundary';
import { Skeleton } from './components/storefront/UI';
import ContentPage from './pages/ContentPage';
import NotFound from './pages/NotFound';
import Navbar from "./components/Navbar";
import Footer from "./components/Footer";
import "./App.css";
const Home = lazy(() => import("./pages/Home"));
const Products = lazy(() => import("./pages/Products"));
const CustomBuilder = lazy(() => import("./pages/CustomBuilder"));
const Wishlist = lazy(() => import("./pages/Wishlisst"));
const Cart = lazy(() => import("./pages/Cart"));
const Checkout = lazy(() => import("./pages/Checkout"));
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


//admin
const AdminLayout = lazy(() => import("./pages/admin/AdminLayout"));
const Dashboard = lazy(() => import("./pages/admin/Dashboard"));
const AdProducts = lazy(() => import("./pages/admin/AdProducts"));
const Reviews = lazy(() => import("./pages/admin/Reviews"));
const Customers = lazy(() => import("./pages/admin/Customers"));
const AdOrders = lazy(() => import("./pages/admin/AdOrders"));
const Categories = lazy(() => import("./pages/admin/Categories"));
const Collections = lazy(() => import("./pages/admin/Collections"));
const Media = lazy(() => import("./pages/admin/Media"));
const Content = lazy(() => import("./pages/admin/Content"));
const Settings = lazy(() => import("./pages/admin/Settings"));
const AdminCustomBuilder = lazy(() => import("./pages/admin/CustomBuilderConfig"));
const AdminCustomDesigns = lazy(() => import("./pages/admin/CustomDesigns"));
const AdminCustomDesignDetail = lazy(() => import("./pages/admin/CustomDesignDetail"));


function App() {
  const location = useLocation();
  const isAdminRoute = location.pathname.startsWith('/admin');
  useEffect(() => {
    if (isAdminRoute) return;
    const titles = { '/': 'Custom stickers & decals', '/products': 'Shop stickers & decals', '/custom': 'Custom sticker builder', '/login': 'Sign in', '/register': 'Create account', '/about': 'About Amaara', '/contact': 'Contact Amaara', '/cart': 'Shopping cart', '/wishlist': 'Wishlist', '/profile': 'Your profile', '/addresses': 'Your addresses', '/orders': 'Your orders', '/account/designs': 'Saved designs' };
    const descriptions = { '/': 'Custom stickers, decals and personalised printed details from Amaara Creations.', '/products': 'Browse stickers, decals and printed products currently available from Amaara Creations.', '/custom': 'Preview custom sticker text, type and proportions before contacting Amaara Creations.' };
    document.title = `${titles[location.pathname] || 'Amaara Creations'} | Amaara Creations`;
    document.querySelector('meta[name="description"]')?.setAttribute('content', descriptions[location.pathname] || 'Explore Amaara Creations products, custom sticker previews and customer account services.');
    window.scrollTo({ top: 0, behavior: 'instant' });
  }, [location.pathname, isAdminRoute]);

  return (
    <StoreProvider><div className={`app-container ${isAdminRoute ? 'admin-app' : 'storefront'}`}>
      {!isAdminRoute && <Navbar />}
      <main id="store-main" tabIndex="-1" className={isAdminRoute ? "admin-main-content" : "main-content"}>
        <ErrorBoundary key={location.pathname}><Suspense fallback={<Skeleton/>}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/products" element={<Products />} />
          <Route path="/custom" element={<CustomBuilder />} />
          <Route path="/custom/:id" element={<CustomBuilder />} />
          <Route path="/wishlist" element={<ProtectedRoute><Wishlist /></ProtectedRoute>} />
          <Route path="/cart" element={<ProtectedRoute><Cart /></ProtectedRoute>} />
          <Route path="/checkout" element={<ProtectedRoute><Checkout /></ProtectedRoute>} />
          <Route element={<ProtectedRoute><AccountLayout /></ProtectedRoute>}>
            <Route path="/profile" element={<Profile/>}/>
            <Route path="/account/security" element={<AccountSecurity/>}/>
            <Route path="/addresses" element={<Addresses/>}/>
            <Route path="/orders" element={<Orders/>}/>
            <Route path="/orders/:id" element={<OrderDetails/>}/>
            <Route path="/account/designs" element={<SavedDesigns/>}/>
            <Route path="/account/designs/:id/proof" element={<ProofReview/>}/>
          </Route>
          <Route path="/account" element={<Navigate to="/profile" replace/>}/>
          <Route path="/account/profile" element={<Navigate to="/profile" replace/>}/>
          <Route path="/account/addresses" element={<Navigate to="/addresses" replace/>}/>
          <Route path="/account/orders" element={<Navigate to="/orders" replace/>}/>
          <Route path="/about" element={<ContentPage page="about"/>}/>
          <Route path="/contact" element={<ContentPage page="contact"/>}/>
          <Route path="/pages/:slug" element={<ContentPage/>}/>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          <Route path="/verify-email" element={<VerifyEmail />} />
          <Route path="/products/:id" element={<ProductDetails />} />

          <Route path="/admin" element={<ProtectedRoute requireAdmin><AdminLayout /></ProtectedRoute>}>
            <Route index element={<Navigate to="/admin/dashboard" replace />} />
            <Route path="dashboard" element={<Dashboard />} />
            <Route path="products" element={<AdProducts />} />
            <Route path="categories" element={<Categories />} />
            <Route path="collections" element={<Collections />} />
            <Route path="media" element={<Media />} />
            <Route path="content" element={<Content />} />
            <Route path="settings" element={<Settings />} />
            <Route path="reviews" element={<Reviews />} />
            <Route path="customers" element={<Customers />} />
            <Route path="orders" element={<AdOrders />} />
            <Route path="custom-builder" element={<AdminCustomBuilder />} />
            <Route path="custom-designs" element={<AdminCustomDesigns />} />
            <Route path="custom-designs/:id" element={<AdminCustomDesignDetail />} />
          </Route>
          <Route path="*" element={<NotFound/>}/>
        </Routes>
        </Suspense></ErrorBoundary>
      </main>
      {!isAdminRoute && <Footer />}
    </div></StoreProvider>
  );
}

export default App;
