import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi } from '@/features/auth/authApi.js';

export function useForgotPassword() {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (event) => {
    event.preventDefault();
    setMessage('');
    setError('');

    const cleanEmail = email.trim();
    if (!cleanEmail) {
      setError('Please enter your email address.');
      return;
    }

    setIsLoading(true);

    try {
      const response = await authApi.forgotPassword(cleanEmail);
      setMessage(response.message);

      // Navigate to reset password page with email
      setTimeout(() => {
        navigate(`/reset-password?email=${encodeURIComponent(cleanEmail)}`, {
          state: {
            email: cleanEmail,
            message: 'A 6-digit password reset code has been sent to your email.'
          }
        });
      }, 1000);
    } catch {
      setError('Unable to process the request. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return { email, setEmail, message, error, isLoading, handleSubmit };
}
