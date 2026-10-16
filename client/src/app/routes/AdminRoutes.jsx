import { lazy } from "react";
import { Routes, Route } from "react-router-dom";
import { AdminGuard } from "@/app/guards/AdminGuard.jsx";
import { AdminLanding } from "@/features/admin/app/AdminLanding.jsx";
import { adminNavigation } from "@/features/admin/app/adminNavigation.js";
import AdminLayout from "@/features/admin/layout/AdminLayout.jsx";
import NotFound from "@/app/pages/NotFoundPage.jsx";
const pages = {
  dashboard: lazy(() => import("@/features/admin/features/dashboard/DashboardPage.jsx")),
  products: lazy(() => import("@/features/admin/features/products/ProductsPage.jsx")),
  categories: lazy(() => import("@/features/admin/features/categories/CategoriesPage.jsx")),
  collections: lazy(
    () => import("@/features/admin/features/collections/CollectionsPage.jsx"),
  ),
  media: lazy(() => import("@/features/admin/features/media/MediaPage.jsx")),
  content: lazy(() => import("@/features/admin/features/content/ContentPage.jsx")),
  settings: lazy(() => import("@/features/admin/features/settings/SettingsPage.jsx")),
  reviews: lazy(() => import("@/features/admin/features/reviews/ReviewsPage.jsx")),
  customers: lazy(() => import("@/features/admin/features/customers/CustomersPage.jsx")),
  "customers/:id": lazy(
    () => import("@/features/admin/features/customers/CustomerDetailPage.jsx"),
  ),
  inventory: lazy(() => import("@/features/admin/features/inventory/InventoryPage.jsx")),
  payments: lazy(() => import("@/features/admin/features/payments/PaymentsPage.jsx")),
  promotions: lazy(() => import("@/features/admin/features/promotions/PromotionsPage.jsx")),
  reports: lazy(() => import("@/features/admin/features/reports/ReportsPage.jsx")),
  audit: lazy(() => import("@/features/admin/features/audit/AuditPage.jsx")),
  staff: lazy(() => import("@/features/admin/features/staff/StaffPage.jsx")),
  orders: lazy(() => import("@/features/admin/features/orders/OrdersPage.jsx")),
  "orders/:id": lazy(() => import("@/features/admin/features/orders/OrderDetailPage.jsx")),
  shipping: lazy(() => import("@/features/admin/features/shipping/ShippingPage.jsx")),
  "custom-builder": lazy(
    () =>
      import("@/features/admin/features/builder-configuration/BuilderConfigurationPage.jsx"),
  ),
  "custom-designs": lazy(
    () => import("@/features/admin/features/production/ProductionQueuePage.jsx"),
  ),
  "custom-designs/:id": lazy(
    () => import("@/features/admin/features/production/ProductionJobPage.jsx"),
  ),
};
export default function AdminRoutes() {
  return (
    <Routes>
      <Route
        element={
          <AdminGuard>
            <AdminLayout />
          </AdminGuard>
        }
      >
        <Route index element={<AdminLanding />} />
        {adminNavigation.map((route) => {
          const Page = pages[route.path];
          return (
            <Route key={route.path} path={route.path} element={<Page />} />
          );
        })}
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  );
}
