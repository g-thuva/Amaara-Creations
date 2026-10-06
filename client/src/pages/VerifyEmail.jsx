import React, { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authApi } from '../services/authApi';
import './Auth.css';
import AuthShell from '../components/storefront/AuthShell';

const VerifyEmail = () => {
  const [searchParams] = useSearchParams();
  const userId = useMemo(() => searchParams.get('userId') || '', [searchParams]);
  const token = useMemo(() => searchParams.get('token') || '', [searchParams]);
  const email = useMemo(() => searchParams.get('email') || '', [searchParams]);
  const [status, setStatus] = useState('loading');
  const [message, setMessage] = useState('Verifying your email...');
  const [resending, setResending] = useState(false);
  const [resendEmail, setResendEmail] = useState(email);

  useEffect(() => {
    const verify = async () => {
      if (!userId || !token) {
        setStatus('failed');
        setMessage('Invalid verification link.');
        return;
      }

      try {
        const response = await authApi.confirmEmail({ userId, token });
        setStatus('success');
        setMessage(response.message);
      } catch (err) {
        setStatus('failed');
        setMessage(err.response?.data?.message || 'Verification link is invalid or expired.');
      }
    };

    verify();
  }, [userId, token]);

  const resend = async (event) => {
    event.preventDefault();
    if (!resendEmail || resending) return;
    setResending(true);
    try { const response = await authApi.resendConfirmation(resendEmail); setMessage(response.message); }
    catch { setMessage('Unable to resend right now. Please try again.'); }
    finally { setResending(false); }
  };

  return (
    <AuthShell>
      <div className="auth-card">
        <div className="auth-header">
          <h1 className="auth-title">{status === 'success' ? 'Email Verified' : 'Verify Email'}</h1>
          <p className="auth-subtitle">{message}</p>
        </div>
        {status === 'failed' && <form className="auth-form" onSubmit={resend}><label htmlFor="resend-email">Email address</label><input id="resend-email" type="email" autoComplete="email" required value={resendEmail} onChange={e => setResendEmail(e.target.value)}/><button className="btn-auth" disabled={resending}>{resending ? 'Sending…' : 'Resend verification'}</button></form>}
        <div className="auth-footer"><Link to="/login" className="auth-link">Go to login</Link></div>
      </div>
    </AuthShell>
  );
};

export default VerifyEmail;
