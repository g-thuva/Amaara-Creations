import React, { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { FaUserCircle } from "react-icons/fa";

const Navbar = () => {
  const navigate = useNavigate();
  const [showDropdown, setShowDropdown] = useState(false);

  // simulate auth state (replace with real context/auth check later)
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  const toggleDropdown = () => {
    setShowDropdown(!showDropdown);
  };

  const handleLogout = () => {
    setIsLoggedIn(false);
    setShowDropdown(false);
    alert("Logged out");
  };

  return (
    <nav className="navbar">
      <div className="navbar-container">
        <Link to="/" className="navbar-logo">
          Amaara Creations
        </Link>

        <ul className="navbar-links">
          <li><Link to="/products">Products</Link></li>
          <li><Link to="/custom">Custom Builder</Link></li>
          <li><Link to="/wishlist">Wishlist</Link></li>
          <li><Link to="/cart">Cart</Link></li>

          <li className="profile-menu">
            <FaUserCircle
              size={24}
              color="#fff"
              style={{ cursor: "pointer" }}
              onClick={toggleDropdown}
            />

            {showDropdown && (
              <div className="dropdown">
                {!isLoggedIn ? (
                  <>
                    <button onClick={() => { navigate("/login"); setShowDropdown(false); }}>Login</button>
                    <button onClick={() => { navigate("/register"); setShowDropdown(false); }}>Register</button>
                  </>
                ) : (
                  <>
                    <button onClick={() => { navigate("/profile"); setShowDropdown(false); }}>Profile</button>
                    <button onClick={() => { navigate("/orders"); setShowDropdown(false); }}>My Orders</button>
                    <button onClick={handleLogout}>Logout</button>
                  </>
                )}
              </div>
            )}
          </li>
        </ul>
      </div>
    </nav>
  );
};

export default Navbar;
