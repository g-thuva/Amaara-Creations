import React, { useState } from "react";
import { useNavigate } from "react-router-dom";

const Login = () => {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const navigate = useNavigate();

  const handleLogin = (e) => {
    e.preventDefault();
    // TODO: connect to backend
    console.log("Login", { email, password });
    alert("Logged in!");
    navigate("/profile");
  };

  return (
    <div className="container" style={{ padding: "2rem 0", maxWidth: "400px" }}>
      <h2 style={{ marginBottom: "1rem" }}>Login</h2>
      <form onSubmit={handleLogin}>
        <label>Email</label>
        <input
          type="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          style={{ width: "100%", padding: "8px", marginBottom: "1rem" }}
        />

        <label>Password</label>
        <input
          type="password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          style={{ width: "100%", padding: "8px", marginBottom: "1rem" }}
        />

        <button type="submit" className="home-btn">Login</button>
      </form>
    </div>
  );
};

export default Login;
