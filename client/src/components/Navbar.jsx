import React, { useState, useEffect } from "react";
import { Link, useNavigate, useLocation } from "react-router-dom";
import { FaUserCircle, FaBars, FaTimes, FaHome, FaBox, FaTools, FaHeart, FaShoppingCart } from "react-icons/fa";
import { useAuth } from "../contexts/AuthContext";
import "./Navbar.css";

const Navbar = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated, user, logout } = useAuth();
  const [showDropdown, setShowDropdown] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [isScrolled, setIsScrolled] = useState(false);

  const isAdmin = user?.role === "Admin";

  // Close mobile menu when route changes
  useEffect(() => {
    setIsMobileMenuOpen(false);
  }, [location]);

  // Handle scroll effect
  useEffect(() => {
    const handleScroll = () => {
      if (window.scrollY > 10) {
        setIsScrolled(true);
      } else {
        setIsScrolled(false);
      }
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  const toggleDropdown = (e) => {
    e.stopPropagation();
    setShowDropdown(!showDropdown);
  };

  const toggleMobileMenu = () => {
    setIsMobileMenuOpen(!isMobileMenuOpen);
    if (showDropdown) setShowDropdown(false);
  };

  const handleLogout = async () => {
    setShowDropdown(false);
    await logout();
    navigate("/login");
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    const closeDropdown = (e) => {
      if (showDropdown && !e.target.closest('.profile-menu')) {
        setShowDropdown(false);
      }
    };

    document.addEventListener('click', closeDropdown);
    return () => document.removeEventListener('click', closeDropdown);
  }, [showDropdown]);

  return (
    <>
      {/* Mobile Menu Button */}
      <button 
        className={`mobile-menu-btn ${isMobileMenuOpen ? 'open' : ''}`} 
        onClick={toggleMobileMenu}
        aria-label="Toggle menu"
      >
        {isMobileMenuOpen ? <FaTimes /> : <FaBars />}
      </button>

      {/* Mobile Sidebar Overlay */}
      {isMobileMenuOpen && (
        <div 
          className="mobile-overlay" 
          onClick={() => setIsMobileMenuOpen(false)}
        />
      )}

      {/* Main Navigation */}
      <nav className={`navbar ${isScrolled ? 'scrolled' : ''} ${isMobileMenuOpen ? 'mobile-open' : ''}`}>
        <div className="navbar-container">
          <Link to="/" className="navbar-logo">
            Amaara Creations
          </Link>

          {/* Desktop Navigation */}
          <ul className="desktop-nav">
            <li className={location.pathname === '/' ? 'active' : ''}>
              <Link to="/"><FaHome /> Home</Link>
            </li>
            <li className={location.pathname === '/products' ? 'active' : ''}>
              <Link to="/products"><FaBox /> Products</Link>
            </li>
            <li className={location.pathname === '/custom' ? 'active' : ''}>
              <Link to="/custom"><FaTools /> Custom Builder</Link>
            </li>
            <li className={location.pathname === '/wishlist' ? 'active' : ''}>
              <Link to="/wishlist"><FaHeart /> Wishlist</Link>
            </li>
            <li className={location.pathname === '/cart' ? 'active' : ''}>
              <Link to="/cart"><FaShoppingCart /> Cart</Link>
            </li>
          </ul>

          {/* Mobile Navigation */}
          <div className={`mobile-nav ${isMobileMenuOpen ? 'open' : ''}`}>
            <ul>
              <li className={location.pathname === '/' ? 'active' : ''}>
                <Link to="/"><FaHome /> Home</Link>
              </li>
              <li className={location.pathname === '/products' ? 'active' : ''}>
                <Link to="/products"><FaBox /> Products</Link>
              </li>
              <li className={location.pathname === '/custom' ? 'active' : ''}>
                <Link to="/custom"><FaTools /> Custom Builder</Link>
              </li>
              <li className={location.pathname === '/wishlist' ? 'active' : ''}>
                <Link to="/wishlist"><FaHeart /> Wishlist</Link>
              </li>
              <li className={location.pathname === '/cart' ? 'active' : ''}>
                <Link to="/cart"><FaShoppingCart /> Cart</Link>
              </li>
              
              {/* Auth Buttons */}
              <div className="mobile-auth-buttons">
                {!isAuthenticated ? (
                  <>
                    <Link to="/login" className="btn btn-outline">Login</Link>
                    <Link to="/register" className="btn btn-primary">Register</Link>
                  </>
                ) : (
                  <>
                    <Link to="/profile" className="btn btn-outline">Profile</Link>
                    {isAdmin && <Link to="/admin" className="btn btn-outline">Admin</Link>}
                    <button onClick={handleLogout} className="btn btn-outline">Logout</button>
                  </>
                )}
              </div>
            </ul>
          </div>

          {/* Profile Menu */}
          <div className="profile-menu">
            <FaUserCircle
              size={28}
              className="profile-icon"
              onClick={toggleDropdown}
            />

            {showDropdown && (
              <div className="dropdown">
                {!isAuthenticated ? (
                  <>
                    <Link to="/login" className="dropdown-item">Login</Link>
                    <Link to="/register" className="dropdown-item">Register</Link>
                  </>
                ) : (
                  <>
                    <div className="dropdown-header">{user?.name || user?.email}</div>
                    <Link to="/profile" className="dropdown-item">Profile</Link>
                    <Link to="/orders" className="dropdown-item">My Orders</Link>
                    {isAdmin && <Link to="/admin" className="dropdown-item">Admin Dashboard</Link>}
                    <button onClick={handleLogout} className="dropdown-item">Logout</button>
                  </>
                )}
              </div>
            )}
          </div>
        </div>
      </nav>
    </>
  );
};

export default Navbar;
