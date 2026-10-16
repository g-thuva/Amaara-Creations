import { Navigate } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";
import { adminStart } from "@/app/config/permissions.js";
export function AdminLanding() {
  const { user } = useAuth();
  return <Navigate to={adminStart(user)} replace />;
}
