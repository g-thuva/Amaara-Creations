import React, { useState } from "react";
import { useNavigate } from "react-router-dom";

const Register = () => {
  const [form, setForm] = useState({ name: "", email: "", password: "" });
  const navigate = useNavigate();

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleRegister = (e) => {
    e.preventDefault();
    // TODO: connect to backend
    console.log("Register", form);
    alert("Registered successfully!");
    navigate("/login");
  };

  return (
    <div className="container" style={{ padding: "2rem 0", maxWidth: "400px" }}>
      <h2 style={{ marginBottom: "1rem" }}>Register</h2>
      <form onSubmit={handleRegister}>
        <label>Name</label>
        <input
          type="text"
          name="name"
          required
          value={form.name}
          onChange={handleChange}
          style={{ width: "100%", padding: "8px", marginBottom: "1rem" }}
        />

        <label>Email</label>
        <input
          type="email"
          name="email"
          required
          value={form.email}
          onChange={handleChange}
          style={{ width: "100%", padding: "8px", marginBottom: "1rem" }}
        />

        <label>Password</label>
        <input
          type="password"
          name="password"
          required
          value={form.password}
          onChange={handleChange}
          style={{ width: "100%", padding: "8px", marginBottom: "1rem" }}
        />

        <button type="submit" className="home-btn">Register</button>
      </form>
    </div>
  );
};

export default Register;
