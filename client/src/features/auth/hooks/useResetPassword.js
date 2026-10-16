import { useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { authApi } from '@/features/auth/authApi.js';

export function useResetPassword() {
  const [searchParams] = useSearchParams();
  const email = useMemo(() => searchParams.get('email') || '', [searchParams]);
  const token = useMemo(() => searchParams.get('token') || '', [searchParams]);
  const [form, setForm] = useState({ newPassword: '', confirmPassword: '' });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setMessage('');
    setError('');

    if (!email || !token) {
      setError('Invalid password reset link.');
      return;
    }

    if (form.newPassword !== form.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setIsLoading(true);
    try {
      const response = await authApi.resetPassword({ email, token, ...form });
      setMessage(response.message);
    } catch (err) {
      setError(err.response?.data?.message || 'Invalid or expired password reset link.');
    } finally {
      setIsLoading(false);
    }
  };

  return { form, setForm, message, error, isLoading, handleSubmit };
}
