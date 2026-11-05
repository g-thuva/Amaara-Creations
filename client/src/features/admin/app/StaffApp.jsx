import SessionUnavailable from '@/features/auth/components/SessionUnavailable.jsx';
import { lazy, Suspense, useEffect, useState } from 'react';
import { Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { managementApi } from './managementApi.js';
import { adminStart, adminRoutes, can } from '@/app/config/permissions.js';
import { safeReturnDestination } from '@/features/auth/returnDestination.js';
import { sharedSignInDestination } from '@/features/auth/sharedSignInDestination.js';
import { AdminSessionActions } from '@/features/admin/layout/AdminSessionActions.js';

const StaffRoutes = lazy(() => import('@/app/routes/AdminRoutes.jsx'));
function SharedSignInRedirect() {
  const target = sharedSignInDestination(window.location.href);
  useEffect(() => { if (target) window.location.replace(target); }, [target]);
  return <main className="s-staff-signin">{target
    ? <><p role="status">Opening sign in...</p><a href={target}>Continue to sign in</a></>
    : <><h1>Sign-in unavailable</h1><p role="alert">The shared account page is unavailable. Contact the operator.</p></>}</main>;
}
function hasCurrentAccess(access, userId) {
  return access.phase === 'authorized' && access.user?.id === userId && new Date(access.expiresAt).getTime() > Date.now();
}

export function StaffApp() {
  const { user, authStatus, logout, updateUser, sessionVersion } = useAuth();
  const location = useLocation(); const navigate = useNavigate();
  const [access, setAccess] = useState({ phase: 'initializing' });
  const userId = user?.id;
  useEffect(() => {
    if (authStatus !== 'authenticated' || !userId) return undefined;
    const controller = new AbortController(); let timer; let generation = 0;
    async function verify() {
      const ticket = ++generation;
      // Keep the verified workspace mounted during navigation/focus checks.
      // Initial sign-in, changed identity and expired assurance still block it.
      setAccess(current => hasCurrentAccess(current, userId) ? current : { phase: 'initializing' });
      try {
        const session = await managementApi.session(controller.signal);
        if (controller.signal.aborted || ticket !== generation) return;
        const verifiedUser = { ...session.user, permissions: session.permissions };
        updateUser(verifiedUser);
        setAccess({ phase: session.status, user: verifiedUser, expiresAt: session.expiresAt });
        if (session.status === 'authorized') {
          const expires = new Date(session.expiresAt).getTime();
          const sensitive = new Date(session.stepUpExpiresAt).getTime();
          const next = sensitive > Date.now() ? sensitive : expires;
          clearTimeout(timer); timer = setTimeout(verify, Math.max(0, next - Date.now()));
        }
      } catch (failure) {
        if (!controller.signal.aborted && ticket === generation) setAccess({ phase: failure.response?.status === 401 ? 'expired' : failure.response?.status === 403 ? 'denied' : 'error' });
      }
    }
    verify(); window.addEventListener('focus', verify);
    return () => { controller.abort(); clearTimeout(timer); window.removeEventListener('focus', verify); };
    // The verified response supplies profile state; revalidation follows identity,
    // authentication status and route changes rather than profile replacement.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authStatus, userId, location.pathname, sessionVersion]);

  const signOut = () => { setAccess({ phase: 'unauthenticated' }); return logout(() => navigate('/login', { replace: true })); };
  if (['unavailable', 'logout-error'].includes(authStatus)) return <SessionUnavailable />;
  const verifying = access.phase === 'initializing' || (access.phase === 'authorized' && !hasCurrentAccess(access, userId));
  if (['loading', 'signing-out'].includes(authStatus) || (authStatus === 'authenticated' && verifying)) return <p role="status">Verifying your session...</p>;
  if (authStatus !== 'authenticated') return <SharedSignInRedirect />;
  if (['mfa_required', 'expired', 'enrollment_required'].includes(access.phase)) return <main className="s-staff-signin"><h1>Sign in again</h1><p>Your management session needs verification.</p><button className="s-button" onClick={signOut}>Sign out / reauthenticate</button></main>;
  if (access.phase !== 'authorized') return <main className="s-staff-signin"><h1>{access.phase === 'denied' ? 'Access denied' : 'Unable to open your workspace'}</h1><p role="alert">{access.phase === 'disabled' ? 'Management is unavailable in this environment.' : 'Your current account cannot open this workspace. Sign in again or contact the operator.'}</p><button className="s-button" onClick={signOut}>Sign out</button></main>;
  const start = adminStart(access.user);
  const requested = safeReturnDestination(location.state?.from || new URLSearchParams(location.search).get('returnTo'), start, true);
  const permission = adminRoutes[requested?.split('/')[2]];
  if (location.pathname === '/login' || !/^\/admin(?:\/|$)/.test(location.pathname)) return <Navigate to={requested && (!permission || can(access.user, permission)) ? requested : start} replace />;
  return <AdminSessionActions.Provider value={{ reauthenticate: signOut }}><div className="admin-app"><a className="s-skip" href="#store-main" onClick={event => { event.preventDefault(); document.getElementById('store-main')?.focus(); }}>Skip to main content</a><main id="store-main" tabIndex={-1}><Suspense fallback={<p role="status">Loading your workspace...</p>}><Routes><Route path="/admin/*" element={<StaffRoutes />} /></Routes></Suspense></main></div></AdminSessionActions.Provider>;
}
