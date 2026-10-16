import { Navigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import { adminStart } from "./adminPermissions.js";
export function AdminLanding() {
  const { user } = useAuth();
  return <Navigate to={adminStart(user)} replace />;
}
