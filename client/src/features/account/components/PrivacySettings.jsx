import { useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useResource } from '@/shared/hooks/useResource.js';
import { securityApi } from '@/features/account/securityApi.js';
import { customerError } from '@/shared/api/apiError.ts';
import ReauthenticationFields from './ReauthenticationFields.jsx';
import { Button } from '@/shared/components/ui/Button.jsx';
import { useAuth } from '@/features/auth/AuthProvider.jsx';

export default function PrivacySettings() {
  const settings = useResource(async signal => ({ capabilities: await securityApi.capabilities(signal), secondFactor: await securityApi.secondFactor() }), 'privacy-settings');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [confirmEmail, setConfirmEmail] = useState(false);
  const [revision, setRevision] = useState(0);
  const locked = useRef(false);
  const { logout } = useAuth(); const navigate = useNavigate();
  async function submit(event, operation) {
    event.preventDefault(); if (locked.current) return; locked.current = true;
    const form = event.currentTarget;
    const data = Object.fromEntries(new FormData(form));
    setPending(true); setError(''); setMessage('');
    try {
      if (operation === 'export') {
        const blob = await securityApi.exportAccount(data);
        const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = 'amaara-account-data.json'; document.body.append(link); link.click(); link.remove(); window.setTimeout(() => URL.revokeObjectURL(url), 1000);
        setMessage('Your account-data download is ready. Store it privately.');
      } else if (operation === 'deletion') {
        await securityApi.requestDeletionReview({ ...data, acknowledged: data.acknowledged === 'on' });
        setMessage('Your deletion review request was received. Track the reply in Your requests. No data has been deleted.');
      } else if (operation === 'email' && !confirmEmail) {
        const response = await securityApi.requestEmailChange(data); setMessage(response.message); setConfirmEmail(true);
      } else {
        await securityApi.confirmEmailChange(data);
        await logout(() => navigate('/login', { replace: true, state: { message: 'Sign-in email changed. Please sign in with your verified new address.' } }));
      }
      if (operation !== 'email') form.reset();
      else form.querySelector('input[name=password]').value = '';
      setRevision(current => current + 1);
    } catch (error) {
      if (error.response?.data instanceof Blob) { try { const problem = JSON.parse(await error.response.data.text()); setError(problem.message || problem.title || 'Unable to export account data.'); } catch { setError(customerError(error)); } }
      else setError(customerError(error));
    }
    finally { locked.current = false; setPending(false); }
  }
  if (settings.loading) return <p role="status">Loading privacy controls…</p>;
  if (settings.error) return <p role="alert">Unable to load privacy controls. <Button variant="text" onClick={settings.reload}>Retry</Button></p>;
  const { capabilities, secondFactor } = settings.data;
  return <section className="s-section"><h2>Privacy and sign-in details</h2>{error && <p role="alert" className="s-error">{error}</p>}{message && <p role="status">{message}</p>}
    <h3>Download your account data</h3><p>Download your profile, addresses, order history, saved-design details, wishlist, reviews and requests as JSON. Artwork stays available through your protected design pages. The download contains personal information.</p>
    <form className="s-form" onSubmit={event => submit(event, 'export')}><ReauthenticationFields key={`export-${revision}`} label="Confirm password for account export" secondFactor={secondFactor.enabled} /><Button disabled={pending}>Download account data</Button></form>
    {capabilities.emailChangeEnabled && <><h3>Change sign-in email</h3><p>Your current email remains active until you verify the new address. Confirmation signs out every device.</p><form className="s-form" onSubmit={event => submit(event, 'email')}><label>New sign-in email<input name="newEmail" type="email" autoComplete="email" required maxLength={254} /></label><ReauthenticationFields key={`email-${revision}`} label="Confirm password for email change" secondFactor={secondFactor.enabled} />{confirmEmail && <label>Email verification token<textarea name="token" autoComplete="one-time-code" maxLength={4096} required spellCheck={false} /></label>}<Button disabled={pending}>{confirmEmail ? 'Verify new email' : 'Send email verification token'}</Button><Button variant="text" type="button" disabled={pending} onClick={() => { setConfirmEmail(current => !current); setError(''); }}>{confirmEmail ? 'Request a new token' : 'I already have a verification token'}</Button></form></>}
    {capabilities.deletionRequestsEnabled && <><h3>Request account deletion review</h3><p>Send a request for review. Required order, payment and audit records may need retention. This action does not erase data immediately or promise an outcome before review.</p><form className="s-form" onSubmit={event => submit(event, 'deletion')}><label>Deletion request details<textarea name="details" required maxLength={2000} /></label><ReauthenticationFields key={`deletion-${revision}`} label="Confirm password for deletion review" secondFactor={secondFactor.enabled} /><label><input type="checkbox" name="acknowledged" required /> I understand this submits a review request and does not delete data immediately.</label><Button disabled={pending}>Submit deletion review request</Button></form></>}
    <p><Link to="/account/requests">Your requests and replies</Link></p>
  </section>;
}
