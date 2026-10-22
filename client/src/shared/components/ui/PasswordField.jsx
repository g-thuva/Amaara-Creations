import { useEffect, useId, useState } from 'react';
import { PASSWORD_HELP, PASSWORD_INPUT_PROPS, passwordPolicyError } from '@/shared/utils/passwordPolicy.js';

export function PasswordField({ label, id, autoComplete = 'new-password', guidance = false, newPasswordRules = false, ...props }) {
  const generatedId = useId();
  const inputId = id || generatedId;
  const [visible, setVisible] = useState(false);
  const [capsLock, setCapsLock] = useState(false);
  useEffect(() => {
    if (!visible) return undefined;
    const hide = () => setVisible(false);
    const timer = window.setTimeout(hide, 30000);
    window.addEventListener('blur', hide);
    return () => { window.clearTimeout(timer); window.removeEventListener('blur', hide); };
  }, [visible]);
  const length = Array.from(props.value || '').length;
  const toggleLabel = (label || props.name || id || 'password').replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase();
  const describedBy = [props['aria-describedby'], guidance && `${inputId}-guidance`, capsLock && `${inputId}-caps`].filter(Boolean).join(' ') || undefined;
  return <div className="s-password-field">
    {label && <label htmlFor={inputId}>{label}</label>}
    <div className="s-password-control">
      <input {...(newPasswordRules ? PASSWORD_INPUT_PROPS : {})} {...props} id={inputId} type={visible ? 'text' : 'password'} autoComplete={autoComplete} aria-describedby={describedBy}
        onKeyUp={(event) => setCapsLock(event.getModifierState('CapsLock'))}
        onKeyDown={(event) => setCapsLock(event.getModifierState('CapsLock'))} />
      <button type="button" aria-controls={inputId} aria-pressed={visible} aria-label={`${visible ? 'Hide' : 'Show'} ${toggleLabel}`} onClick={() => setVisible(value => !value)}>{visible ? 'Hide' : 'Show'}</button>
    </div>
    {capsLock && <small id={`${inputId}-caps`} role="status">Caps Lock is on.</small>}
    {guidance && <small id={`${inputId}-guidance`}>{PASSWORD_HELP}{length > 0 && <span role="status"> {length < 6 ? `${6 - length} more characters needed.` : passwordPolicyError(props.value) ? 'Check the password requirements.' : 'Requirements met. Avoid common or reused passwords.'}</span>}</small>}
  </div>;
}
