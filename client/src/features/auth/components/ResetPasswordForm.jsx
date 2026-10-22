import { PasswordField } from '@/shared/components/ui/PasswordField.jsx';
import { Link } from 'react-router-dom';
import { useResetPassword } from '@/features/auth/hooks/useResetPassword.js';

export default function ResetPasswordForm() {
  const {
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
  } = useResetPassword();

  if (status === 'success') {
    return (
      <div className="auth-card">
        <div className="auth-header" style={{ textAlign: 'center' }}>
          <div style={{
            width: '64px',
            height: '64px',
            borderRadius: '50%',
            background: 'var(--success, #10b981)',
            color: '#ffffff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            margin: '0 auto 1.25rem',
            fontSize: '32px'
          }}>
            ✓
          </div>
          <h1 className="auth-title">Password Reset!</h1>
          <p className="auth-subtitle">
            {message || 'Your password has been updated. Redirecting you to sign in...'}
          </p>
        </div>
        <div style={{ textAlign: 'center', marginTop: '1.5rem' }}>
          <Link to="/login" className="btn-auth" style={{ display: 'inline-block', textDecoration: 'none', textAlign: 'center' }}>
            Go to Login
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="auth-card">
      <div className="auth-header">
        <h1 className="auth-title">Reset Password</h1>
        <p className="auth-subtitle">
          {email
            ? <>Enter the 6-digit code sent to <strong>{email}</strong> and your new password.</>
            : 'Enter your email address, the 6-digit code you received, and your new password.'}
        </p>
      </div>

      {message && <div className="success-message" role="status">{message}</div>}
      {resendSuccess && <div className="success-message" role="status">{resendSuccess}</div>}
      {error && <div className="error-message" role="alert" id="form-error">{error}</div>}

      <form aria-describedby={error ? "form-error" : undefined} className="auth-form" onSubmit={handleSubmit}>
        <div className="form-group">
          <label htmlFor="reset-email" className="form-label">Email Address</label>
          <input
            id="reset-email"
            type="email"
            name="email"
            autoComplete="email"
            required
            className="form-control"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com"
          />
        </div>

        <div className="form-group">
          <label htmlFor="reset-code" className="form-label">6-Digit Reset Code</label>
          <input
            id="reset-code"
            type="text"
            inputMode="numeric"
            autoComplete="one-time-code"
            pattern="[0-9]*"
            maxLength={6}
            required
            autoFocus
            className="form-control otp-input"
            value={code}
            onChange={(e) => handleCodeChange(e.target.value)}
            placeholder="123456"
            style={{
              textAlign: 'center',
              letterSpacing: '0.35em',
              fontSize: '1.5rem',
              fontWeight: 700
            }}
          />
        </div>

        <div className="form-group">
          <label htmlFor="newPassword" className="form-label">New Password</label>
          <PasswordField
            guidance
            newPasswordRules
            autoComplete="new-password"
            id="newPassword"
            className="form-control"
            required
            value={form.newPassword}
            onChange={(e) => setForm({ ...form, newPassword: e.target.value })}
            placeholder="Enter your new password"
          />
        </div>

        <div className="form-group">
          <label htmlFor="confirmPassword" className="form-label">Confirm Password</label>
          <PasswordField
            newPasswordRules
            autoComplete="new-password"
            id="confirmPassword"
            className="form-control"
            required
            value={form.confirmPassword}
            onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
            placeholder="Confirm your new password"
          />
        </div>

        <button
          type="submit"
          className="btn-auth"
          disabled={status === 'loading' || code.length !== 6 || !email.trim() || !form.newPassword}
        >
          {status === 'loading' ? 'Resetting Password...' : 'Reset Password'}
        </button>

        <div style={{ textAlign: 'center', marginTop: '0.75rem', fontSize: '0.9rem' }}>
          <span style={{ color: 'var(--text-secondary)' }}>Didn't receive the code? </span>
          <button
            type="button"
            className="auth-link"
            onClick={handleResend}
            disabled={resending || cooldown > 0 || !email.trim()}
            style={{
              background: 'transparent',
              border: 'none',
              cursor: cooldown > 0 || resending || !email.trim() ? 'not-allowed' : 'pointer',
              opacity: cooldown > 0 || resending || !email.trim() ? 0.6 : 1,
              padding: 0
            }}
          >
            {resending
              ? 'Sending code...'
              : cooldown > 0
                ? `Resend in ${cooldown}s`
                : 'Resend Code'}
          </button>
        </div>
      </form>

      <div className="auth-footer">
        <Link to="/login" className="auth-link">Return to login</Link>
      </div>
    </div>
  );
}
