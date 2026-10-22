import AuthenticatorQrCard from './AuthenticatorQrCard.jsx';
import MfaCodeForm from './MfaCodeForm.jsx';
import RecoveryCodePanel from './RecoveryCodePanel.jsx';
import { useEffect, useRef } from 'react';

export default function MfaFlow({ challenge, error, notice, isLoading, verify, finish, cancel }) {
  const enrollment = challenge.status === 'enroll';
  const recovery = challenge.status === 'recovery_codes';
  const heading = useRef(null);
  useEffect(() => { heading.current?.focus(); }, [challenge.status]);
  return <section className="auth-card mfa-card" aria-labelledby="mfa-heading">
    <p className="mfa-progress">Security verification <span aria-hidden="true">·</span> {recovery ? 'Save your backup' : enrollment ? 'One-time setup' : 'Almost there'}</p>
    <header className="auth-header"><h1 className="auth-title" id="mfa-heading" tabIndex={-1} ref={heading}>{recovery ? 'Save your recovery codes' : enrollment ? 'Secure your Amaara account' : "Verify it’s you"}</h1><p className="auth-subtitle">{recovery ? 'These codes can help you sign in if you lose access to your phone.' : enrollment ? challenge.mandatory ? 'Your work account needs one extra security step. This helps protect customer orders and business information.' : 'Reconnect your authenticator to keep your account protected.' : 'Enter the 6-digit code from Google Authenticator to continue.'}</p></header>
    {error && <p role="alert" className="error-message" id="mfa-error">{error}</p>}{notice && <p role="status">{notice}</p>}
    {enrollment && <><p className="mfa-help">You’ll only need to scan the QR code once.</p><AuthenticatorQrCard enrollment={challenge.enrollment} /><h2 className="mfa-code-heading">Enter your verification code</h2></>}
    {recovery ? <RecoveryCodePanel codes={challenge.recoveryCodes} onFinish={finish} pending={isLoading} /> : <MfaCodeForm key={challenge.status} onVerify={verify} pending={isLoading} enrollment={enrollment} error={error} />}
    <button type="button" className="mfa-text-button mfa-back" disabled={isLoading} onClick={cancel}>Use a different account</button>
  </section>;
}
