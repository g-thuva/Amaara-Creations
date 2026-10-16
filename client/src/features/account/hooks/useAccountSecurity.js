import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { authApi } from '@/features/auth/authApi.js';
import { customerError } from '@/shared/api/apiError.ts';

export function useAccountSecurity() {
  const [error, setError] = useState('');
  const [pending, setPending] = useState(false);
  const [confirm, setConfirm] = useState(false);
  const { logout } = useAuth(); const navigate = useNavigate();
  const submit = async e => {
    e.preventDefault(); const values = Object.fromEntries(new FormData(e.currentTarget)); setError('');
    if (values.newPassword !== values.confirmPassword) { setError('Passwords do not match.'); return; }
    setPending(true);
    try { await authApi.changePassword(values); await logout(() => navigate('/login', { replace: true, state: { message: 'Password changed. Please sign in again.' } })); }
    catch (err) { setError(customerError(err)); } finally { setPending(false); }
  };
  const logoutAll = async () => {
    setPending(true); setError('');
    try { await authApi.logoutAll(); await logout(() => navigate('/login', { replace: true, state: { message: 'Signed out of all devices.' } })); }
    catch (err) { setError(customerError(err)); } finally { setPending(false); setConfirm(false); }
  };
  return { error, pending, confirm, setConfirm, submit, logoutAll };
}
