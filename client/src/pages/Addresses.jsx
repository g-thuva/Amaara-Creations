import React, { useEffect, useState } from 'react';
import { addressApi } from '../services/addressApi';
import './Profile.css';

const emptyForm = {
  label: 'Home',
  recipientName: '',
  phone: '',
  addressLine1: '',
  addressLine2: '',
  city: '',
  districtOrProvince: '',
  postalCode: '',
  country: 'Sri Lanka',
  isDefault: false
};

const Addresses = () => {
  const [addresses, setAddresses] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState('');

  const loadAddresses = async () => {
    setIsLoading(true);
    setError('');
    try {
      setAddresses(await addressApi.getAddresses());
    } catch {
      setError('Failed to load addresses.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadAddresses();
  }, []);

  const editAddress = (address) => {
    setEditingId(address.id);
    setForm({
      label: address.label,
      recipientName: address.recipientName,
      phone: address.phone || '',
      addressLine1: address.addressLine1,
      addressLine2: address.addressLine2 || '',
      city: address.city,
      districtOrProvince: address.districtOrProvince || '',
      postalCode: address.postalCode || '',
      country: address.country,
      isDefault: address.isDefault
    });
  };

  const resetForm = () => {
    setEditingId(null);
    setForm(emptyForm);
  };

  const saveAddress = async (event) => {
    event.preventDefault();
    setIsSaving(true);
    setError('');
    try {
      if (editingId) {
        await addressApi.updateAddress(editingId, form);
      } else {
        await addressApi.createAddress(form);
      }
      resetForm();
      await loadAddresses();
    } catch (err) {
      setError(err.response?.data?.message || 'Failed to save address.');
    } finally {
      setIsSaving(false);
    }
  };

  const deleteAddress = async (id) => {
    if (!window.confirm('Delete this address?')) return;
    await addressApi.deleteAddress(id);
    await loadAddresses();
  };

  const setDefault = async (id) => {
    await addressApi.setDefault(id);
    await loadAddresses();
  };

  return (
    <div className="profile-container">
      <div className="profile-header">
        <h1>Saved Addresses</h1>
      </div>
      {error && <div className="error-message">{error}</div>}
      <div className="profile-content">
        <div className="profile-details">
          <h2>{editingId ? 'Edit Address' : 'Add Address'}</h2>
          <form onSubmit={saveAddress}>
            {['label', 'recipientName', 'phone', 'addressLine1', 'addressLine2', 'city', 'districtOrProvince', 'postalCode', 'country'].map((field) => (
              <div className="detail-group" key={field}>
                <label className="detail-label" htmlFor={field}>{field.replace(/([A-Z])/g, ' $1').replace(/^./, c => c.toUpperCase())}</label>
                <input
                  id={field}
                  className="form-control"
                  required={['label', 'recipientName', 'addressLine1', 'city', 'country'].includes(field)}
                  value={form[field]}
                  onChange={(e) => setForm({ ...form, [field]: e.target.value })}
                />
              </div>
            ))}
            <label style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', marginBottom: '1rem' }}>
              <input type="checkbox" checked={form.isDefault} onChange={(e) => setForm({ ...form, isDefault: e.target.checked })} />
              Default address
            </label>
            <div className="profile-actions">
              <button className="btn btn-primary" type="submit" disabled={isSaving}>{isSaving ? 'Saving...' : 'Save Address'}</button>
              {editingId && <button className="btn btn-outline" type="button" onClick={resetForm}>Cancel</button>}
            </div>
          </form>
        </div>
        <div className="profile-details">
          <h2>Your Addresses</h2>
          {isLoading ? <p>Loading addresses...</p> : addresses.length === 0 ? <p>No saved addresses yet.</p> : addresses.map((address) => (
            <div className="detail-group" key={address.id}>
              <strong>{address.label}{address.isDefault ? ' (Default)' : ''}</strong>
              <p className="detail-value">{address.recipientName}</p>
              <p>{address.addressLine1}{address.addressLine2 ? `, ${address.addressLine2}` : ''}</p>
              <p>{address.city}, {address.country}</p>
              <div className="profile-actions">
                <button className="btn btn-outline" onClick={() => editAddress(address)}>Edit</button>
                {!address.isDefault && <button className="btn btn-outline" onClick={() => setDefault(address.id)}>Set Default</button>}
                <button className="btn btn-outline" onClick={() => deleteAddress(address.id)}>Delete</button>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Addresses;
