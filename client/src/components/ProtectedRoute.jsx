import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { can, adminRoutes } from '../utils/permissions';

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

  const routePermission = adminRoutes[location.pathname.split('/')[2]];
  if (requireAdmin && (!can(user, 'AdminAccess') || routePermission && !can(user, routePermission))) {
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
