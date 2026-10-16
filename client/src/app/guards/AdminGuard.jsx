import { useLocation } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";
import ProtectedRoute from "@/app/guards/AuthGuard.jsx";
import { can, adminRoutes } from "@/app/config/permissions.js";
export function AdminGuard({ children }) {
  const { user } = useAuth();
  const { pathname } = useLocation();
  const permission = adminRoutes[pathname.split("/")[2]];
  const allowed =
    can(user, "AdminAccess") && (!permission || can(user, permission));
  return (
    <ProtectedRoute>
      {allowed ? (
        children
      ) : (
        <div
          style={{
            minHeight: "50vh",
            display: "grid",
            placeItems: "center",
            padding: "2rem",
            textAlign: "center",
          }}
        >
          <div>
            <h1>Access denied</h1>
            <p>You do not have permission to access the admin area.</p>
          </div>
        </div>
      )}
    </ProtectedRoute>
  );
}
