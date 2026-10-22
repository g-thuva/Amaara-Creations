import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { authApi } from '@/features/auth/authApi.js';
import { passwordPolicyError } from '@/shared/utils/passwordPolicy.js';

export function useResetPassword() {
  const [searchParams] = useSearchParams();
  const location = useLocation();
  const navigate = useNavigate();

  const initialEmail = useMemo(() => {
    return searchParams.get('email') || location.state?.email || '';
  }, [searchParams, location.state]);

  const initialCode = useMemo(() => {
    const raw = searchParams.get('code') || searchParams.get('token') || '';
    return raw.trim();
  }, [searchParams]);

  const [email, setEmail] = useState(initialEmail);
  const [code, setCode] = useState(initialCode);
  const [form, setForm] = useState({ newPassword: '', confirmPassword: '' });
  const [status, setStatus] = useState('idle'); // idle | loading | success
  const [message, setMessage] = useState(location.state?.message || '');
  const [error, setError] = useState('');
  const [resending, setResending] = useState(false);
  const [resendSuccess, setResendSuccess] = useState('');
  const [cooldown, setCooldown] = useState(0);

  useEffect(() => {
    if (initialEmail && !email) {
      setEmail(initialEmail);
    }
  }, [initialEmail, email]);

  useEffect(() => {
    if (initialCode && !code) {
      setCode(initialCode);
    }
  }, [initialCode, code]);

  useEffect(() => {
    if (cooldown <= 0) return;
    const timer = setInterval(() => {
      setCooldown((prev) => Math.max(0, prev - 1));
    }, 1000);
    return () => clearInterval(timer);
  }, [cooldown]);

  const handleCodeChange = (val) => {
    const numeric = val.replace(/\D/g, '').slice(0, 6);
    setCode(numeric);
    if (error) setError('');
  };

  const handleSubmit = async (event) => {
    if (event) event.preventDefault();
    setMessage('');
    setError('');
    setResendSuccess('');

    const cleanEmail = email.trim();
    const cleanCode = code.trim();

    if (!cleanEmail) {
      setError('Please enter your email address.');
      return;
    }

    if (!cleanCode || cleanCode.length !== 6 || !/^\d{6}$/.test(cleanCode)) {
      setError('Please enter the 6-digit password reset code sent to your email.');
      return;
    }

    if (!form.newPassword) {
      setError('Please enter a new password.');
      return;
    }

    if (form.newPassword !== form.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    const policyError = passwordPolicyError(form.newPassword);
    if (policyError) {
      setError(policyError);
      return;
    }

    setStatus('loading');

    try {
      const response = await authApi.resetPassword({
        email: cleanEmail,
        code: cleanCode,
        token: cleanCode,
        newPassword: form.newPassword,
        confirmPassword: form.confirmPassword
      });

      setStatus('success');
      setMessage(response.message || 'Password reset successfully! Redirecting to login...');

      setTimeout(() => {
        navigate('/login', {
          state: { message: 'Your password has been reset successfully. Please sign in with your new password.' }
        });
      }, 2000);
    } catch (err) {
      setStatus('idle');
      setError(err.response?.data?.message || 'Invalid or expired password reset code.');
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
      const response = await authApi.forgotPassword(cleanEmail);
      setResendSuccess(response.message || 'A new 6-digit password reset code has been sent to your email.');
      setCooldown(60);
    } catch (err) {
      setError(err.response?.data?.message || 'Unable to resend code right now. Please try again later.');
    } finally {
      setResending(false);
    }
  };

  return {
    email,
    setEmail,
    code,
    handleCodeChange,
    form,
    setForm,
    status,
    message,
    error,
    resending,
    resendSuccess,
    cooldown,
    handleSubmit,
    handleResend
  };
}
