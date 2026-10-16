import { lazy } from 'react';
import HomePage from '@/features/home/pages/HomePage.jsx';
import ContentPage from '@/features/content/pages/ContentPage.jsx';

const ProductsPage = lazy(() => import('@/features/catalog/pages/ProductsPage.jsx'));
const ProductDetailsPage = lazy(() => import('@/features/catalog/pages/ProductDetailsPage.jsx'));
const CustomBuilderPage = lazy(() => import('@/features/custom-design/pages/CustomBuilderPage.jsx'));
const LoginPage = lazy(() => import('@/features/auth/pages/LoginPage.jsx'));
const RegisterPage = lazy(() => import('@/features/auth/pages/RegisterPage.jsx'));
const ForgotPasswordPage = lazy(() => import('@/features/auth/pages/ForgotPasswordPage.jsx'));
const ResetPasswordPage = lazy(() => import('@/features/auth/pages/ResetPasswordPage.jsx'));
const VerifyEmailPage = lazy(() => import('@/features/auth/pages/VerifyEmailPage.jsx'));

export const publicRoutes = [
  { path: '/', element: <HomePage /> },
  { path: '/products', element: <ProductsPage /> },
  { path: '/products/:id', element: <ProductDetailsPage /> },
  { path: '/custom/:id?', element: <CustomBuilderPage /> },
  { path: '/about', element: <ContentPage page="about" /> },
  { path: '/contact', element: <ContentPage page="contact" /> },
  { path: '/pages/:slug', element: <ContentPage /> },
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  { path: '/forgot-password', element: <ForgotPasswordPage /> },
  { path: '/reset-password', element: <ResetPasswordPage /> },
  { path: '/verify-email', element: <VerifyEmailPage /> },
];
