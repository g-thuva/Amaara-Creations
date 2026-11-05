/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useEffect, startTransition, Fragment } from 'react';
import { authApi } from '@/features/auth/authApi.js';
import { useSessionActivity } from './hooks/useSessionActivity.js';
import { publishSessionEvent, subscribeSessionEvents } from '@/shared/api/sessionEvents.js';
import { AUTH_BASE_PATH } from '@/shared/api/requestConfig.js';
import { acceptsIdentity } from './sessionIdentity.js';
import { reportClientError } from '@/shared/api/clientTelemetry.js';
import { beginAuthOperation, isCurrentAuthOperation, clearAccessToken, setAccessToken, setAuthFailureHandler, setSessionHandler } from '@/shared/api/authSession.js';

const AuthContext = createContext(null);
const management = AUTH_BASE_PATH === '/auth/staff';
function validateSession(response) {
  if (!response?.token || !acceptsIdentity(response.user, management)) {
    const error = new Error('The session does not belong to this sign-in page. Please sign in again.');
    error.response = { status: 401 };
    throw error;
  }
  return response;
}

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [authStatus, setAuthStatus] = useState('loading');
  const [challenge, setChallenge] = useState(null);
  const [restoreRevision, setRestoreRevision] = useState(0);
  const [sessionVersion, setSessionVersion] = useState(0);
  const [workspaceHandoff, setWorkspaceHandoff] = useState(null);
  useSessionActivity(authStatus === 'authenticated');
  const retrySession = () => { setAuthStatus('loading'); setRestoreRevision(value => value + 1); };
  useEffect(() => subscribeSessionEvents(type => {
    setWorkspaceHandoff(null);
    beginAuthOperation(); authApi.cancelRefresh(); clearAccessToken(); setChallenge(null); setUser(null);
    if (type === 'signed-out') setAuthStatus('unauthenticated');
    else retrySession();
  }), []);
  useEffect(() => {
    if (!challenge) return undefined;
    const timer = setTimeout(() => { setChallenge(null); clearAccessToken(); setUser(null); setAuthStatus('expired'); }, Math.max(0, new Date(challenge.expiresAt).getTime() - Date.now()));
    return () => clearTimeout(timer);
  }, [challenge]);

  useEffect(() => {
    let active = true;
    const ticket = beginAuthOperation();
    authApi.cancelRefresh();
    setSessionHandler(response => {
      if (!active) return;
      try { validateSession(response); }
      catch { clearAccessToken(); setUser(null); setAuthStatus('expired'); return; }
      setUser(previous => ({ ...response.user, ...(previous?.id === response.user.id && previous.permissions ? { permissions: previous.permissions } : {}) }));
      setSessionVersion(value => value + 1); setAuthStatus('authenticated');
    });
    setAuthFailureHandler(() => {
      setWorkspaceHandoff(null);
      beginAuthOperation();
      clearAccessToken();
      setChallenge(null);
      setUser(null);
      setAuthStatus('expired');
    });

    const restoreSession = async () => {
      try {
        {
          try {
            const pending = await authApi.pendingSignIn();
            if (!active || !isCurrentAuthOperation(ticket)) return;
            if (pending.status === 'credentials') {
              setChallenge(null); clearAccessToken(); setUser(null); setAuthStatus('unauthenticated');
              return;
            }
            if (pending.status !== 'session') {
              setChallenge(pending); clearAccessToken(); setUser(null); setAuthStatus('pending');
              return;
            }
          } catch (error) { if (error.response?.status !== 401) throw error; }
          if (!active || !isCurrentAuthOperation(ticket)) return;
        }
        const response = validateSession(await authApi.refresh());
        if (!active || !isCurrentAuthOperation(ticket)) return;
        setAccessToken(response.token);
        setUser(response.user);
        setAuthStatus('authenticated');
      } catch (error) {
        if (!active || !isCurrentAuthOperation(ticket)) return;
        clearAccessToken();
        setUser(null);
        setAuthStatus(authApi.isSessionRejected(error) ? 'unauthenticated' : 'unavailable');
      }
    };

    restoreSession();
    return () => { active = false; setSessionHandler(null); setAuthFailureHandler(null); };
  }, [restoreRevision]);

  const authenticate = async (operation) => {
    const ticket = beginAuthOperation();
    setWorkspaceHandoff(null);
    authApi.cancelRefresh();
    setAuthStatus('authenticating');
    let response;
    try {
      response = await operation();
      // Hand a completed staff sign-in to the management app without installing
      // its access token as the storefront's active identity.
      if (response.token && (management || !acceptsIdentity(response.user, true))) validateSession(response);
    }
    catch (error) {
      if (isCurrentAuthOperation(ticket)) {
        clearAccessToken(); setUser(null);
        if (error.response?.data?.code === 'MFA_CHALLENGE_EXPIRED') { setChallenge(null); setAuthStatus('unauthenticated'); }
        else setAuthStatus(challenge ? 'pending' : 'unauthenticated');
      }
      throw error;
    }
    if (!isCurrentAuthOperation(ticket)) throw new Error('This sign-in attempt was replaced. Please try again.');
    if (response.token && !management && acceptsIdentity(response.user, true)) {
      setChallenge(null); clearAccessToken(); setUser(null); setAuthStatus('unauthenticated');
    } else if (response.token && response.user) {
      setChallenge(null); setAccessToken(response.token); setUser(response.user); setAuthStatus('authenticated'); publishSessionEvent('signed-in');
    } else {
      clearAccessToken(); setUser(null);
      setChallenge(response.status === 'credentials' ? null : response);
      setAuthStatus(response.status === 'credentials' ? 'unauthenticated' : 'pending');
    }
    return response;
  };
  const login = (credentials) => authenticate(() => authApi.login(credentials));
  const advanceSignIn = (action, values) => authenticate(() => authApi.advanceSignIn(action, values));

  const register = async (userData) => {
    return authApi.register(userData);
  };

  const logout = async (onSignedOut) => {
    setWorkspaceHandoff(null);
    const ticket = beginAuthOperation();
    authApi.cancelRefresh();
    setAuthStatus('signing-out');
    try {
      await authApi.logout();
    } catch (error) {
      reportClientError('auth.logout', error);
      if (isCurrentAuthOperation(ticket)) { clearAccessToken(); setUser(null); setChallenge(null); setAuthStatus('logout-error'); }
      return;
    }
    if (isCurrentAuthOperation(ticket)) {
      clearAccessToken();
      setChallenge(null);
      publishSessionEvent('signed-out');
      // Complete caller navigation after cookie revocation and before guards
      // observe the cleared session. Always clear local state if it throws.
      startTransition(() => {
        try { onSignedOut?.(); }
        finally {
          setUser(null);
          setAuthStatus('unauthenticated');
        }
      });
    }
  };

  const updateUser = (userData) => {
    setUser(userData);
  };

  const value = {
    user,
    isAuthenticated: authStatus === 'authenticated',
    isLoading: ['loading', 'authenticating', 'signing-out'].includes(authStatus),
    authStatus,
    retrySession,
    sessionVersion,
    workspaceHandoff,
    setWorkspaceHandoff,
    challenge,
    advanceSignIn,
    login,
    register,
    logout,
    updateUser,
  };

  return <AuthContext.Provider value={value}><Fragment key={user?.id ?? 'anonymous'}>{children}</Fragment></AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

