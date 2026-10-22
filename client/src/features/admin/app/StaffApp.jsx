import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import { Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { PasswordField } from '@/shared/components/ui/PasswordField.jsx';
import AuthenticatorSettings from '@/features/account/components/AuthenticatorSettings.jsx';
import { managementApi } from './managementApi.js';
import { adminStart, adminRoutes, can } from '@/app/config/permissions.js';
import { safeReturnDestination } from '@/features/auth/returnDestination.js';

const StaffRoutes = lazy(() => import('@/app/routes/AdminRoutes.jsx'));
export function StaffApp() {
  const { user, authStatus, login, logout, updateUser } = useAuth();
  const location = useLocation(); const navigate = useNavigate();
  const [access, setAccess] = useState({ phase: 'initializing' });
  const [error, setError] = useState('');
  const [manageSecurity, setManageSecurity] = useState(false);
  const [useRecovery, setUseRecovery] = useState(false);
  const pending = useRef(false);
  const userId = user?.id;
  // Revalidate on route changes, focus, or assurance expiry. Unknown state never
  // imports operational routes. AuthProvider owns the underlying session lifetime.
  useEffect(() => {
    if (authStatus !== 'authenticated' || !userId) return undefined;
    const controller = new AbortController(); let timer; let generation = 0;
    async function verify() {
      const ticket = ++generation;
      setAccess({ phase: 'initializing' });
      try {
        const session = await managementApi.session(controller.signal);
        if (controller.signal.aborted || ticket !== generation) return;
        const verifiedUser = { ...session.user, permissions: session.permissions };
        updateUser(verifiedUser);
        setAccess({ phase: session.status, user: verifiedUser, path: location.pathname });
        if (session.status === 'authorized') {
          const expires = new Date(session.expiresAt).getTime();
          // Recheck when sensitive assurance (5m) ends, then at the absolute 15m end.
          const sensitive = expires - 10 * 60000;
          const next = sensitive > Date.now() ? sensitive : expires;
          clearTimeout(timer); timer = setTimeout(verify, Math.max(0, next - Date.now()));
        }
      } catch (failure) {
        if (!controller.signal.aborted && ticket === generation) setAccess({ phase: failure.response?.status === 401 ? 'expired' : failure.response?.status === 403 ? 'denied' : 'error' });
      }
    }
    verify(); window.addEventListener('focus', verify);
    return () => { controller.abort(); clearTimeout(timer); window.removeEventListener('focus', verify); };
    // Profile replacement is deliberately not a dependency: the session response
    // supplies it. A new authenticated identity/status or route starts a new check.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authStatus, userId, location.pathname]);

  async function submit(event) {
    event.preventDefault(); if (pending.current) return;
    pending.current = true; setError(''); setManageSecurity(false); setAccess({ phase: 'initializing' });
    const credentials = Object.fromEntries(new FormData(event.currentTarget));
    try { await login(credentials); }
    catch (failure) {
      const code = failure.response?.data?.code;
      setError(code === 'MANAGEMENT_DISABLED' ? 'Management access is disabled. Ask the operator to activate this environment.' : failure.response?.status === 403 ? 'This account is not authorized for management.' : 'Unable to sign in. Check your credentials and use a current authenticator code if enrolled.');
    } finally { pending.current = false; }
  }
  const signOut = () => { setAccess({ phase: 'unauthenticated' }); setManageSecurity(false); return logout(() => navigate('/login', { replace: true })); };
  if (['loading', 'authenticating', 'signing-out'].includes(authStatus) || (authStatus === 'authenticated' && (access.phase === 'initializing' || (access.phase === 'authorized' && access.path !== location.pathname)))) return <p role="status">{authStatus === 'signing-out' ? 'Signing out…' : authStatus === 'authenticating' ? 'Verifying sign-in…' : 'Verifying management session…'}</p>;
  const authenticated = authStatus === 'authenticated';
  if (authenticated && (manageSecurity || access.phase === 'enrollment_required')) return <div className="storefront"><main className="s-staff-signin"><h1>Set up management security</h1><p>Enroll or recover your own authenticator before accessing operations. Recovery codes permit repair; an authenticator code is required for management access.</p><AuthenticatorSettings staff /><button className="s-button s-button-secondary" onClick={signOut}>Back to sign in</button></main></div>;
  if (authenticated && access.phase === 'authorized') {
    const start = adminStart(access.user);
    const requested = safeReturnDestination(location.state?.from, start, true);
    const permission = adminRoutes[requested?.split('/')[2]];
    if (location.pathname === '/login' || !/^\/admin(?:\/|$)/.test(location.pathname)) return <Navigate to={requested && (!permission || can(access.user, permission)) ? requested : start} replace />;
    return <div className="admin-app"><a className="s-skip" href="#store-main" onClick={event => { event.preventDefault(); document.getElementById('store-main')?.focus(); }}>Skip to main content</a><button className="staff-reauth" onClick={signOut}>Sign out / reauthenticate</button><main id="store-main" tabIndex={-1}><Suspense fallback={<p role="status">Loading operations…</p>}><Routes><Route path="/admin/*" element={<StaffRoutes />} /></Routes></Suspense></main></div>;
  }
  return <div className="storefront"><main className="s-staff-signin"><p className="s-eyebrow">Amaara Creations · Studio operations</p><h1>Staff sign in</h1><p>Use your staff credentials and current authenticator code. Management access expires after 15 minutes.</p>
    {(authStatus === 'expired' || access.phase === 'expired') && <p role="alert">Your management session expired. Sign in again.</p>}
    {authenticated && access.phase === 'mfa_required' && <p role="alert">A fresh authenticator sign-in is required before entering operations.</p>}
    {authenticated && access.phase === 'denied' && <p role="alert">This account is not authorized for management.</p>}
    {authenticated && access.phase === 'disabled' && <p role="alert">Management is disabled in this environment. Contact the operator.</p>}
    {authenticated && access.phase === 'error' && <p role="alert">Unable to verify management access. Sign in again or retry when the service is available.</p>}
    {error && <p role="alert">{error}</p>}
    <form className="s-form" onSubmit={submit}><label>Email Address<input type="email" name="email" autoComplete="username" required /></label><PasswordField label="Password" name="password" autoComplete="current-password" required />{useRecovery ? <PasswordField label="Recovery code" name="recoveryCode" autoComplete="off" required maxLength={64} /> : <div><label htmlFor="staff-code">Authenticator code</label><input id="staff-code" name="authenticatorCode" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} aria-describedby="staff-code-help" /><p id="staff-code-help" className="s-meta">Required for enrolled accounts. Leave blank only before initial setup.</p></div>}<button className="s-button s-button-primary" disabled={authStatus === 'authenticating'}>Sign In</button><button className="s-button s-button-secondary" type="button" onClick={() => setUseRecovery(value => !value)}>{useRecovery ? 'Use authenticator code' : 'Use a recovery code'}</button></form>
    {authenticated && <div className="s-actions"><button className="s-button s-button-secondary" onClick={() => setManageSecurity(true)}>Manage authenticator</button><button className="s-button s-button-secondary" onClick={signOut}>Sign out</button></div>}</main></div>;
}
