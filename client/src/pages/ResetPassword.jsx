import React, { useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authApi } from '../services/authApi';
import './Auth.css';
import AuthShell from '../components/storefront/AuthShell';

const ResetPassword = () => {
  const [searchParams] = useSearchParams();
  const email = useMemo(() => searchParams.get('email') || '', [searchParams]);
  const token = useMemo(() => searchParams.get('token') || '', [searchParams]);
  const [form, setForm] = useState({ newPassword: '', confirmPassword: '' });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setMessage('');
    setError('');

    if (!email || !token) {
      setError('Invalid password reset link.');
      return;
    }

    if (form.newPassword !== form.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setIsLoading(true);
    try {
      const response = await authApi.resetPassword({ email, token, ...form });
      setMessage(response.message);
    } catch (err) {
      setError(err.response?.data?.message || 'Invalid or expired password reset link.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthShell>
      <div className="auth-card">
        <div className="auth-header">
          <h1 className="auth-title">Choose New Password</h1>
          <p className="auth-subtitle">Create a new password for your account.</p>
        </div>
        {message && <div className="success-message" role="status">{message}</div>}
        {error && <div className="error-message" role="alert" id="form-error">{error}</div>}
        <form aria-describedby={error ? "form-error" : undefined} className="auth-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="newPassword" className="form-label">New Password</label>
            <input id="newPassword" type="password" className="form-control" required minLength="6" value={form.newPassword} onChange={(e) => setForm({ ...form, newPassword: e.target.value })} />
          </div>
          <div className="form-group">
            <label htmlFor="confirmPassword" className="form-label">Confirm Password</label>
            <input id="confirmPassword" type="password" className="form-control" required minLength="6" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} />
          </div>
          <button type="submit" className="btn-auth" disabled={isLoading}>{isLoading ? 'Resetting...' : 'Reset Password'}</button>
        </form>
        <div className="auth-footer"><Link to="/login" className="auth-link">Sign in</Link></div>
      </div>
    </AuthShell>
  );
};

export default ResetPassword;
