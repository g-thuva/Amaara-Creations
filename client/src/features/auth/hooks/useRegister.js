import { customerError } from '@/shared/api/apiError.ts';
import { passwordPolicyError } from '@/shared/utils/passwordPolicy.js';
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/features/auth/AuthProvider.jsx";

export function useRegister() {
  const [form, setForm] = useState({
    name: "",
    email: "",
    password: "",
    confirmPassword: ""
  });
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();
  const { register } = useAuth();

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleRegister = async (e) => {
    e.preventDefault();
    setError("");

    // Validate passwords match
    if (form.password !== form.confirmPassword) {
      setError("Passwords do not match");
      return;
    }

    const policyError = passwordPolicyError(form.password);
    if (policyError) {
      setError(policyError);
      return;
    }

    setIsLoading(true);

    try {
      await register({
        name: form.name,
        email: form.email,
        password: form.password,
        confirmPassword: form.confirmPassword
      });

      navigate(`/verify-email?email=${encodeURIComponent(form.email)}`, {
        state: {
          email: form.email,
          registered: true,
          message: "Account created successfully! We've sent a 6-digit verification code to your email."
        }
      });
    } catch (err) {
      setError(customerError(err, 'Registration failed. Please try again.'));
    } finally {
      setIsLoading(false);
    }
  };

  return { form, error, isLoading, handleChange, handleRegister };
}
