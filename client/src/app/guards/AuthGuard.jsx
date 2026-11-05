import SessionUnavailable from '@/features/auth/components/SessionUnavailable.jsx';
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";
import { acceptsIdentity } from '@/features/auth/sessionIdentity.js';

const AuthGuard = ({ children, management = false }) => {
  const { authStatus, isAuthenticated, user } = useAuth();
  const location = useLocation();

  if (["loading", "authenticating", "signing-out"].includes(authStatus)) {
    return (
      <div style={{ minHeight: "50vh", display: "grid", placeItems: "center" }}>
        <p>Restoring session...</p>
      </div>
    );
  }

  if (['unavailable', 'logout-error'].includes(authStatus)) return <SessionUnavailable />;

  if (!isAuthenticated || !acceptsIdentity(user, management)) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: location.pathname + location.search, message: authStatus === 'expired' ? 'Your session has expired. Please sign in again.' : undefined }}
      />
    );
  }

  return children;
};

export default AuthGuard;
