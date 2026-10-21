import { lazy } from 'react';
import { Navigate } from 'react-router-dom';
import AuthGuard from '@/app/guards/AuthGuard.jsx';
import AccountLayout from '@/features/account/components/AccountLayout.jsx';

const WishlistPage = lazy(() => import('@/features/wishlist/pages/WishlistPage.jsx'));
const CartPage = lazy(() => import('@/features/cart/pages/CartPage.jsx'));
const CheckoutPage = lazy(() => import('@/features/checkout/pages/CheckoutPage.jsx'));
const PaymentReturnPage = lazy(() => import('@/features/payments/pages/PaymentReturnPage.jsx'));
const ProfilePage = lazy(() => import('@/features/account/pages/ProfilePage.jsx'));
const AccountSecurityPage = lazy(() => import('@/features/account/pages/AccountSecurityPage.jsx'));
const CustomerRequestsPage = lazy(() => import('@/features/account/pages/CustomerRequestsPage.jsx'));
const AddressesPage = lazy(() => import('@/features/account/pages/AddressesPage.jsx'));
const OrdersPage = lazy(() => import('@/features/orders/pages/OrdersPage.jsx'));
const OrderDetailsPage = lazy(() => import('@/features/orders/pages/OrderDetailsPage.jsx'));
const SavedDesignsPage = lazy(() => import('@/features/custom-design/pages/SavedDesignsPage.jsx'));
const ProofReviewPage = lazy(() => import('@/features/custom-design/pages/ProofReviewPage.jsx'));

export const accountRoutes = [
  { path: '/wishlist', element: <AuthGuard><WishlistPage /></AuthGuard> },
  { path: '/cart', element: <AuthGuard><CartPage /></AuthGuard> },
  { path: '/checkout', element: <AuthGuard><CheckoutPage /></AuthGuard> },
  { path: '/checkout/payment/:outcome', element: <AuthGuard><PaymentReturnPage /></AuthGuard> },
  { element: <AuthGuard><AccountLayout /></AuthGuard>, children: [
      { path: '/profile', element: <ProfilePage /> },
      { path: '/account/security', element: <AccountSecurityPage /> },
      { path: '/account/requests', element: <CustomerRequestsPage /> },
      { path: '/addresses', element: <AddressesPage /> },
      { path: '/orders', element: <OrdersPage /> },
      { path: '/orders/:id', element: <OrderDetailsPage /> },
      { path: '/account/designs', element: <SavedDesignsPage /> },
      { path: '/account/designs/:id/proof', element: <ProofReviewPage /> },
  ] },
  { path: '/account', element: <Navigate to="/profile" replace /> },
  { path: '/account/profile', element: <Navigate to="/profile" replace /> },
  { path: '/account/addresses', element: <Navigate to="/addresses" replace /> },
  { path: '/account/orders', element: <Navigate to="/orders" replace /> },
];
