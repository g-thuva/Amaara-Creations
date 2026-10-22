import React, { lazy, Suspense, useEffect, useState } from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { AuthProvider, useAuth } from '@/features/auth/AuthProvider.jsx';
import { ToastProvider } from '@/shared/components/feedback/ToastProvider.jsx';
import { PasswordField } from '@/shared/components/ui/PasswordField.jsx';
import AuthenticatorSettings from '@/features/account/components/AuthenticatorSettings.jsx';
import http from '@/shared/api/httpClient.js';
import '@/shared/styles/fonts.css';
import '@/shared/styles/tokens.css';
import '@/shared/styles/globals.css';
import '@/features/auth/auth.css';
import '@/shared/styles/studio.css';

const StaffRoutes = lazy(() => import('@/app/routes/AdminRoutes.jsx'));
export function StaffApp() {
  const { user, authStatus, login, logout } = useAuth();
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const [verified, setVerified] = useState(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState('');
  const [manageSecurity, setManageSecurity] = useState(false);
  const [useRecovery, setUseRecovery] = useState(false);
  useEffect(() => {
    document.title = 'Amaara studio operations';
    let meta = document.querySelector('meta[name="robots"]');
    if (!meta) { meta = document.createElement('meta'); meta.name = 'robots'; document.head.append(meta); }
    meta.content = 'noindex, nofollow, noarchive';
  }, []);
  useEffect(() => {
    setVerified(false);
    if (!user) return undefined;
    let active = true;
    http.get('/admin/capabilities').then(() => { if (active) setVerified(true); }).catch(() => {});
    return () => { active = false; };
  }, [user]);
  useEffect(() => {
    if (!verified) return undefined;
    const timer = window.setTimeout(() => { setVerified(false); setError('Sign in again to renew staff access.'); }, 14 * 60000);
    return () => window.clearTimeout(timer);
  }, [verified]);
  async function submit(event) {
    event.preventDefault();
    if (pending) return;
    setPending(true); setError('');
    const data = new FormData(event.currentTarget);
    try {
      await login(Object.fromEntries(data));
      await http.get('/admin/capabilities');
      setVerified(true);
      navigate('/admin', { replace: true });
    } catch { setError('Unable to sign in. Check your credentials and authenticator code, or contact the owner.'); }
    finally { setPending(false); }
  }
  if (authStatus === 'loading') return <p role="status">Restoring session…</p>;
  if (user && manageSecurity) return <div className="storefront"><main className="s-staff-signin"><h1>Authenticator settings</h1><p>Verify your password and current factor to replace an authenticator. Recovery codes restore account access; a new authenticator code is required before entering operations.</p><AuthenticatorSettings staff /><button className="s-button s-button-secondary" onClick={() => setManageSecurity(false)}>Back to sign in</button></main></div>;
  if (!verified || pathname === '/login') return <div className="storefront"><main className="s-staff-signin"><p className="s-eyebrow">Amaara Creations · Studio operations</p><h1>Staff sign in</h1><p>Use your staff credentials and current authenticator code. Reauthenticate to renew access after 15 minutes.</p>{error && <p role="alert">{error}</p>}<form className="s-form" onSubmit={submit}><label>Email Address<input type="email" name="email" autoComplete="username" required /></label><PasswordField label="Password" name="password" autoComplete="current-password" required />{useRecovery ? <PasswordField label="Recovery code" name="recoveryCode" autoComplete="off" required maxLength={64} /> : <div><label htmlFor="staff-code">Authenticator code</label><input id="staff-code" name="authenticatorCode" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} aria-describedby="staff-code-help" /><p id="staff-code-help" className="s-meta">Required for enrolled accounts. Leave blank only to sign in before initial setup.</p></div>}<button className="s-button s-button-primary" disabled={pending}>{pending ? 'Signing in…' : 'Sign In'}</button><button className="s-button s-button-secondary" type="button" onClick={() => setUseRecovery(value => !value)}>{useRecovery ? 'Use authenticator code' : 'Use a recovery code'}</button></form>{user && <div className="s-actions"><button className="s-button s-button-secondary" onClick={() => setManageSecurity(true)}>Manage authenticator</button><button className="s-button s-button-secondary" onClick={() => logout()}>Sign out</button></div>}</main></div>;
  if (!pathname.startsWith('/admin')) return <Navigate to="/admin" replace />;
  return <div className="admin-app"><a className="s-skip" href="#store-main" onClick={event => { event.preventDefault(); document.getElementById('store-main')?.focus(); }}>Skip to main content</a><button className="staff-reauth" onClick={() => { setVerified(false); logout(); }}>Sign out / reauthenticate</button><main id="store-main" tabIndex={-1}><Suspense fallback={<p role="status">Loading operations…</p>}><Routes><Route path="/admin/*" element={<StaffRoutes />} /></Routes></Suspense></main></div>;
}

ReactDOM.createRoot(document.getElementById('root')).render(<React.StrictMode><BrowserRouter basename={import.meta.env.BASE_URL.replace(/\/$/, '') || '/'}><AuthProvider><ToastProvider><StaffApp /></ToastProvider></AuthProvider></BrowserRouter></React.StrictMode>);
