import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

const hasAdminAccess = (user) => {
  const roles = user?.roles || (user?.role ? [user.role] : []);
  return roles.includes('Admin') || roles.includes('SuperAdmin');
};

const ProtectedRoute = ({ children, requireAdmin = false }) => {
  const { authStatus, isAuthenticated, user } = useAuth();
  const location = useLocation();

  if (authStatus === 'loading') {
    return (
      <div style={{ minHeight: '50vh', display: 'grid', placeItems: 'center' }}>
        <p>Restoring session...</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  }

  if (requireAdmin && !hasAdminAccess(user)) {
    return (
      <div style={{ minHeight: '50vh', display: 'grid', placeItems: 'center', padding: '2rem', textAlign: 'center' }}>
        <div>
          <h1>Access denied</h1>
          <p>You do not have permission to access the admin area.</p>
        </div>
      </div>
    );
  }

  return children;
};

export default ProtectedRoute;
