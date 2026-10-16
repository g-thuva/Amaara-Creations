import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';

export function useRequireLogin() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  return () => {
    if (isAuthenticated) return true;
    navigate('/login', { state: { from: location.pathname + location.search } });
    return false;
  };
}
