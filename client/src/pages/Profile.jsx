import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { userApi } from "../services/userApi";
import "./Profile.css";
import { FiEdit, FiUser, FiMail, FiPhone, FiMapPin, FiLock, FiShoppingBag, FiHeart, FiLogOut } from "react-icons/fi";

const Profile = () => {
  const [isEditing, setIsEditing] = useState(false);
  const [user, setUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const navigate = useNavigate();
  const { logout, updateUser } = useAuth();

  // Fetch user profile from API
  useEffect(() => {
    const fetchProfile = async () => {
      setIsLoading(true);
      setError("");
      try {
        const profileData = await userApi.getProfile();
        setUser(profileData);
      } catch (err) {
        console.error("Error fetching profile:", err);
        setError("Failed to load profile. Please try again.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchProfile();
  }, []);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setUser(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSave = async (e) => {
    e.preventDefault();
    setIsSaving(true);
    
    try {
      const updatedProfile = await userApi.updateProfile({
        name: user.name,
        phone: user.phone || "",
        address: user.address || "",
        avatarUrl: user.avatarUrl || ""
      });
      
      setUser(updatedProfile);
      updateUser(updatedProfile); // Update AuthContext
      setIsEditing(false);
      alert('Profile updated successfully!');
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to update profile";
      alert(errorMessage);
      console.error("Error updating profile:", err);
    } finally {
      setIsSaving(false);
    }
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  if (isLoading) {
    return (
      <div className="profile-container">
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading profile...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="profile-container">
        <div style={{ textAlign: 'center', padding: '3rem', color: 'red' }}>
          <p>{error}</p>
        </div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="profile-container">
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>No user data available.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="profile-container">
      <div className="profile-header">
        <h1>My Profile</h1>
      </div>

      <div className="profile-content">
        <div className="profile-sidebar">
          <div className="profile-avatar">
            {user.avatarUrl ? (
              <img src={user.avatarUrl} alt={user.name} />
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
                {user.name?.charAt(0).toUpperCase() || 'U'}
              </div>
            )}
          </div>
          <h2 style={{ textAlign: 'center', marginBottom: '0.5rem' }}>{user.name || 'User'}</h2>
          <p style={{ textAlign: 'center', color: '#666', marginBottom: '1.5rem' }}>{user.email || ''}</p>
          
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
                <p className="detail-value">{user.phone || 'Not provided'}</p>
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
                <p className="detail-value">{user.address || 'Not provided'}</p>
              )}
            </div>
            
            {isEditing && (
              <div className="profile-actions">
                <button type="submit" className="btn btn-primary" disabled={isSaving}>
                  {isSaving ? 'Saving...' : 'Save Changes'}
                </button>
                <button 
                  type="button" 
                  className="btn btn-outline"
                  onClick={() => setIsEditing(false)}
                  disabled={isSaving}
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
