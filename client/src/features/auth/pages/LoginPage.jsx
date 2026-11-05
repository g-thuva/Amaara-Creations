import { AUTH_BASE_PATH } from '@/shared/api/requestConfig.js';
import SessionUnavailable from '../components/SessionUnavailable.jsx';
import AuthShell from '@/features/auth/components/AuthShell.jsx';
import LoginForm from '@/features/auth/components/LoginForm.jsx';
import { useAuth } from '@/features/auth/AuthProvider.jsx';

export default function LoginPage() {
  const { challenge, authStatus } = useAuth();
  if (authStatus === 'loading') return <p role="status">Checking your session...</p>;
  if (['unavailable', 'logout-error'].includes(authStatus)) return <SessionUnavailable />;
  return <AuthShell security={Boolean(challenge) || AUTH_BASE_PATH === '/auth/staff'}><LoginForm /></AuthShell>;
}
