import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import "./Profile.css";
import { FiEdit, FiUser, FiMail, FiPhone, FiMapPin, FiLock, FiShoppingBag, FiHeart, FiLogOut } from "react-icons/fi";

const Profile = () => {
  const [isEditing, setIsEditing] = useState(false);
  const navigate = useNavigate();
  
  // Replace with real user data from context/state later
  const [user, setUser] = useState({
    name: "Thuva G",
    email: "thuva@example.com",
    phone: "0771234567",
    address: "123 Sticker Lane, Jaffna"
  });

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setUser(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSave = (e) => {
    e.preventDefault();
    // TODO: Connect to backend to save changes
    setIsEditing(false);
    // Show success message
    alert('Profile updated successfully!');
  };

  const handleLogout = () => {
    // TODO: Implement logout logic
    navigate('/login');
  };

  return (
    <div className="profile-container">
      <div className="profile-header">
        <h1>My Profile</h1>
      </div>

      <div className="profile-content">
        <div className="profile-sidebar">
          <div className="profile-avatar">
            {user.avatar ? (
              <img src={user.avatar} alt={user.name} />
            ) : (
              <div style={{
                width: '100%',
                height: '100%',
                backgroundColor: '#f0f0f0',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: '3rem',
                color: '#8b5a2b'
              }}>
                {user.name.charAt(0).toUpperCase()}
              </div>
            )}
          </div>
          <h2 style={{ textAlign: 'center', marginBottom: '0.5rem' }}>{user.name}</h2>
          <p style={{ textAlign: 'center', color: '#666', marginBottom: '1.5rem' }}>{user.email}</p>
          
          <div className="profile-actions">
            <button 
              className="btn btn-outline" 
              onClick={() => setIsEditing(!isEditing)}
              style={{ flex: 1 }}
            >
              <FiEdit /> {isEditing ? 'Cancel' : 'Edit Profile'}
            </button>
            <button 
              className="btn btn-outline"
              onClick={handleLogout}
              style={{ flex: 1 }}
            >
              <FiLogOut /> Logout
            </button>
          </div>
          
          <div style={{ marginTop: '2rem' }}>
            <button 
              className="btn btn-outline"
              onClick={() => navigate('/orders')}
              style={{ width: '100%', marginBottom: '0.75rem' }}
            >
              <FiShoppingBag /> My Orders
            </button>
            <button 
              className="btn btn-outline"
              onClick={() => navigate('/wishlist')}
              style={{ width: '100%' }}
            >
              <FiHeart /> Wishlist
            </button>
          </div>
        </div>

        <div className="profile-details">
          <h2 style={{ marginBottom: '1.5rem', color: '#333' }}>Personal Information</h2>
          
          <form onSubmit={handleSave}>
            <div className="detail-group">
              <label className="detail-label">
                <FiUser style={{ marginRight: '8px' }} /> Full Name
              </label>
              {isEditing ? (
                <input
                  type="text"
                  name="name"
                  value={user.name}
                  onChange={handleInputChange}
                  className="form-control"
                  style={{ width: '100%', padding: '0.5rem' }}
                />
              ) : (
                <p className="detail-value">{user.name}</p>
              )}
            </div>
            
            <div className="detail-group">
              <label className="detail-label">
                <FiMail style={{ marginRight: '8px' }} /> Email Address
              </label>
              <p className="detail-value">{user.email}</p>
              <small style={{ color: '#666' }}>Contact support to change your email</small>
            </div>
            
            <div className="detail-group">
              <label className="detail-label">
                <FiPhone style={{ marginRight: '8px' }} /> Phone Number
              </label>
              {isEditing ? (
                <input
                  type="tel"
                  name="phone"
                  value={user.phone}
                  onChange={handleInputChange}
                  className="form-control"
                  style={{ width: '100%', padding: '0.5rem' }}
                />
              ) : (
                <p className="detail-value">{user.phone}</p>
              )}
            </div>
            
            <div className="detail-group">
              <label className="detail-label">
                <FiMapPin style={{ marginRight: '8px' }} /> Delivery Address
              </label>
              {isEditing ? (
                <textarea
                  name="address"
                  value={user.address}
                  onChange={handleInputChange}
                  className="form-control"
                  rows="3"
                  style={{ width: '100%', padding: '0.5rem' }}
                />
              ) : (
                <p className="detail-value">{user.address}</p>
              )}
            </div>
            
            {isEditing && (
              <div className="profile-actions">
                <button type="submit" className="btn btn-primary">
                  Save Changes
                </button>
                <button 
                  type="button" 
                  className="btn btn-outline"
                  onClick={() => setIsEditing(false)}
                >
                  Cancel
                </button>
              </div>
            )}
          </form>
          
          <div className="detail-group" style={{ marginTop: '2rem' }}>
            <h3 style={{ marginBottom: '1rem' }}>Account Security</h3>
            <button 
              className="btn btn-outline"
              onClick={() => navigate('/change-password')}
            >
              <FiLock /> Change Password
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Profile;
