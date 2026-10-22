import { useEffect, useMemo, useRef, useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { authApi } from '@/features/auth/authApi.js';

export function useVerifyEmail() {
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const navigate = useNavigate();

  const initialEmail = useMemo(() => {
    return searchParams.get('email') || location.state?.email || '';
  }, [searchParams, location.state]);

  const userId = useMemo(() => searchParams.get('userId') || '', [searchParams]);
  const urlToken = useMemo(() => searchParams.get('token') || searchParams.get('code') || '', [searchParams]);

  const [email, setEmail] = useState(initialEmail);
  const [code, setCode] = useState(urlToken && urlToken.length === 6 ? urlToken : '');
  const [status, setStatus] = useState('idle'); // idle | verifying | success | failed
  const [message, setMessage] = useState(location.state?.message || '');
  const [error, setError] = useState('');
  const [resending, setResending] = useState(false);
  const [resendSuccess, setResendSuccess] = useState('');
  const [cooldown, setCooldown] = useState(0);

  const autoVerifyAttempted = useRef(false);

  // If initialEmail changes from navigation, update state
  useEffect(() => {
    if (initialEmail && !email) {
      setEmail(initialEmail);
    }
  }, [initialEmail, email]);

  // Handle countdown timer for resend
  useEffect(() => {
    if (cooldown <= 0) return;
    const timer = setInterval(() => {
      setCooldown((prev) => Math.max(0, prev - 1));
    }, 1000);
    return () => clearInterval(timer);
  }, [cooldown]);

  // Auto-verify legacy link if both userId and full token were provided in URL
  useEffect(() => {
    if (!autoVerifyAttempted.current && userId && urlToken && urlToken.length > 6) {
      autoVerifyAttempted.current = true;
      const autoVerify = async () => {
        setStatus('verifying');
        setError('');
        try {
          const res = await authApi.confirmEmail({ userId, token: urlToken });
          setStatus('success');
          setMessage(res.message || 'Email verified successfully! Redirecting to login...');
          setTimeout(() => {
            navigate('/login', { state: { message: 'Your email has been verified. You can now sign in.' } });
          }, 2000);
        } catch (err) {
          setStatus('failed');
          setError(err.response?.data?.message || 'Verification link is invalid or expired.');
        }
      };
      autoVerify();
    }
  }, [userId, urlToken, navigate]);

  const handleVerify = async (e) => {
    if (e) e.preventDefault();
    setError('');
    setResendSuccess('');

    const cleanEmail = email.trim();
    const cleanCode = code.trim().replace(/\s+/g, '');

    if (!cleanEmail) {
      setError('Please enter your email address.');
      return;
    }

    if (!cleanCode || cleanCode.length !== 6 || !/^\d{6}$/.test(cleanCode)) {
      setError('Please enter a valid 6-digit verification code.');
      return;
    }

    setStatus('verifying');

    try {
      const response = await authApi.confirmEmail({
        email: cleanEmail,
        code: cleanCode,
        ...(userId ? { userId } : {})
      });

      setStatus('success');
      setMessage(response.message || 'Email verified successfully! Redirecting to login...');

      setTimeout(() => {
        navigate('/login', {
          state: { message: 'Your email has been verified. You can now sign in.' }
        });
      }, 2000);
    } catch (err) {
      setStatus('failed');
      setError(err.response?.data?.message || 'Invalid or expired verification code.');
    }
  };

  const handleResend = async (e) => {
    if (e) e.preventDefault();
    const cleanEmail = email.trim();
    if (!cleanEmail || resending || cooldown > 0) return;

    setResending(true);
    setError('');
    setResendSuccess('');

    try {
      const response = await authApi.resendConfirmation(cleanEmail);
      setResendSuccess(response.message || 'A new 6-digit verification code has been sent to your email.');
      setCooldown(60); // 60s cooldown
    } catch (err) {
      setError(err.response?.data?.message || 'Unable to resend verification code right now. Please try again later.');
    } finally {
      setResending(false);
    }
  };

  const handleCodeChange = (val) => {
    // Only accept numeric digits up to 6 characters
    const numeric = val.replace(/\D/g, '').slice(0, 6);
    setCode(numeric);
    if (error) setError('');
  };

  return {
    email,
    setEmail,
    code,
    handleCodeChange,
    status,
    message,
    error,
    resending,
    resendSuccess,
    cooldown,
    handleVerify,
    handleResend
  };
}
