import { useLocation } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import ProtectedRoute from "../../components/ProtectedRoute";
import { can, adminRoutes } from "./adminPermissions.js";
export function AdminRouteGuard({ children }) {
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
