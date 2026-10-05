import React, { useState, useEffect } from "react";
import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import "./AdminStyles.css";

const menuItems = [
  { 
    title: "Dashboard", 
    path: "/admin/dashboard", 
    icon: "fa-solid fa-house",
    submenu: []
  },
  { 
    title: "Products", 
    path: "/admin/products", 
    icon: "fa-solid fa-box",
    submenu: []
  },
  {
    title: "Categories",
    path: "/admin/categories",
    icon: "fa-solid fa-tags",
    submenu: []
  },
  {
    title: "Collections",
    path: "/admin/collections",
    icon: "fa-solid fa-layer-group",
    submenu: []
  },
  {
    title: "Media",
    path: "/admin/media",
    icon: "fa-solid fa-images",
    submenu: []
  },
  {
    title: "Content",
    path: "/admin/content",
    icon: "fa-solid fa-file-lines",
    submenu: []
  },
  {
    title: "Settings",
    path: "/admin/settings",
    icon: "fa-solid fa-gear",
    submenu: []
  },
  { 
    title: "Orders", 
    path: "/admin/orders", 
    icon: "fa-solid fa-bag-shopping",
    submenu: []
  },
  { 
    title: "Customers", 
    path: "/admin/customers", 
    icon: "fa-solid fa-users",
    submenu: []
  },
  { 
    title: "Reviews", 
    path: "/admin/reviews", 
    icon: "fa-solid fa-star",
    submenu: []
  },
];

const AdminLayout = () => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [activeSubmenu, setActiveSubmenu] = useState(null);
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout: authLogout } = useAuth();
  const isMobile = window.innerWidth <= 992;

  useEffect(() => {
    if (isMobile) {
      setSidebarOpen(false);
    }
  }, [isMobile]);

  const toggleSidebar = () => {
    setSidebarOpen(!sidebarOpen);
  };

  const toggleSubmenu = (index) => {
    setActiveSubmenu(activeSubmenu === index ? null : index);
  };

  const handleLogout = async () => {
    await authLogout();
    navigate('/login');
  };

  // Close sidebar when clicking outside on mobile
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (isMobile && sidebarOpen && !e.target.closest('.admin-sidebar')) {
        setSidebarOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [isMobile, sidebarOpen]);

  return (
    <div className="admin-container">
      {/* Sidebar */}
      <aside className={`admin-sidebar ${sidebarOpen ? 'open' : ''}`}>
        <div className="sidebar-header">
          <h2>
            <span style={{ color: '#d4a76a' }}>Amaara</span> Admin
          </h2>
        </div>
        
        <nav className="sidebar-menu">
          {menuItems.map((item, index) => (
            <React.Fragment key={index}>
              <Link
                to={item.path}
                className={`menu-item ${location.pathname === item.path ? 'active' : ''}`}
                onClick={() => {
                  if (item.submenu.length > 0) {
                    toggleSubmenu(index);
                  }
                }}
              >
                <i className={item.icon} />
                <span>{item.title}</span>
                {item.submenu.length > 0 && (
                  <span style={{ marginLeft: 'auto' }}>
                    <i className={`fa-solid ${activeSubmenu === index ? 'fa-chevron-down' : 'fa-chevron-right'}`} />
                  </span>
                )}
              </Link>
              
              {item.submenu.length > 0 && activeSubmenu === index && (
                <div className="submenu" style={{ paddingLeft: '2.5rem', paddingTop: '0.25rem' }}>
                  {item.submenu.map((subItem, subIndex) => (
                    <Link
                      key={subIndex}
                      to={subItem.path}
                      className="menu-item"
                      style={{
                        padding: '0.5rem 1rem',
                        fontSize: '0.9rem',
                        opacity: 0.9,
                        display: 'block',
                        marginBottom: '0.25rem'
                      }}
                    >
                      {subItem.title}
                    </Link>
                  ))}
                </div>
              )}
            </React.Fragment>
          ))}
          
          <div className="menu-item" onClick={handleLogout} style={{ cursor: 'pointer' }}>
            <i className="fa-solid fa-right-from-bracket" />
            <span>Logout</span>
          </div>
        </nav>
      </aside>

      {/* Main Content */}
      <div className={`admin-main ${sidebarOpen ? 'sidebar-open' : ''}`}>
        {/* Header */}
        <header className="admin-header">
          <div className="header-left">
            <button className="toggle-sidebar" onClick={toggleSidebar}>
              <i className={`fa-solid ${sidebarOpen ? 'fa-xmark' : 'fa-bars'}`} />
            </button>
            <button 
              className="back-button" 
              onClick={() => navigate('/')}
              title="Back to Home"
            >
              <i className="fa-solid fa-arrow-left" />
            </button>
            <h3 style={{ margin: 0, color: '#2d3748' }}>
              {menuItems.find(item => item.path === location.pathname)?.title || 'Dashboard'}
            </h3>
          </div>
          
          <div className="user-menu">
            <div className="user-info">
              <div className="user-name">{user?.name || 'Admin'}</div>
              <div className="user-role">{user?.role || 'Administrator'}</div>
            </div>
            <div className="user-avatar">
              {user?.name?.charAt(0).toUpperCase() || 'A'}
            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="admin-content">
          <div className="fade-in">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
};

export default AdminLayout;
