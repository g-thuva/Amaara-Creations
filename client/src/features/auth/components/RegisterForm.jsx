import { Link } from 'react-router-dom';
import { useRegister } from '@/features/auth/hooks/useRegister.js';

export default function RegisterForm() {
  const { form, error, isLoading, handleChange, handleRegister } = useRegister();
  return (
    <>
      <div className="auth-card">
        <div className="auth-header">
          <h1 className="auth-title">Create Account</h1>
          <p className="auth-subtitle">Join Amaara Creations today</p>
        </div>
        
        {error && <div className="error-message" role="alert" id="form-error">{error}</div>}
        
        <form aria-describedby={error ? "form-error" : undefined} className="auth-form" onSubmit={handleRegister}>
          <div className="form-group">
            <label htmlFor="name" className="form-label">Full Name</label>
            <input
              id="name"
              type="text"
              name="name"
              autoComplete="name"
              maxLength="100"
              className="form-control"
              required
              value={form.name}
              onChange={handleChange}
              placeholder="Enter your full name"
            />
          </div>
          
          <div className="form-group">
            <label htmlFor="email" className="form-label">Email Address</label>
            <input
              id="email"
              type="email"
              name="email"
              autoComplete="email"
              maxLength="256"
              className="form-control"
              required
              value={form.email}
              onChange={handleChange}
              placeholder="Enter your email"
            />
          </div>

          <div className="form-group">
            <label htmlFor="password" className="form-label">Password</label>
            <input
              id="password"
              type="password"
              name="password"
              autoComplete="new-password"
              aria-describedby="password-help"
              className="form-control"
              required
              minLength="6"
              value={form.password}
              onChange={handleChange}
              placeholder="Create a password (min 6 characters)"
            />
            <p id="password-help" className="s-meta">At least 6 characters, including uppercase, lowercase and a number.</p>
          </div>
          
          <div className="form-group">
            <label htmlFor="confirmPassword" className="form-label">Confirm Password</label>
            <input
              id="confirmPassword"
              type="password"
              name="confirmPassword"
              autoComplete="new-password"
              className="form-control"
              required
              minLength="6"
              value={form.confirmPassword}
              onChange={handleChange}
              placeholder="Confirm your password"
            />
          </div>

          <button 
            type="submit" 
            className="btn-auth"
            disabled={isLoading}
          >
            {isLoading ? 'Creating Account...' : 'Create Account'}
          </button>
        </form>

        <div className="auth-footer">
          Already have an account?{' '}
          <Link to="/login" className="auth-link">
            Sign in
          </Link>
        </div>
      </div>
    </>
  );
}
