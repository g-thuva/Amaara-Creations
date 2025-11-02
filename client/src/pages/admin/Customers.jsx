import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { FiEye, FiRefreshCw, FiMail, FiPhone, FiMapPin } from "react-icons/fi";
import { adminApi } from "../../services/adminApi";
import "./AdminStyles.css";

const Customers = () => {
  const navigate = useNavigate();
  const [customers, setCustomers] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState({
    search: "",
    pageNumber: 1,
    pageSize: 20
  });

  useEffect(() => {
    fetchCustomers();
  }, [filters.search, filters.pageNumber]);

  const fetchCustomers = async () => {
    setIsLoading(true);
    try {
      const response = await adminApi.getAllCustomers({
        search: filters.search || undefined,
        pageNumber: filters.pageNumber,
        pageSize: filters.pageSize
      });
      setCustomers(response.customers || response || []);
    } catch (err) {
      console.error("Error fetching customers:", err);
      alert("Failed to load customers");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Customer Management</h2>
            <p>View and manage all registered customers</p>
          </div>
        </div>
      </div>

      {/* Search */}
      <div style={{ marginBottom: '1.5rem', display: 'flex', gap: '1rem' }}>
        <input
          type="text"
          placeholder="Search by name or email..."
          value={filters.search}
          onChange={(e) => setFilters({ ...filters, search: e.target.value, pageNumber: 1 })}
          style={{ flex: 1, padding: '0.75rem', borderRadius: '4px', border: '1px solid #ddd' }}
        />
        <button onClick={fetchCustomers} className="btn btn-outline" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <FiRefreshCw /> Refresh
        </button>
      </div>

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading customers...</p>
        </div>
      ) : (
        <div className="table-responsive">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Phone</th>
                <th>Address</th>
                <th>Joined</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {customers.length === 0 ? (
                <tr>
                  <td colSpan="6" style={{ textAlign: 'center', padding: '2rem' }}>
                    No customers found
                  </td>
                </tr>
              ) : (
                customers.map((customer) => (
                  <tr key={customer.id}>
                    <td>
                      <strong>{customer.name || 'N/A'}</strong>
                    </td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                        <FiMail size={14} />
                        {customer.email || 'N/A'}
                      </div>
                    </td>
                    <td>
                      {customer.phone ? (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <FiPhone size={14} />
                          {customer.phone}
                        </div>
                      ) : (
                        'N/A'
                      )}
                    </td>
                    <td>
                      {customer.address ? (
                        <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                          <FiMapPin size={14} />
                          <span style={{ maxWidth: '200px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                            {customer.address}
                          </span>
                        </div>
                      ) : (
                        'N/A'
                      )}
                    </td>
                    <td>{new Date(customer.createdAt).toLocaleDateString()}</td>
                    <td>
                      <button
                        className="btn-edit"
                        onClick={() => navigate(`/admin/customers/${customer.id}`)}
                        title="View Details"
                      >
                        <FiEye /> View
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default Customers;
