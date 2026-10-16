import { Link } from 'react-router-dom';
import { useResetPassword } from '@/features/auth/hooks/useResetPassword.js';

export default function ResetPasswordForm() {
  const { form, setForm, message, error, isLoading, handleSubmit } = useResetPassword();
  return (
    <>
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
    </>
  );
}
