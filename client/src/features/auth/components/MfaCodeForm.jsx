import { useState } from 'react';
import { Link } from 'react-router-dom';

export default function MfaCodeForm({ onVerify, pending, error, enrollment = false }) {
  const [code, setCode] = useState('');
  const [recovery, setRecovery] = useState(false);
  function submit(event) {
    event.preventDefault();
    onVerify(recovery ? { recoveryCode: code } : { code });
    setCode('');
  }
  return <form className="auth-form" onSubmit={submit}>
    <div className="form-group"><label className="form-label" htmlFor="mfa-code">{recovery ? 'Recovery code' : 'Authentication code'}</label>
      <input id="mfa-code" className="form-control mfa-code-input" inputMode={recovery ? 'text' : 'numeric'} autoComplete={recovery ? 'off' : 'one-time-code'} autoFocus={!enrollment} required minLength={recovery ? 8 : 6} maxLength={recovery ? 64 : 6} pattern={recovery ? undefined : '[0-9]{6}'} value={code} onChange={event => setCode(recovery ? event.target.value : event.target.value.replace(/\D/g, '').slice(0, 6))} onPaste={event => { event.preventDefault(); const text = event.clipboardData.getData('text'); setCode(recovery ? text.trim().slice(0, 64) : text.replace(/\D/g, '').slice(0, 6)); }} aria-invalid={Boolean(error)} aria-describedby={error ? 'mfa-code-help mfa-error' : 'mfa-code-help'} />
      <p className="mfa-help" id="mfa-code-help">{recovery ? 'Use one of the recovery codes you saved. You can then reconnect an authenticator.' : 'Your authenticator code refreshes approximately every 30 seconds.'}</p>
    </div>
    <button className="btn-auth" disabled={pending}>{pending ? 'Verifying…' : enrollment ? 'Verify and continue' : 'Verify and sign in'}</button>
    {!enrollment && <button className="mfa-text-button" type="button" onClick={() => { setRecovery(value => !value); setCode(''); }}>{recovery ? 'Use an authenticator code' : 'Use a recovery code'}</button>}
    {recovery && <p className="mfa-help">No recovery codes left? <Link className="auth-link" to="/contact">Contact support</Link> for a verified account recovery. Your password alone cannot reset staff security.</p>}
  </form>;
}
