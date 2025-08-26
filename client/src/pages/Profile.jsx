import React from "react";

const Profile = () => {
  // Replace with real user data later
  const user = {
    name: "Thuva G",
    email: "thuva@example.com",
    phone: "0771234567",
    address: "123 Sticker Lane, Jaffna"
  };

  return (
    <div className="container" style={{ padding: "2rem 0" }}>
      <h2 style={{ fontSize: "2rem", marginBottom: "1rem" }}>My Profile</h2>

      <div style={{ background: "#fff", padding: "1.5rem", borderRadius: "8px", maxWidth: "500px" }}>
        <p><strong>Name:</strong> {user.name}</p>
        <p><strong>Email:</strong> {user.email}</p>
        <p><strong>Phone:</strong> {user.phone}</p>
        <p><strong>Address:</strong> {user.address}</p>
        <button className="home-btn" style={{ marginTop: "1rem" }}>Edit Profile</button>
      </div>
    </div>
  );
};

export default Profile;
