import { useEffect, useRef, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { customerError } from '@/shared/api/apiError.ts';
import { loginDestination } from '@/features/auth/loginDestination.js';
import { AUTH_BASE_PATH } from '@/shared/api/requestConfig.js';

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
      // Password and pending verification stay here until the server issues a session.
      if (!response.token) return;
      // StaffApp waits for authoritative permissions before choosing the existing
      // dashboard or specialist route. Stay mounted while that check completes.
      if (AUTH_BASE_PATH === '/auth/staff') return;
      const destination = loginDestination(response.user, location.state?.from);
      if (!destination) { setError('Your sign-in was verified, but your workspace address is unavailable. Contact the operator.'); return; }
      if (destination.external) window.location.replace(destination.url);
      else navigate(destination.url, { replace: true });
    } catch (failure) {
      const code = failure.response?.data?.code;
      const messages = {
        MFA_CODE_INVALID: "That code didn't work. Check your authenticator app and try the current code.",
        MFA_CHALLENGE_EXPIRED: 'Your sign-in expired or changed. Please enter your email and password again.',
        MANAGEMENT_DISABLED: 'Staff sign-in is unavailable in this environment. Contact the operator.',
        ACCOUNT_ACCESS_DENIED: 'This account is not available for sign-in. Contact the operator.',
        MANAGEMENT_ACCESS_DENIED: 'This account cannot use staff sign-in. Customer accounts must use the customer sign-in page.',
        EMAIL_CONFIRMATION_REQUIRED: 'Verify your email before signing in. Check your inbox for the confirmation email.',
      };
      setError(messages[code] || (failure.response?.status === 401
        ? 'Unable to sign in. Check your email and password, and confirm your email before trying again.'
        : customerError(failure)));
    } finally { busy.current = false; }
  }
  const handleLogin = event => { event.preventDefault(); return run(() => login({ email, password })); };
  const verify = values => run(() => advanceSignIn(challenge?.status === 'enroll' ? 'confirm' : 'verify', values));
  const finish = () => run(() => advanceSignIn('complete', { savedRecoveryCodes: true }));
  const cancel = () => run(async () => { try { return await advanceSignIn('cancel'); } catch (failure) { if (failure.response?.status !== 401) throw failure; return { status: 'credentials' }; } });
  return { email, setEmail, password, setPassword, error, notice, isLoading, message: location.state?.message, challenge, handleLogin, verify, finish, cancel };
}
