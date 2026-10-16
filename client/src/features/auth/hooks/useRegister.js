import { customerError } from '@/shared/api/apiError.ts';
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

    // Validate password length
    if (form.password.length < 6 || !/[a-z]/.test(form.password) || !/[A-Z]/.test(form.password) || !/[0-9]/.test(form.password)) {
      setError("Use at least 6 characters with uppercase, lowercase and a number.");
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

      navigate("/login", {
        state: { message: "Registration successful. Check your email to verify your account before signing in." }
      });
    } catch (err) {
      setError(customerError(err, 'Registration failed. Please try again.'));
    } finally {
      setIsLoading(false);
    }
  };

  return { form, error, isLoading, handleChange, handleRegister };
}
