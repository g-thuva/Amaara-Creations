import { useEffect, useRef, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { customerError } from '@/shared/api/apiError.ts';
import { loginDestination } from '@/features/auth/loginDestination.js';

export function useLogin() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const busy = useRef(false);
  const navigate = useNavigate(); const location = useLocation();
  const { login, advanceSignIn, challenge, authStatus } = useAuth();
  // A slow anonymous session restore must not prevent a new credential entry.
  // AuthProvider invalidates that restore when this explicit sign-in starts.
  const isLoading = authStatus === 'authenticating';
  useEffect(() => {
    if (!challenge) return undefined;
    const timer = setTimeout(() => setNotice('Your sign-in expired. Enter your email and password again.'), Math.max(0, new Date(challenge.expiresAt).getTime() - Date.now()));
    return () => clearTimeout(timer);
  }, [challenge]);
  async function run(operation) {
    if (busy.current) return;
    busy.current = true; setError(''); setNotice('');
    try {
      const response = await operation();
      setPassword('');
      if (!response.token) return;
      const destination = loginDestination(response.user, location.state?.from);
      if (!destination) { setError('Your account was verified, but the management address is unavailable. Contact the operator.'); return; }
      if (destination.external) window.location.replace(destination.url);
      else navigate(destination.url, { replace: true });
    } catch (failure) {
      const code = failure.response?.data?.code;
      setError(code === 'MFA_CODE_INVALID' ? "That code didn't work. Check your authenticator app and try the current code." : code === 'MFA_CHALLENGE_EXPIRED' ? 'Your sign-in expired or changed. Please enter your email and password again.' : code === 'MANAGEMENT_DISABLED' ? 'Staff sign-in is unavailable in this environment. Contact the operator.' : failure.response?.status === 401 ? 'Unable to sign in. Check your email and password, and confirm your email before trying again.' : customerError(failure));
    } finally { busy.current = false; }
  }
  const handleLogin = event => { event.preventDefault(); return run(() => login({ email, password })); };
  const verify = values => run(() => advanceSignIn(challenge?.status === 'enroll' ? 'confirm' : 'verify', values));
  const finish = () => run(() => advanceSignIn('complete', { savedRecoveryCodes: true }));
  const cancel = () => run(async () => { try { return await advanceSignIn('cancel'); } catch (failure) { if (failure.response?.status !== 401) throw failure; return { status: 'credentials' }; } });
  return { email, setEmail, password, setPassword, error, notice, isLoading, message: location.state?.message, challenge, handleLogin, verify, finish, cancel };
}
