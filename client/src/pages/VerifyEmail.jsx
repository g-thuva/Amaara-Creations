import React, { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authApi } from '../services/authApi';
import './Auth.css';

const VerifyEmail = () => {
  const [searchParams] = useSearchParams();
  const userId = useMemo(() => searchParams.get('userId') || '', [searchParams]);
  const token = useMemo(() => searchParams.get('token') || '', [searchParams]);
  const email = useMemo(() => searchParams.get('email') || '', [searchParams]);
  const [status, setStatus] = useState('loading');
  const [message, setMessage] = useState('Verifying your email...');

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

  const resend = async () => {
    if (!email) return;
    const response = await authApi.resendConfirmation(email);
    setMessage(response.message);
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        <div className="auth-header">
          <h1 className="auth-title">{status === 'success' ? 'Email Verified' : 'Verify Email'}</h1>
          <p className="auth-subtitle">{message}</p>
        </div>
        {status === 'failed' && email && <button className="btn-auth" onClick={resend}>Resend Verification</button>}
        <div className="auth-footer"><Link to="/login" className="auth-link">Go to login</Link></div>
      </div>
    </div>
  );
};

export default VerifyEmail;
