import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../services/authApi';
import './Auth.css';
import AuthShell from '../components/storefront/AuthShell';

const ForgotPassword = () => {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setMessage('');
    setError('');
    setIsLoading(true);

    try {
      const response = await authApi.forgotPassword(email);
      setMessage(response.message);
    } catch {
      setError('Unable to process the request. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthShell>
      <div className="auth-card">
        <div className="auth-header">
          <h1 className="auth-title">Reset Password</h1>
          <p className="auth-subtitle">Enter your email and we will send reset instructions.</p>
        </div>
        {message && <div className="success-message" role="status">{message}</div>}
        {error && <div className="error-message" role="alert" id="form-error">{error}</div>}
        <form aria-describedby={error ? "form-error" : undefined} className="auth-form" onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="email" className="form-label">Email Address</label>
            <input id="email" type="email" className="form-control" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>
          <button type="submit" className="btn-auth" disabled={isLoading}>{isLoading ? 'Sending...' : 'Send Instructions'}</button>
        </form>
        <div className="auth-footer"><Link to="/login" className="auth-link">Return to login</Link></div>
      </div>
    </AuthShell>
  );
};

export default ForgotPassword;
