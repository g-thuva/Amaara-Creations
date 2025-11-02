import React, { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { FiDollarSign, FiShoppingBag, FiUsers, FiStar, FiTrendingUp } from "react-icons/fi";
import { adminApi } from "../../services/adminApi";

const Dashboard = () => {
  const [dashboardStats, setDashboardStats] = useState(null);
  const [recentOrders, setRecentOrders] = useState([]);
  const [topProducts, setTopProducts] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const fetchDashboardData = async () => {
      setIsLoading(true);
      setError("");
      try {
        // Fetch dashboard stats
        const statsData = await adminApi.getDashboardStats();
        setDashboardStats(statsData);

        // Fetch recent orders
        const recentData = await adminApi.getRecentOrders(5);
        setRecentOrders(recentData || []);

        // Fetch product stats for top products
        const productStats = await adminApi.getProductStats();
        setTopProducts(productStats.topSellingProducts || []);
      } catch (err) {
        console.error("Error fetching dashboard data:", err);
        setError("Failed to load dashboard data");
      } finally {
        setIsLoading(false);
      }
    };

    fetchDashboardData();
  }, []);

  if (isLoading) {
    return (
      <div className="dashboard">
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading dashboard...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="dashboard">
        <div style={{ textAlign: 'center', padding: '3rem', color: 'red' }}>
          <p>{error}</p>
        </div>
      </div>
    );
  }

  const stats = [
    { 
      title: "Total Revenue", 
      value: `Rs. ${dashboardStats?.totalRevenue?.toLocaleString() || '0'}`, 
      change: dashboardStats?.revenueThisMonth ? `+${((dashboardStats.revenueThisMonth / (dashboardStats.totalRevenue || 1)) * 100).toFixed(1)}%` : '0%', 
      trend: "up", 
      icon: <FiDollarSign size={24} />
    },
    { 
      title: "Total Orders", 
      value: dashboardStats?.totalOrders?.toLocaleString() || '0', 
      change: dashboardStats?.ordersThisMonth ? `+${((dashboardStats.ordersThisMonth / (dashboardStats.totalOrders || 1)) * 100).toFixed(1)}%` : '0%', 
      trend: "up", 
      icon: <FiShoppingBag size={24} />
    },
    { 
      title: "Total Customers", 
      value: dashboardStats?.totalCustomers?.toLocaleString() || '0', 
      change: "+0%", 
      trend: "up", 
      icon: <FiUsers size={24} />
    },
    { 
      title: "Avg. Rating", 
      value: dashboardStats?.averageRating?.toFixed(1) || '0.0', 
      change: "+0.0", 
      trend: "up", 
      icon: <FiStar size={24} />
    },
  ];

  const getStatusColor = (status) => {
    const colors = {
      'Pending': 'pending',
      'Processing': 'processing',
      'Shipped': 'shipped',
      'Delivered': 'completed',
      'Cancelled': 'pending'
    };
    return colors[status] || 'pending';
  };

  return (
    <div className="dashboard">
      <div className="dashboard-header">
        <h1>Dashboard</h1>
        <p>Welcome back! Here's what's happening with your store today.</p>
      </div>

      {/* Stats Cards */}
      <div className="stats-grid">
        {stats.map((stat, index) => (
          <div key={index} className="stat-card">
            <div className="stat-icon" style={{ background: 'rgba(139, 90, 43, 0.1)', color: '#8b5a2b' }}>
              {stat.icon}
            </div>
            <div className="stat-info">
              <span className="stat-value">{stat.value}</span>
              <span className="stat-title">{stat.title}</span>
            </div>
            <div className={`stat-change ${stat.trend}`}>
              <FiTrendingUp size={16} />
              <span>{stat.change}</span>
            </div>
          </div>
        ))}
      </div>

      <div className="dashboard-row">
        {/* Recent Orders */}
        <div className="dashboard-card">
          <div className="card-header">
            <h3>Recent Orders</h3>
            <Link to="/admin/orders" className="view-all">View All</Link>
          </div>
          <div className="table-responsive">
            <table>
              <thead>
                <tr>
                  <th>Order ID</th>
                  <th>Customer</th>
                  <th>Date</th>
                  <th>Amount</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {recentOrders.length === 0 ? (
                  <tr>
                    <td colSpan="5" style={{ textAlign: 'center', padding: '2rem' }}>
                      No orders yet
                    </td>
                  </tr>
                ) : (
                  recentOrders.map((order) => (
                    <tr key={order.id}>
                      <td>
                        <Link to={`/admin/orders/${order.id}`} className="order-link">
                          {order.orderNumber || `#${order.id}`}
                        </Link>
                      </td>
                      <td>{order.customerName || order.customerEmail}</td>
                      <td>{new Date(order.orderDate).toLocaleDateString()}</td>
                      <td>Rs. {order.total?.toLocaleString()}</td>
                      <td>
                        <span className={`status-badge ${getStatusColor(order.status)}`}>
                          {order.status}
                        </span>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Top Products */}
        <div className="dashboard-card">
          <div className="card-header">
            <h3>Top Selling Products</h3>
            <Link to="/admin/products" className="view-all">View All</Link>
          </div>
          <div className="product-list">
            {topProducts.length === 0 ? (
              <div style={{ padding: '2rem', textAlign: 'center' }}>
                <p>No products yet</p>
              </div>
            ) : (
              topProducts.slice(0, 5).map((product, index) => (
                <div key={index} className="product-item">
                  <div className="product-info">
                    <h4>{product.productName}</h4>
                    <div className="product-meta">
                      <span>{product.quantitySold} sales</span>
                    </div>
                  </div>
                  <div className="product-chart">
                    <div 
                      className="chart-bar" 
                      style={{ width: `${(product.quantitySold / (topProducts[0]?.quantitySold || 1)) * 100}%` }}
                    ></div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>

      <style jsx>{`
        .dashboard {
          padding: 1rem 0;
        }
        
        .dashboard-header {
          margin-bottom: 2rem;
        }
        
        .dashboard-header h1 {
          font-size: 1.75rem;
          color: #2d3748;
          margin-bottom: 0.5rem;
        }
        
        .dashboard-header p {
          color: #718096;
          margin: 0;
        }
        
        .stats-grid {
          display: grid;
          grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
          gap: 1.5rem;
          margin-bottom: 2rem;
        }
        
        .stat-card {
          background: #fff;
          border-radius: 0.5rem;
          padding: 1.5rem;
          display: flex;
          align-items: center;
          box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
          transition: transform 0.2s, box-shadow 0.2s;
        }
        
        .stat-card:hover {
          transform: translateY(-2px);
          box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }
        
        .stat-icon {
          width: 48px;
          height: 48px;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          margin-right: 1rem;
        }
        
        .stat-info {
          flex: 1;
        }
        
        .stat-value {
          display: block;
          font-size: 1.25rem;
          font-weight: 600;
          color: #2d3748;
          line-height: 1.2;
        }
        
        .stat-title {
          font-size: 0.875rem;
          color: #718096;
        }
        
        .stat-change {
          display: flex;
          align-items: center;
          font-size: 0.875rem;
          font-weight: 500;
          padding: 0.25rem 0.5rem;
          border-radius: 9999px;
        }
        
        .stat-change.up {
          color: #38a169;
          background: rgba(72, 187, 120, 0.1);
        }
        
        .stat-change.down {
          color: #e53e3e;
          background: rgba(229, 62, 62, 0.1);
        }
        
        .stat-change svg {
          margin-right: 0.25rem;
        }
        
        .dashboard-row {
          display: grid;
          grid-template-columns: 2fr 1fr;
          gap: 1.5rem;
          margin-bottom: 2rem;
        }
        
        @media (max-width: 1200px) {
          .dashboard-row {
            grid-template-columns: 1fr;
          }
        }
        
        .dashboard-card {
          background: #fff;
          border-radius: 0.5rem;
          box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
          overflow: hidden;
        }
        
        .card-header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 1.25rem 1.5rem;
          border-bottom: 1px solid #edf2f7;
        }
        
        .card-header h3 {
          margin: 0;
          font-size: 1.125rem;
          color: #2d3748;
        }
        
        .view-all {
          color: #8b5a2b;
          font-size: 0.875rem;
          font-weight: 500;
          text-decoration: none;
          transition: color 0.2s;
        }
        
        .view-all:hover {
          color: #6b4620;
          text-decoration: underline;
        }
        
        table {
          width: 100%;
          border-collapse: collapse;
        }
        
        th, td {
          padding: 1rem 1.5rem;
          text-align: left;
          border-bottom: 1px solid #edf2f7;
        }
        
        th {
          font-size: 0.75rem;
          font-weight: 600;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          color: #718096;
          background: #f8fafc;
        }
        
        tr:hover {
          background: #f8fafc;
        }
        
        .order-link {
          color: #3182ce;
          text-decoration: none;
          font-weight: 500;
        }
        
        .order-link:hover {
          text-decoration: underline;
        }
        
        .status-badge {
          display: inline-block;
          padding: 0.25rem 0.5rem;
          border-radius: 9999px;
          font-size: 0.75rem;
          font-weight: 500;
          text-transform: capitalize;
        }
        
        .status-badge.completed {
          background: #c6f6d5;
          color: #22543d;
        }
        
        .status-badge.processing {
          background: #bee3f8;
          color: #2a4365;
        }
        
        .status-badge.shipped {
          background: #e9d8fd;
          color: #44337a;
        }
        
        .status-badge.pending {
          background: #feebc8;
          color: #7b341e;
        }
        
        .product-list {
          padding: 0.5rem 0;
        }
        
        .product-item {
          padding: 1rem 1.5rem;
          border-bottom: 1px solid #edf2f7;
        }
        
        .product-item:last-child {
          border-bottom: none;
        }
        
        .product-item h4 {
          margin: 0 0 0.25rem;
          font-size: 0.9375rem;
          color: #2d3748;
          font-weight: 500;
        }
        
        .product-meta {
          display: flex;
          align-items: center;
          font-size: 0.8125rem;
          color: #718096;
          margin-bottom: 0.5rem;
        }
        
        .product-meta span {
          margin-right: 0.5rem;
        }
        
        .product-chart {
          height: 6px;
          background: #edf2f7;
          border-radius: 3px;
          overflow: hidden;
        }
        
        .chart-bar {
          height: 100%;
          background: #8b5a2b;
          border-radius: 3px;
        }
      `}</style>
    </div>
  );
};

export default Dashboard;
