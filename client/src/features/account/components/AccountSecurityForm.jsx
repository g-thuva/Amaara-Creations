import { useAccountSecurity } from '@/features/account/hooks/useAccountSecurity.js';
import { Button } from '@/shared/components/ui/Button.jsx';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog.jsx';

export default function AccountSecurityForm() {
  const { error, pending, confirm, setConfirm, submit, logoutAll } = useAccountSecurity();
  return <>{error && <p className="s-alert s-error" role="alert" id="security-error">{error}</p>}<form className="s-form" onSubmit={submit} aria-describedby={error ? 'security-error' : undefined}><label>Current password<input name="currentPassword" type="password" autoComplete="current-password" required/></label><label>New password<input name="newPassword" type="password" autoComplete="new-password" required minLength="6" aria-describedby="password-help"/></label><p id="password-help" className="s-meta">At least 6 characters, including uppercase, lowercase and a number.</p><label>Confirm new password<input name="confirmPassword" type="password" autoComplete="new-password" required minLength="6"/></label><Button disabled={pending}>{pending ? 'Saving…' : 'Change password'}</Button></form><section className="s-section"><h2>Your sessions</h2><p>Sign out on every device, including this one.</p><Button variant="secondary" disabled={pending} onClick={() => setConfirm(true)}>Sign out of all devices</Button></section><ConfirmDialog open={confirm} onClose={() => setConfirm(false)} title="Sign out of all devices?" pending={pending} onConfirm={logoutAll}>You will need to sign in again on each device.</ConfirmDialog></>;
}
