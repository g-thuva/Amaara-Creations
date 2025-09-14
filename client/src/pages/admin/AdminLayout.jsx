import React, { useState, useEffect } from "react";
import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import { FiMenu, FiX, FiHome, FiPackage, FiStar, FiUsers, FiShoppingBag, FiLogOut, FiChevronDown, FiChevronRight } from "react-icons/fi";
import "./AdminStyles.css";

const menuItems = [
  { 
    title: "Dashboard", 
    path: "/admin/dashboard", 
    icon: <FiHome />,
    submenu: []
  },
  { 
    title: "Products", 
    path: "/admin/products", 
    icon: <FiPackage />,
    submenu: []
  },
  { 
    title: "Orders", 
    path: "/admin/orders", 
    icon: <FiShoppingBag />,
    submenu: []
  },
  { 
    title: "Customers", 
    path: "/admin/customers", 
    icon: <FiUsers />,
    submenu: []
  },
  { 
    title: "Reviews", 
    path: "/admin/reviews", 
    icon: <FiStar />,
    submenu: []
  },
];

const AdminLayout = () => {
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const [activeSubmenu, setActiveSubmenu] = useState(null);
  const location = useLocation();
  const navigate = useNavigate();
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

  const handleLogout = () => {
    // TODO: Implement logout logic
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
                <i>{item.icon}</i>
                <span>{item.title}</span>
                {item.submenu.length > 0 && (
                  <span style={{ marginLeft: 'auto' }}>
                    {activeSubmenu === index ? <FiChevronDown /> : <FiChevronRight />}
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
            <i><FiLogOut /></i>
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
              {sidebarOpen ? <FiX /> : <FiMenu />}
            </button>
            <h3 style={{ margin: 0, color: '#2d3748' }}>
              {menuItems.find(item => item.path === location.pathname)?.title || 'Dashboard'}
            </h3>
          </div>
          
          <div className="user-menu">
            <div className="user-info">
              <div className="user-name">Admin User</div>
              <div className="user-role">Administrator</div>
            </div>
            <div className="user-avatar">
              AU
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
