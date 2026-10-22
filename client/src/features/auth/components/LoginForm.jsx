import { PasswordField } from '@/shared/components/ui/PasswordField.jsx';
import { Link } from 'react-router-dom';
import { useLogin } from '@/features/auth/hooks/useLogin.js';

export default function LoginForm() {
  const { email, setEmail, password, setPassword, authenticatorCode, setAuthenticatorCode, recoveryCode, setRecoveryCode, error, isLoading, message, handleLogin } = useLogin();
  return (
    <>
      <div className="auth-card">
        <div className="auth-header">
          <h1 className="auth-title">Welcome Back</h1>
          <p className="auth-subtitle">Sign in to continue to Amaara Creations</p>
        </div>

        {message && <div className="success-message" role="status" style={{ color: 'green', marginBottom: '1rem' }}>{message}</div>}
        {error && <div className="error-message" role="alert" id="form-error">{error}</div>}

        <form aria-describedby={error ? "form-error" : undefined} className="auth-form" onSubmit={handleLogin}>
          <div className="form-group">
            <label htmlFor="email" className="form-label">Email Address</label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              className="form-control"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Enter your email"
            />
          </div>

          <div className="form-group">
            <div className="d-flex justify-content-between">
              <label htmlFor="password" className="form-label">Password</label>
              <Link to="/forgot-password" className="auth-link" style={{ fontSize: '0.85rem' }}>
                Forgot password?
              </Link>
            </div>
            <PasswordField
              id="password"

              autoComplete="current-password"
              className="form-control"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
            />
          </div>

          <details className="form-group"><summary>Two-step verification</summary><p>Required if you enabled an authenticator. Use a current code or one unused recovery code.</p><label className="form-label" htmlFor="login-authenticator">Authenticator code</label><input className="form-control" id="login-authenticator" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} value={authenticatorCode} onChange={event => { setAuthenticatorCode(event.target.value); setRecoveryCode(''); }} /><label className="form-label" htmlFor="login-recovery">Recovery code</label><input className="form-control" id="login-recovery" autoComplete="off" spellCheck={false} maxLength={64} value={recoveryCode} onChange={event => { setRecoveryCode(event.target.value); setAuthenticatorCode(''); }} /></details>

          <button type="submit" className="btn-auth" disabled={isLoading}>
            {isLoading ? "Signing In..." : "Sign In"}
          </button>
        </form>

        <div className="auth-footer">
          Don't have an account?{' '}
          <Link to="/register" className="auth-link">
            Sign up
          </Link>
        </div>
      </div>
    </>
  );
}
