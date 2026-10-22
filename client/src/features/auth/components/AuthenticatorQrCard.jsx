import { useState } from 'react';
import { QRCodeSVG } from 'qrcode.react';
import '@/features/auth/auth.css';

export default function AuthenticatorQrCard({ enrollment }) {
  const [message, setMessage] = useState('');
  async function copy() {
    try { await navigator.clipboard.writeText(enrollment.sharedKey); setMessage('Setup key copied.'); }
    catch { setMessage('Select the setup key below and copy it manually.'); }
  }
  return <div className="mfa-enrollment">
    <details className="mfa-install"><summary>Get Google Authenticator</summary><p>Install the app, open it, tap + and choose Scan a QR code. Already have an authenticator? Use the QR code below.</p><div className="mfa-actions"><a href="https://play.google.com/store/apps/details?id=com.google.android.apps.authenticator2" target="_blank" rel="noreferrer">Google Play</a><a href="https://apps.apple.com/app/google-authenticator/id388497605" target="_blank" rel="noreferrer">App Store</a></div><p>You can use Google Authenticator without signing into a Google account. Compatible authenticator apps work too.</p></details>
    <div className="mfa-qr" role="img" aria-label="Authenticator enrollment QR code"><QRCodeSVG value={enrollment.authenticatorUri} size={224} level="M" marginSize={4} aria-hidden="true" /></div>
    <p className="mfa-help">Open Google Authenticator and scan this QR code.</p>
    <details className="mfa-manual"><summary>Can’t scan? Enter the setup key manually.</summary><p>Add a time-based account named Amaara Creations using this key.</p><code className="s-setup-key">{enrollment.sharedKey}</code><button type="button" className="mfa-text-button" onClick={copy}>Copy setup key</button><a href={enrollment.authenticatorUri}>Open in an installed authenticator app</a></details>
    {message && <p role="status">{message}</p>}
  </div>;
}
