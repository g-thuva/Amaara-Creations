import { Link } from 'react-router-dom';
import { useForgotPassword } from '@/features/auth/hooks/useForgotPassword.js';

export default function ForgotPasswordForm() {
  const { email, setEmail, message, error, isLoading, handleSubmit } = useForgotPassword();
  return (
    <>
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
    </>
  );
}
