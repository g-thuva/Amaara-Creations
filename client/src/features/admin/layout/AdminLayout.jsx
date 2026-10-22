import { Outlet } from "react-router-dom";
import { Suspense } from "react";
import { useAdminNavigation } from "@/features/admin/layout/useAdminNavigation.js";
import { AdminSidebar } from "@/features/admin/layout/AdminSidebar.jsx";
import { AdminHeader } from "@/features/admin/layout/AdminHeader.jsx";
import "@/features/admin/shared/styles/adminTokens.css";
import "@/features/admin/shared/styles/adminBase.css";
import { ToastProvider } from '@/features/admin/shared/components/Toast.jsx';
import { Skeleton } from '@/features/admin/shared/components/Skeleton.jsx';
export default function AdminLayout() {
  const navigation = useAdminNavigation();
  return (
    <ToastProvider><div className={`admin-container ${!navigation.sidebarOpen && !navigation.isMobile ? 'is-collapsed' : ''}`}>
      <AdminSidebar navigation={navigation} />
      {navigation.isMobile && navigation.sidebarOpen && <button className="sidebar-backdrop" aria-label="Close admin navigation" tabIndex={-1} onClick={navigation.close} />}
      <div
        className={`admin-main ${navigation.sidebarOpen ? "sidebar-open" : ""}`}
        inert={navigation.isMobile && navigation.sidebarOpen ? true : undefined}
      >
        <AdminHeader navigation={navigation} />
        <div className="admin-content">
          <div>
            <Suspense fallback={<Skeleton label="Loading page..." />}>
              <Outlet />
            </Suspense>
          </div>
        </div>
      </div>
    </div></ToastProvider>
  );
}
