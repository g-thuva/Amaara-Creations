import React, { useState, useEffect, useRef } from "react";
import { Link, Outlet, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../../contexts/AuthContext";
import { can, adminRoutes } from "../../utils/permissions";
import AppIcon from "../../components/AppIcon";
import "./AdminStyles.css";

const menuItems = [
  { title: 'Payments', path: '/admin/payments', icon: 'bag', submenu: [] },
  { title: 'Inventory', path: '/admin/inventory', icon: 'package', submenu: [] },
  { title: 'Promotions', path: '/admin/promotions', icon: 'tag', submenu: [] },
  { title: 'Reports', path: '/admin/reports', icon: 'file', submenu: [] },
  { title: 'Audit log', path: '/admin/audit', icon: 'file', submenu: [] },
  { title: 'Staff', path: '/admin/staff', icon: 'users', submenu: [] },
  { title: "Shipping", path: "/admin/shipping", icon: "truck", submenu: [] },
  { 
    title: "Dashboard", 
    path: "/admin/dashboard", 
    icon: "home",
    submenu: []
  },
  { 
    title: "Products", 
    path: "/admin/products", 
    icon: "package",
    submenu: []
  },
  {
    title: "Categories",
    path: "/admin/categories",
    icon: "tag",
    submenu: []
  },
  {
    title: "Collections",
    path: "/admin/collections",
    icon: "layers",
    submenu: []
  },
  {
    title: "Media",
    path: "/admin/media",
    icon: "images",
    submenu: []
  },
  {
    title: "Content",
    path: "/admin/content",
    icon: "file",
    submenu: []
  },
  {
    title: "Settings",
    path: "/admin/settings",
    icon: "settings",
    submenu: []
  },
  {
    title: "Custom Builder",
    path: "/admin/custom-builder",
    icon: "edit",
    submenu: []
  },
  {
    title: "Custom Designs",
    path: "/admin/custom-designs",
    icon: "layers",
    submenu: []
  },
  { 
    title: "Orders", 
    path: "/admin/orders", 
    icon: "bag",
    submenu: []
  },
  { 
    title: "Customers", 
    path: "/admin/customers", 
    icon: "users",
    submenu: []
  },
  { 
    title: "Reviews", 
    path: "/admin/reviews", 
    icon: "star",
    submenu: []
  },
];

const AdminLayout = () => {
  const navigationToggle = useRef(null);
  const [sidebarOpen, setSidebarOpen] = useState(() => window.innerWidth > 992);
  const [activeSubmenu, setActiveSubmenu] = useState(null);
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout: authLogout } = useAuth();
  const [isMobile, setIsMobile] = useState(() => window.innerWidth <= 992);

  useEffect(() => {
    const media = window.matchMedia('(max-width: 992px)');
    const changed = () => setIsMobile(media.matches);
    media.addEventListener('change', changed);
    return () => media.removeEventListener('change', changed);
  }, []);

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
      <aside inert={isMobile && !sidebarOpen ? true : undefined} aria-hidden={isMobile && !sidebarOpen ? true : undefined} className={`admin-sidebar ${sidebarOpen ? 'open' : ''}`} onKeyDown={event => { if (isMobile && event.key === 'Escape') { setSidebarOpen(false); navigationToggle.current?.focus(); } }}>
        <div className="sidebar-header">
          <h2>
            <span style={{ color: '#d4a76a' }}>Amaara</span> Admin
          </h2>
        </div>
        
        <nav className="sidebar-menu" aria-label="Administration">
          {menuItems.filter(item => can(user, adminRoutes[item.path.split("/")[2]])).map((item, index) => (
            <React.Fragment key={index}>
              <Link
                to={item.path}
                className={`menu-item ${location.pathname === item.path ? 'active' : ''}`}
                onClick={() => {
                  if (isMobile) setSidebarOpen(false);
                  if (item.submenu.length > 0) {
                    toggleSubmenu(index);
                  }
                }}
              >
                <AppIcon name={item.icon} />
                <span>{item.title}</span>
                {item.submenu.length > 0 && (
                  <span style={{ marginLeft: 'auto' }}>
                    <AppIcon name={activeSubmenu === index ? 'chevronDown' : 'chevronRight'} />
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
          
          <button type="button" className="menu-item" onClick={handleLogout}>
            <AppIcon name="logout" />
            <span>Logout</span>
          </button>
        </nav>
      </aside>

      {/* Main Content */}
      <div className={`admin-main ${sidebarOpen ? 'sidebar-open' : ''}`}>
        {/* Header */}
        <header className="admin-header">
          <div className="header-left">
            <button ref={navigationToggle} className="toggle-sidebar" aria-label="Toggle admin navigation" aria-expanded={sidebarOpen} onClick={toggleSidebar}>
              <AppIcon name={sidebarOpen ? 'x' : 'menu'} />
            </button>
            <button 
              className="back-button" 
              onClick={() => navigate('/')}
              title="Back to Home"
            >
              <AppIcon name="arrowLeft" />
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
        <div className="admin-content">
          <div>
            <Outlet />
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminLayout;
