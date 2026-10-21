import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { securityApi } from '@/features/account/securityApi.js';
import { customerError } from '@/shared/api/apiError.ts';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog.jsx';
import { Button } from '@/shared/components/ui/Button.jsx';
import { useAuth } from '@/features/auth/AuthProvider.jsx';

export default function SessionList() {
  const [sessions, setSessions] = useState(null);
  const [selected, setSelected] = useState(null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const { logout } = useAuth();
  const navigate = useNavigate();
  useEffect(() => {
    let active = true;
    securityApi.sessions().then(data => { if (active) setSessions(data); }).catch(error => { if (active) setError(customerError(error)); });
    return () => { active = false; };
  }, []);
  async function revoke() {
    setPending(true); setError('');
    try {
      await securityApi.revokeSession(selected.id);
      if (selected.current) await logout(() => navigate('/login', { replace: true }));
      else setSessions(current => current.filter(item => item.id !== selected.id));
      setSelected(null);
    } catch (error) { setError(customerError(error)); }
    finally { setPending(false); }
  }
  return <section className="s-section"><h2>Active devices</h2><p>Up to 50 recent active sessions. Browser details are supplied by the device and may be unfamiliar; sign out any session you do not recognize.</p>{error && <p role="alert" className="s-error">{error}</p>}{sessions ? <ul className="s-session-list">{sessions.map(session => <li key={session.id}><strong>{session.current ? 'This device' : 'Other device'}</strong><p className="s-meta s-device-description">{session.device}</p><p className="s-meta">Last refreshed: <time dateTime={session.refreshedAt}>{new Date(session.refreshedAt).toLocaleString('en-LK')}</time></p><Button variant="secondary" disabled={pending} onClick={() => setSelected(session)}>Sign out {session.current ? 'this device' : 'device'}</Button></li>)}</ul> : <p role="status">Loading active sessions…</p>}<ConfirmDialog open={Boolean(selected)} onClose={() => setSelected(null)} title="Sign out this session?" onConfirm={revoke} pending={pending}>This device will need to sign in again.</ConfirmDialog></section>;
}
