import { useState } from 'react';
import { authApi } from '@/features/auth/authApi.js';

export function useForgotPassword() {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setMessage('');
    setError('');
    setIsLoading(true);

    try {
      const response = await authApi.forgotPassword(email);
      setMessage(response.message);
    } catch {
      setError('Unable to process the request. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return { email, setEmail, message, error, isLoading, handleSubmit };
}
