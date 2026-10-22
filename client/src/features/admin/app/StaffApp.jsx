import { lazy, Suspense, useEffect, useState } from 'react';
import { Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { managementApi } from './managementApi.js';
import { adminStart, adminRoutes, can } from '@/app/config/permissions.js';
import { safeReturnDestination } from '@/features/auth/returnDestination.js';
import { sharedSignInDestination } from '@/features/auth/sharedSignInDestination.js';

const StaffRoutes = lazy(() => import('@/app/routes/AdminRoutes.jsx'));
function SharedSignInEntry() {
  const destination = sharedSignInDestination(window.location.href);
  useEffect(() => { if (destination) window.location.replace(destination); }, [destination]);
  return <main className="s-staff-signin"><h1>Continue to sign in</h1>{destination ? <p role="status">Opening the Amaara sign-in page...</p> : <p role="alert">The sign-in address is unavailable. Contact the operator.</p>}</main>;
}

export function StaffApp() {
  const { user, authStatus, logout, updateUser } = useAuth();
  const location = useLocation(); const navigate = useNavigate();
  const [access, setAccess] = useState({ phase: 'initializing' });
  const userId = user?.id;
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
    // The verified response supplies profile state; revalidation follows identity,
    // authentication status and route changes rather than profile replacement.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authStatus, userId, location.pathname]);

  const signOut = () => { setAccess({ phase: 'unauthenticated' }); return logout(() => navigate('/login', { replace: true })); };
  if (['loading', 'authenticating', 'signing-out'].includes(authStatus) || (authStatus === 'authenticated' && (access.phase === 'initializing' || (access.phase === 'authorized' && access.path !== location.pathname)))) return <p role="status">Verifying your session...</p>;
  if (authStatus !== 'authenticated' || ['mfa_required', 'expired', 'enrollment_required'].includes(access.phase)) return <SharedSignInEntry />;
  if (access.phase !== 'authorized') return <main className="s-staff-signin"><h1>{access.phase === 'denied' ? 'Access denied' : 'Unable to open your workspace'}</h1><p role="alert">{access.phase === 'disabled' ? 'Management is unavailable in this environment.' : 'Your current account cannot open this workspace. Sign in again or contact the operator.'}</p><button className="s-button" onClick={signOut}>Sign out</button></main>;
  const start = adminStart(access.user);
  const requested = safeReturnDestination(location.state?.from || new URLSearchParams(location.search).get('returnTo'), start, true);
  const permission = adminRoutes[requested?.split('/')[2]];
  if (location.pathname === '/login' || !/^\/admin(?:\/|$)/.test(location.pathname)) return <Navigate to={requested && (!permission || can(access.user, permission)) ? requested : start} replace />;
  return <div className="admin-app"><a className="s-skip" href="#store-main" onClick={event => { event.preventDefault(); document.getElementById('store-main')?.focus(); }}>Skip to main content</a><button className="staff-reauth" onClick={signOut}>Sign out / reauthenticate</button><main id="store-main" tabIndex={-1}><Suspense fallback={<p role="status">Loading your workspace...</p>}><Routes><Route path="/admin/*" element={<StaffRoutes />} /></Routes></Suspense></main></div>;
}
