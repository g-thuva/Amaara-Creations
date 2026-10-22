import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { securityApi } from '@/features/account/securityApi.js';
import { customerError } from '@/shared/api/apiError.ts';
import { PasswordField } from '@/shared/components/ui/PasswordField.jsx';
import { Button } from '@/shared/components/ui/Button.jsx';
import { useAuth } from '@/features/auth/AuthProvider.jsx';

export default function AuthenticatorSettings({ staff = false }) {
  const [status, setStatus] = useState(null);
  const [enrollment, setEnrollment] = useState(null);
  const [codes, setCodes] = useState(null);
  const [requiresSignIn, setRequiresSignIn] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const { logout } = useAuth();
  const navigate = useNavigate();
  useEffect(() => {
    let active = true;
    securityApi.secondFactor().then(data => { if (active) setStatus(data); }).catch(error => { if (active) setError(customerError(error)); });
    return () => { active = false; };
  }, []);
  useEffect(() => {
    if (!enrollment) return undefined;
    const timer = window.setTimeout(() => { setEnrollment(null); setError('Setup expired. Start again to get a new key.'); }, Math.max(0, new Date(enrollment.expiresAt).getTime() - Date.now()));
    return () => window.clearTimeout(timer);
  }, [enrollment]);
  async function submit(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const values = Object.fromEntries(new FormData(form));
    const action = event.nativeEvent.submitter?.value || 'enrollment';
    if (pending) return;
    setPending(true); setError(''); setMessage('');
    try {
      const operations = { enrollment: securityApi.beginEnrollment, confirm: securityApi.confirmEnrollment, 'recovery-codes': securityApi.replaceRecoveryCodes, disable: securityApi.disableSecondFactor };
      const data = await operations[action](values);
      form.reset();
      if (action === 'enrollment') setEnrollment(data);
      else if (data.recoveryCodes) { setCodes(data.recoveryCodes); setEnrollment(null); setRequiresSignIn(Boolean(data.requiresSignIn)); }
      else if (action === 'disable') { await logout(() => navigate('/login', { replace: true, state: { message: 'Two-step verification disabled. Please sign in again.' } })); }
    } catch (error) { setError(customerError(error)); }
    finally { setPending(false); }
  }
  async function finish() {
    setCodes(null);
    if (requiresSignIn) await logout(() => navigate('/login', { replace: true, state: { message: 'Authenticator saved. Sign in with a new code from your app.' } }));
    else {
      try { setStatus(await securityApi.secondFactor()); setMessage('Recovery codes replaced. Keep the new codes somewhere safe.'); }
      catch (error) { setError(customerError(error)); }
    }
  }
  return <section className="s-section" aria-labelledby="authenticator-heading"><h2 id="authenticator-heading">Two-step verification</h2>
    <p>Add an authenticator app to protect sign-in. Each code works once; wait for a new code after signing in. {staff && 'Staff operations require a current authenticator code; recovery codes only restore account access.'}</p>
    {error && <p role="alert" className="s-alert s-error">{error}</p>}{message && <p role="status">{message}</p>}
    {codes ? <div className="s-alert" role="status"><h3>Save your recovery codes</h3><p>Each code works once with your password. Store these privately in a password manager or offline. They are shown only now; replacing them invalidates all previous codes.</p><ul className="s-recovery-codes">{codes.map(code => <li key={code}><code>{code}</code></li>)}</ul><Button onClick={finish}>I have saved these codes{requiresSignIn ? ' — sign in again' : ''}</Button></div> : status ? <>
      <p role="status">{status.enabled ? `Enabled · ${status.recoveryCodesLeft} recovery codes remaining` : 'Not enabled'}</p>
      {enrollment && <div className="s-alert"><h3>Connect your authenticator</h3><p>Add a time-based account in your authenticator app using this setup key. Confirm within 10 minutes. Never share this key.</p><code className="s-setup-key">{enrollment.sharedKey}</code><p><a href={enrollment.authenticatorUri}>Open in an installed authenticator app</a></p></div>}
      <form className="s-form" onSubmit={submit}><PasswordField label="Confirm your password" name="password" autoComplete="current-password" required maxLength={256} />
        {(status.enabled || enrollment) && <label>Authenticator code<input name="authenticatorCode" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} required={Boolean(enrollment)} /></label>}
        {status.enabled && !enrollment && <label>Or an unused recovery code<input name="recoveryCode" autoComplete="off" maxLength={64} spellCheck={false} /><span className="s-meta">Enter one factor only. To replace recovery codes, use your authenticator.</span></label>}
        <div className="s-actions">{enrollment ? <Button disabled={pending} name="action" value="confirm">{pending ? 'Verifying…' : 'Verify and enable'}</Button> : <>
          <Button disabled={pending} name="action" value="enrollment">{status.enabled ? 'Replace authenticator' : 'Set up authenticator'}</Button>
          {status.enabled && <Button disabled={pending} variant="secondary" name="action" value="recovery-codes">Replace recovery codes</Button>}
          {status.enabled && !staff && !status.mandatory && <Button disabled={pending} variant="secondary" name="action" value="disable">Disable two-step verification</Button>}
        </>}</div></form>
    </> : <p role="status">Loading security settings…</p>}
  </section>;
}
