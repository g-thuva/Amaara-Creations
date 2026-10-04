import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi } from '../services/authApi';
import { useAuth } from '../contexts/AuthContext';
import './Profile.css';

const AccountSecurity = () => {
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmPassword: '' });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const { logout } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (event) => {
    event.preventDefault();
    setMessage('');
    setError('');

    if (form.newPassword !== form.confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setIsSaving(true);
    try {
      const response = await authApi.changePassword(form);
      setMessage(response.message);
      await logout();
      setTimeout(() => navigate('/login', { replace: true, state: { message: 'Password changed. Please sign in again.' } }), 300);
    } catch (err) {
      setError(err.response?.data?.message || 'Password change failed.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="profile-container">
      <div className="profile-header">
        <h1>Account Security</h1>
      </div>
      <div className="profile-details">
        {message && <div className="success-message">{message}</div>}
        {error && <div className="error-message">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="detail-group">
            <label className="detail-label" htmlFor="currentPassword">Current Password</label>
            <input id="currentPassword" type="password" className="form-control" required value={form.currentPassword} onChange={(e) => setForm({ ...form, currentPassword: e.target.value })} />
          </div>
          <div className="detail-group">
            <label className="detail-label" htmlFor="newPassword">New Password</label>
            <input id="newPassword" type="password" className="form-control" required minLength="6" value={form.newPassword} onChange={(e) => setForm({ ...form, newPassword: e.target.value })} />
          </div>
          <div className="detail-group">
            <label className="detail-label" htmlFor="confirmPassword">Confirm New Password</label>
            <input id="confirmPassword" type="password" className="form-control" required minLength="6" value={form.confirmPassword} onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })} />
          </div>
          <div className="profile-actions">
            <button type="submit" className="btn btn-primary" disabled={isSaving}>{isSaving ? 'Saving...' : 'Change Password'}</button>
            <button type="button" className="btn btn-outline" onClick={() => navigate('/profile')}>Back to Profile</button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default AccountSecurity;
