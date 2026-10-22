import { useState } from 'react';

export default function RecoveryCodePanel({ codes, onFinish, pending, finishLabel = 'Continue to my account' }) {
  const [saved, setSaved] = useState(false);
  const [message, setMessage] = useState('');
  const content = () => `Amaara Creations recovery codes\n\n${codes.join('\n')}\n\nEach code works once. Keep these codes private.\n`;
  async function copy() {
    try { await navigator.clipboard.writeText(content()); setMessage('Recovery codes copied.'); }
    catch { setMessage('Copy is unavailable. Download or print your codes instead.'); }
  }
  function download() {
    const url = URL.createObjectURL(new Blob([content()], { type: 'text/plain' }));
    const link = document.createElement('a'); link.href = url; link.download = 'amaara-recovery-codes.txt'; link.click(); URL.revokeObjectURL(url);
    setMessage('Recovery codes downloaded.');
  }
  return <section className="mfa-recovery" aria-label="Recovery codes">
    <ul className="s-recovery-codes">{codes.map(code => <li key={code}><code>{code}</code></li>)}</ul>
    <p className="mfa-help">Each code works once. Keep them private; replacing them invalidates the previous set.</p>
    <div className="mfa-actions"><button type="button" onClick={download}>Download codes</button><button type="button" onClick={copy}>Copy codes</button><button type="button" onClick={() => window.print()}>Print codes</button></div>
    {message && <p role="status">{message}</p>}
    <label className="mfa-confirm"><input type="checkbox" checked={saved} onChange={event => setSaved(event.target.checked)} />I have saved my recovery codes somewhere safe.</label>
    <button className="btn-auth" disabled={!saved || pending} onClick={onFinish}>{pending ? 'Continuing…' : finishLabel}</button>
  </section>;
}
