import { Outlet } from "react-router-dom";
import { Suspense } from "react";
import { useAdminNavigation } from "@/features/admin/layout/useAdminNavigation.js";
import { AdminSidebar } from "@/features/admin/layout/AdminSidebar.jsx";
import { AdminHeader } from "@/features/admin/layout/AdminHeader.jsx";
import "@/features/admin/shared/styles/adminTokens.css";
import "@/features/admin/shared/styles/adminBase.css";
export default function AdminLayout() {
  const navigation = useAdminNavigation();
  return (
    <div className="admin-container">
      <AdminSidebar navigation={navigation} />
      <div
        className={`admin-main ${navigation.sidebarOpen ? "sidebar-open" : ""}`}
      >
        <AdminHeader navigation={navigation} />
        <div className="admin-content">
          <div>
            <Suspense fallback={<p role="status">Loading page...</p>}>
              <Outlet />
            </Suspense>
          </div>
        </div>
      </div>
    </div>
  );
}
