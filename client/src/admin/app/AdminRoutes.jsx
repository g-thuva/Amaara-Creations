import { lazy } from "react";
import { Routes, Route } from "react-router-dom";
import { AdminRouteGuard } from "./AdminRouteGuard.jsx";
import { AdminLanding } from "./AdminLanding.jsx";
import { adminNavigation } from "./adminNavigation.js";
import AdminLayout from "../layout/AdminLayout.jsx";
import NotFound from "../../pages/NotFound";
const pages = {
  dashboard: lazy(() => import("../features/dashboard/DashboardPage.jsx")),
  products: lazy(() => import("../features/products/ProductsPage.jsx")),
  categories: lazy(() => import("../features/categories/CategoriesPage.jsx")),
  collections: lazy(
    () => import("../features/collections/CollectionsPage.jsx"),
  ),
  media: lazy(() => import("../features/media/MediaPage.jsx")),
  content: lazy(() => import("../features/content/ContentPage.jsx")),
  settings: lazy(() => import("../features/settings/SettingsPage.jsx")),
  reviews: lazy(() => import("../features/reviews/ReviewsPage.jsx")),
  customers: lazy(() => import("../features/customers/CustomersPage.jsx")),
  "customers/:id": lazy(
    () => import("../features/customers/CustomerDetailPage.jsx"),
  ),
  inventory: lazy(() => import("../features/inventory/InventoryPage.jsx")),
  payments: lazy(() => import("../features/payments/PaymentsPage.jsx")),
  promotions: lazy(() => import("../features/promotions/PromotionsPage.jsx")),
  reports: lazy(() => import("../features/reports/ReportsPage.jsx")),
  audit: lazy(() => import("../features/audit/AuditPage.jsx")),
  staff: lazy(() => import("../features/staff/StaffPage.jsx")),
  orders: lazy(() => import("../features/orders/OrdersPage.jsx")),
  "orders/:id": lazy(() => import("../features/orders/OrderDetailPage.jsx")),
  shipping: lazy(() => import("../features/shipping/ShippingPage.jsx")),
  "custom-builder": lazy(
    () =>
      import("../features/builder-configuration/BuilderConfigurationPage.jsx"),
  ),
  "custom-designs": lazy(
    () => import("../features/production/ProductionQueuePage.jsx"),
  ),
  "custom-designs/:id": lazy(
    () => import("../features/production/ProductionJobPage.jsx"),
  ),
};
export default function AdminRoutes() {
  return (
    <Routes>
      <Route
        element={
          <AdminRouteGuard>
            <AdminLayout />
          </AdminRouteGuard>
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
