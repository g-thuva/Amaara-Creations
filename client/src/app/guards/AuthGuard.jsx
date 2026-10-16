import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";

const AuthGuard = ({ children }) => {
  const { authStatus, isAuthenticated } = useAuth();
  const location = useLocation();

  if (authStatus === "loading") {
    return (
      <div style={{ minHeight: "50vh", display: "grid", placeItems: "center" }}>
        <p>Restoring session...</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: location.pathname + location.search }}
      />
    );
  }

  return children;
};

export default AuthGuard;
