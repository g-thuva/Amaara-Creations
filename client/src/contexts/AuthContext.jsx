/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useState, useEffect } from 'react';
import { authApi } from '../services/authApi';
import { clearAccessToken, setAccessToken, setAuthFailureHandler } from '../services/authSession';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [authStatus, setAuthStatus] = useState('loading');

  useEffect(() => {
    setAuthFailureHandler(() => {
      clearAccessToken();
      setUser(null);
      setAuthStatus('unauthenticated');
    });

    const restoreSession = async () => {
      try {
        const response = await authApi.refresh();
        setAccessToken(response.token);
        setUser(response.user);
        setAuthStatus('authenticated');
      } catch {
        clearAccessToken();
        setUser(null);
        setAuthStatus('unauthenticated');
      }
    };

    restoreSession();
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

  const logout = async () => {
    try {
      await authApi.logout();
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      clearAccessToken();
      setUser(null);
      setAuthStatus('unauthenticated');
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

