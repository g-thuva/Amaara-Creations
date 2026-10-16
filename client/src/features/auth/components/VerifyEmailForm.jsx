import { Link } from 'react-router-dom';
import { useVerifyEmail } from '@/features/auth/hooks/useVerifyEmail.js';

export default function VerifyEmailForm() {
  const { status, message, resending, resendEmail, setResendEmail, resend } = useVerifyEmail();
  return (
    <>
      <div className="auth-card">
        <div className="auth-header">
          <h1 className="auth-title">{status === 'success' ? 'Email Verified' : 'Verify Email'}</h1>
          <p className="auth-subtitle">{message}</p>
        </div>
        {status === 'failed' && <form className="auth-form" onSubmit={resend}><label htmlFor="resend-email">Email address</label><input id="resend-email" type="email" autoComplete="email" required value={resendEmail} onChange={e => setResendEmail(e.target.value)}/><button className="btn-auth" disabled={resending}>{resending ? 'Sending…' : 'Resend verification'}</button></form>}
        <div className="auth-footer"><Link to="/login" className="auth-link">Go to login</Link></div>
      </div>
    </>
  );
}
