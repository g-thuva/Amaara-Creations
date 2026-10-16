import { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";
import { customerError } from '@/shared/api/apiError.ts';

export function useLogin() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();

  // Check for message from navigation (e.g., from register)
  const message = location.state?.message;

  const handleLogin = async (e) => {
    e.preventDefault();
    setError("");
    setIsLoading(true);

    try {
      await login({ email, password });

      // On successful login, redirect to intended page or profile
      const from = location.state?.from;
      const redirectTo = typeof from === 'string' && from.startsWith('/') && !from.startsWith('//') ? from : '/profile';
      navigate(redirectTo, { replace: true });
    } catch (err) {
      const errorMessage = err.response?.status === 401 ? 'Unable to sign in. Check your details and confirm your email before trying again.' : customerError(err);
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return { email, setEmail, password, setPassword, error, isLoading, message, handleLogin };
}
