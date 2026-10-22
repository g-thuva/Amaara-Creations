import { PASSWORD_HELP } from '@/shared/utils/passwordPolicy.js';
import { PasswordField } from '@/shared/components/ui/PasswordField.jsx';
import { useAccountSecurity } from '@/features/account/hooks/useAccountSecurity.js';
import { Button } from '@/shared/components/ui/Button.jsx';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog.jsx';

export default function AccountSecurityForm() {
  const { error, pending, confirm, setConfirm, submit, logoutAll } = useAccountSecurity();
  return <>{error && <p className="s-alert s-error" role="alert" id="security-error">{error}</p>}<form className="s-form" onSubmit={submit} aria-describedby={error ? 'security-error' : undefined}><PasswordField label="Current password" name="currentPassword"  autoComplete="current-password" required/><PasswordField label="New password" name="newPassword"  autoComplete="new-password" required newPasswordRules aria-describedby="password-help"/><p id="password-help" className="s-meta">{PASSWORD_HELP}</p><PasswordField label="Confirm new password" name="confirmPassword"  autoComplete="new-password" required newPasswordRules/><Button disabled={pending}>{pending ? 'Saving…' : 'Change password'}</Button></form><section className="s-section"><h2>Your sessions</h2><p>Sign out on every device, including this one.</p><Button variant="secondary" disabled={pending} onClick={() => setConfirm(true)}>Sign out of all devices</Button></section><ConfirmDialog open={confirm} onClose={() => setConfirm(false)} title="Sign out of all devices?" pending={pending} onConfirm={logoutAll}>You will need to sign in again on each device.</ConfirmDialog></>;
}
