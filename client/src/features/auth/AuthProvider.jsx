/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useEffect, startTransition } from 'react';
import { authApi } from '@/features/auth/authApi.js';
import { AUTH_BASE_PATH } from '@/shared/api/requestConfig.js';
import { reportClientError } from '@/shared/api/clientTelemetry.js';
import { beginAuthOperation, isCurrentAuthOperation, clearAccessToken, setAccessToken, setAuthFailureHandler } from '@/shared/api/authSession.js';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [authStatus, setAuthStatus] = useState('loading');
  const [challenge, setChallenge] = useState(null);
  useEffect(() => {
    if (!challenge) return undefined;
    const timer = setTimeout(() => { setChallenge(null); clearAccessToken(); setUser(null); setAuthStatus('expired'); }, Math.max(0, new Date(challenge.expiresAt).getTime() - Date.now()));
    return () => clearTimeout(timer);
  }, [challenge]);

  useEffect(() => {
    let active = true;
    const ticket = beginAuthOperation();
    setAuthFailureHandler(() => {
      beginAuthOperation();
      clearAccessToken();
      setChallenge(null);
      setUser(null);
      setAuthStatus('expired');
    });

    const restoreSession = async () => {
      try {
        if (AUTH_BASE_PATH === '/auth') {
          try {
            const pending = await authApi.pendingSignIn();
            if (!active || !isCurrentAuthOperation(ticket)) return;
            setChallenge(pending); clearAccessToken(); setUser(null); setAuthStatus('pending');
            return;
          } catch { /* No pending first factor: restore the completed session. */ }
          if (!active || !isCurrentAuthOperation(ticket)) return;
        }
        const response = await authApi.refresh();
        if (!active || !isCurrentAuthOperation(ticket)) return;
        setAccessToken(response.token);
        setUser(response.user);
        setAuthStatus('authenticated');
      } catch {
        if (!active || !isCurrentAuthOperation(ticket)) return;
        clearAccessToken();
        setUser(null);
        setAuthStatus('unauthenticated');
      }
    };

    restoreSession();
    return () => { active = false; };
  }, []);

  const authenticate = async (operation) => {
    const ticket = beginAuthOperation();
    authApi.cancelRefresh();
    setAuthStatus('authenticating');
    let response;
    try { response = await operation(); }
    catch (error) {
      if (isCurrentAuthOperation(ticket)) {
        clearAccessToken(); setUser(null);
        if (error.response?.data?.code === 'MFA_CHALLENGE_EXPIRED') { setChallenge(null); setAuthStatus('unauthenticated'); }
        else setAuthStatus(challenge ? 'pending' : 'unauthenticated');
      }
      throw error;
    }
    if (!isCurrentAuthOperation(ticket)) throw new Error('This sign-in attempt was replaced. Please try again.');
    if (response.token && response.user) {
      setChallenge(null); setAccessToken(response.token); setUser(response.user); setAuthStatus('authenticated');
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
    const ticket = beginAuthOperation();
    authApi.cancelRefresh();
    setAuthStatus('signing-out');
    try {
      await authApi.logout();
    } catch (error) {
      reportClientError('auth.logout', error);
    } finally {
      if (isCurrentAuthOperation(ticket)) {
        clearAccessToken();
        setChallenge(null);
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
    challenge,
    advanceSignIn,
    login,
    register,
    logout,
    updateUser,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

