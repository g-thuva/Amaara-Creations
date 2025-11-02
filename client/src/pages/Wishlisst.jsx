import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { wishlistApi } from "../services/wishlistApi";
import { cartApi } from "../services/cartApi";
import "./Wishlist.css";

const Wishlist = () => {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [wishlist, setWishlist] = useState([]);
  const [isLoading, setIsLoading] = useState(true);

  // Fetch wishlist items
  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login", { state: { from: "/wishlist" } });
      return;
    }

    const fetchWishlist = async () => {
      setIsLoading(true);
      try {
        const wishlistData = await wishlistApi.getWishlist();
        setWishlist(wishlistData.items || []);
      } catch (err) {
        console.error("Error fetching wishlist:", err);
        alert("Failed to load wishlist. Please try again.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchWishlist();
  }, [isAuthenticated, navigate]);

  const handleRemove = async (productId) => {
    try {
      await wishlistApi.removeFromWishlist(productId);
      const wishlistData = await wishlistApi.getWishlist();
      setWishlist(wishlistData.items || []);
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to remove item";
      alert(errorMessage);
    }
  };

  const handleAddToCart = async (item) => {
    try {
      await cartApi.addToCart(item.productId, 1);
      alert(`${item.productName || item.name} added to cart!`);
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to add to cart";
      alert(errorMessage);
    }
  };

  if (isLoading) {
    return (
      <div className="wishlist-container">
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading wishlist...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="wishlist-container">
      <div className="wishlist-header">
        <h2>Your Wishlist</h2>
      </div>

      {wishlist.length === 0 ? (
        <div className="empty-wishlist">
          <p>Your wishlist is empty.</p>
          <p>Start adding some products to see them here!</p>
        </div>
      ) : (
        <div className="wishlist-grid">
          {wishlist.map(item => (
            <div key={item.id} className="wishlist-card" onClick={() => navigate(`/products/${item.productId}`)} style={{ cursor: 'pointer' }}>
              <img 
                src={item.productImageUrl || item.image || 'https://via.placeholder.com/300x200'} 
                alt={item.productName || item.name} 
                className="wishlist-image"
                onError={(e) => {
                  e.target.onerror = null;
                  e.target.src = 'https://via.placeholder.com/300x200?text=Product+Image';
                }}
              />
              <div className="wishlist-details">
                <h3 className="wishlist-name">{item.productName || item.name}</h3>
                <p className="wishlist-price">Rs. {(item.productPrice || item.price)?.toLocaleString()}</p>
                <div className="wishlist-actions" onClick={(e) => e.stopPropagation()}>
                  <button
                    className="wishlist-btn add-to-cart"
                    onClick={() => handleAddToCart(item)}
                  >
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M6 2L3 6V20C3 20.5304 3.21071 21.0391 3.58579 21.4142C3.96086 21.7893 4.46957 22 5 22H19C19.5304 22 20.0391 21.7893 20.4142 21.4142C20.7893 21.0391 21 20.5304 21 20V6L18 2H6Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                      <path d="M16 10C16 11.0609 15.5786 12.0783 14.8284 12.8284C14.0783 13.5786 13.0609 14 12 14C10.9391 14 9.92172 13.5786 9.17157 12.8284C8.42143 12.0783 8 11.0609 8 10" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                    Add to Cart
                  </button>
                  <button
                    className="wishlist-btn remove"
                    onClick={() => handleRemove(item.productId)}
                  >
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                      <path d="M19 7L18.1327 19.1425C18.0579 20.1891 17.187 21 16.1378 21H7.86224C6.81296 21 5.94208 20.1891 5.86732 19.1425L5 7M10 11V17M14 11V17M15 7V4C15 3.44772 14.5523 3 14 3H10C9.44772 3 9 3.44772 9 4V7M4 7H20" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                    Remove
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default Wishlist;
