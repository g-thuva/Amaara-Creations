import { Outlet } from "react-router-dom";
import { useAdminNavigation } from "./useAdminNavigation.js";
import { AdminSidebar } from "./AdminSidebar.jsx";
import { AdminHeader } from "./AdminHeader.jsx";
import "../shared/styles/adminTokens.css";
import "../shared/styles/adminBase.css";
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
            <Outlet />
          </div>
        </div>
      </div>
    </div>
  );
}
