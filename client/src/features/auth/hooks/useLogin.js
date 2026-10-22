import { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";
import { customerError } from '@/shared/api/apiError.ts';
import { loginDestination } from '@/features/auth/loginDestination.js';

export function useLogin() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [authenticatorCode, setAuthenticatorCode] = useState('');
  const [recoveryCode, setRecoveryCode] = useState('');
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
      const response = await login({ email, password, authenticatorCode, recoveryCode });
      const destination = loginDestination(response.user, location.state?.from);
      if (!destination) {
        setError('Your staff sign-in succeeded, but the separate management sign-in address is not configured correctly. Contact the operator.');
        return;
      }
      if (destination.external) window.location.replace(destination.url);
      else navigate(destination.url, { replace: true });
    } catch (err) {
      const errorMessage = err.response?.data?.code === 'MANAGEMENT_DISABLED' ? 'Staff sign-in is unavailable in this environment. Contact the operator.' : err.response?.status === 401 ? 'Unable to sign in. Check your email, password and current authenticator code, and confirm your email before trying again.' : customerError(err);
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return { email, setEmail, password, setPassword, authenticatorCode, setAuthenticatorCode, recoveryCode, setRecoveryCode, error, isLoading, message, handleLogin };
}
