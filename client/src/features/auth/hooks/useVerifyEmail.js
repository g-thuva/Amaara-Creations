import { useEffect, useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { authApi } from '@/features/auth/authApi.js';

export function useVerifyEmail() {
  const [searchParams] = useSearchParams();
  const userId = useMemo(() => searchParams.get('userId') || '', [searchParams]);
  const token = useMemo(() => searchParams.get('token') || '', [searchParams]);
  const email = useMemo(() => searchParams.get('email') || '', [searchParams]);
  const [status, setStatus] = useState('loading');
  const [message, setMessage] = useState('Verifying your email...');
  const [resending, setResending] = useState(false);
  const [resendEmail, setResendEmail] = useState(email);
  const confirmationRequest = useRef(null);

  useEffect(() => {
    let active = true;
    const verify = async () => {
      if (!userId || !token) {
        setStatus('failed');
        setMessage('Invalid verification link.');
        return;
      }

      try {
        // Strict Mode replays effects; confirmation is a mutation, so reuse its request.
        if (confirmationRequest.current?.userId !== userId || confirmationRequest.current?.token !== token) {
          confirmationRequest.current = { userId, token, promise: authApi.confirmEmail({ userId, token }) };
        }
        const response = await confirmationRequest.current.promise;
        if (!active) return;
        setStatus('success');
        setMessage(response.message);
      } catch (err) {
        if (!active) return;
        setStatus('failed');
        setMessage(err.response?.data?.message || 'Verification link is invalid or expired.');
      }
    };

    verify();
    return () => { active = false; };
  }, [userId, token]);

  const resend = async (event) => {
    event.preventDefault();
    if (!resendEmail || resending) return;
    setResending(true);
    try { const response = await authApi.resendConfirmation(resendEmail); setMessage(response.message); }
    catch { setMessage('Unable to resend right now. Please try again.'); }
    finally { setResending(false); }
  };

  return { status, message, resending, resendEmail, setResendEmail, resend };
}
