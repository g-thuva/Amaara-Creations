import { useId, useState } from 'react';
import { PasswordField } from '@/shared/components/ui/PasswordField.jsx';

export default function ReauthenticationFields({ label, secondFactor }) {
  const id = useId();
  const [code, setCode] = useState('');
  const [recovery, setRecovery] = useState('');
  return <><PasswordField label={label} name="password" autoComplete="current-password" required maxLength={256} />{secondFactor && <><p className="s-meta">Use a new authenticator code or one unused recovery code.</p><label htmlFor={`${id}-code`}>Authenticator code</label><input id={`${id}-code`} name="authenticatorCode" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} value={code} onChange={event => { setCode(event.target.value); setRecovery(''); }} /><label htmlFor={`${id}-recovery`}>Recovery code</label><input id={`${id}-recovery`} name="recoveryCode" autoComplete="off" maxLength={64} value={recovery} onChange={event => { setRecovery(event.target.value); setCode(''); }} /></>}</>;
}
