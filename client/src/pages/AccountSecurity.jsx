import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi } from '../services/authApi';
import { useAuth } from '../contexts/AuthContext';
import { Button, ConfirmDialog, PageHeader } from '../components/storefront/UI';
import { customerError } from '../utils/storefront';
export default function AccountSecurity() {
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);
  const [confirm, setConfirm] = useState(false);
  const { logout } = useAuth(); const navigate = useNavigate();
  const submit = async e => {
    e.preventDefault(); const values = Object.fromEntries(new FormData(e.currentTarget)); setError('');
    if (values.newPassword !== values.confirmPassword) { setError('Passwords do not match.'); return; }
    setPending(true);
    try { await authApi.changePassword(values); await logout(); navigate('/login', { replace: true, state: { message: 'Password changed. Please sign in again.' } }); }
    catch (err) { setError(customerError(err)); } finally { setPending(false); }
  };
  const logoutAll = async () => {
    setPending(true); setError('');
    try { await authApi.logoutAll(); await logout(); navigate('/login', { replace: true, state: { message: 'Signed out of all devices.' } }); }
    catch (err) { setError(customerError(err)); } finally { setPending(false); setConfirm(false); }
  };
  return <><PageHeader title="Account security">Manage your password and sign-in sessions.</PageHeader>{error && <p className="s-alert s-error" role="alert" id="security-error">{error}</p>}<form className="s-form" onSubmit={submit} aria-describedby={error ? 'security-error' : undefined}><label>Current password<input name="currentPassword" type="password" autoComplete="current-password" required/></label><label>New password<input name="newPassword" type="password" autoComplete="new-password" required minLength="6" aria-describedby="password-help"/></label><p id="password-help" className="s-meta">At least 6 characters, including uppercase, lowercase and a number.</p><label>Confirm new password<input name="confirmPassword" type="password" autoComplete="new-password" required minLength="6"/></label><Button disabled={pending}>{pending ? 'Saving…' : 'Change password'}</Button></form><section className="s-section"><h2>Your sessions</h2><p>Sign out on every device, including this one.</p><Button variant="secondary" disabled={pending} onClick={() => setConfirm(true)}>Sign out of all devices</Button></section><ConfirmDialog open={confirm} onClose={() => setConfirm(false)} title="Sign out of all devices?" pending={pending} onConfirm={logoutAll}>You will need to sign in again on each device.</ConfirmDialog></>;
}
