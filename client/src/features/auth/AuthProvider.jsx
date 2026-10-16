/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useEffect, startTransition } from 'react';
import { authApi } from '@/features/auth/authApi.js';
import { reportClientError } from '@/shared/api/clientTelemetry.js';
import { clearAccessToken, setAccessToken, setAuthFailureHandler } from '@/shared/api/authSession.js';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [authStatus, setAuthStatus] = useState('loading');

  useEffect(() => {
    let active = true;
    setAuthFailureHandler(() => {
      clearAccessToken();
      setUser(null);
      setAuthStatus('unauthenticated');
    });

    const restoreSession = async () => {
      try {
        const response = await authApi.refresh();
        if (!active) return;
        setAccessToken(response.token);
        setUser(response.user);
        setAuthStatus('authenticated');
      } catch {
        if (!active) return;
        clearAccessToken();
        setUser(null);
        setAuthStatus('unauthenticated');
      }
    };

    restoreSession();
    return () => { active = false; };
  }, []);

  const login = async (credentials) => {
    const response = await authApi.login(credentials);
    setAccessToken(response.token);
    setUser(response.user);
    setAuthStatus('authenticated');
    return response;
  };

  const register = async (userData) => {
    return authApi.register(userData);
  };

  const logout = async (onSignedOut) => {
    try {
      await authApi.logout();
    } catch (error) {
      reportClientError('auth.logout', error);
    } finally {
      clearAccessToken();
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
    isLoading: authStatus === 'loading',
    authStatus,
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

