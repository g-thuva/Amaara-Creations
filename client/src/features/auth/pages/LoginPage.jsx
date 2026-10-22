import AuthShell from '@/features/auth/components/AuthShell.jsx';
import LoginForm from '@/features/auth/components/LoginForm.jsx';
import { useAuth } from '@/features/auth/AuthProvider.jsx';

export default function LoginPage() {
  const { challenge } = useAuth();
  return <AuthShell security={Boolean(challenge)}><LoginForm /></AuthShell>;
}
