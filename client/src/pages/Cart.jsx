import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { cartApi } from "../services/cartApi";
import "./Cart.css";

const Cart = () => {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [cartItems, setCartItems] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isUpdating, setIsUpdating] = useState(false);

  // Fetch cart items
  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login", { state: { from: "/cart" } });
      return;
    }

    const fetchCart = async () => {
      setIsLoading(true);
      try {
        const cartData = await cartApi.getCart();
        setCartItems(cartData.items || []);
      } catch (err) {
        console.error("Error fetching cart:", err);
        alert("Failed to load cart. Please try again.");
      } finally {
        setIsLoading(false);
      }
    };

    fetchCart();
  }, [isAuthenticated, navigate]);

  const handleQuantityChange = async (itemId, qty) => {
    if (qty < 1) return;
    
    setIsUpdating(true);
    try {
      await cartApi.updateCartItem(itemId, qty);
      const cartData = await cartApi.getCart();
      setCartItems(cartData.items || []);
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to update quantity";
      alert(errorMessage);
    } finally {
      setIsUpdating(false);
    }
  };

  const handleRemove = async (itemId) => {
    try {
      await cartApi.removeFromCart(itemId);
      const cartData = await cartApi.getCart();
      setCartItems(cartData.items || []);
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to remove item";
      alert(errorMessage);
    }
  };

  // Handle quantity increment/decrement
  const updateQuantity = async (itemId, currentQty, change) => {
    const newQty = Math.max(1, currentQty + change);
    await handleQuantityChange(itemId, newQty);
  };

  const handleCheckout = () => {
    if (cartItems.length === 0) {
      alert("Your cart is empty!");
      return;
    }
    navigate("/checkout");
  };

  const total = cartItems.reduce((sum, item) => sum + (item.totalPrice || item.price * item.quantity), 0);

  // Format price with commas
  const formatPrice = (price) => {
    return price.toLocaleString('en-IN');
  };

  if (isLoading) {
    return (
      <div className="cart-container">
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading cart...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="cart-container">
      <div className="cart-header">
        <h2>Your Shopping Cart</h2>
      </div>

      {cartItems.length === 0 ? (
        <div className="empty-cart">
          <p>Your cart is empty.</p>
          <p>Continue shopping to add items to your cart.</p>
        </div>
      ) : (
        <>
          <table className="cart-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>Price</th>
                <th>Quantity</th>
                <th>Subtotal</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {cartItems.map(item => (
                <tr key={item.id}>
                  <td>
                    <div className="cart-item">
                      <img 
                        src={item.productImageUrl || item.image || 'https://via.placeholder.com/100'} 
                        alt={item.productName || item.name} 
                        onError={(e) => {
                          e.target.onerror = null;
                          e.target.src = 'https://via.placeholder.com/100?text=Product+Image';
                        }}
                      />
                      <div className="cart-item-info">
                        <span className="cart-item-name">{item.productName || item.name}</span>
                        <span className="cart-item-price">Rs. {formatPrice(item.price || item.productPrice)}</span>
                      </div>
                    </div>
                  </td>
                  <td>Rs. {formatPrice(item.price || item.productPrice)}</td>
                  <td>
                    <div className="quantity-control">
                      <button 
                        className="quantity-btn"
                        onClick={() => updateQuantity(item.id, item.quantity, -1)}
                        disabled={item.quantity <= 1 || isUpdating}
                      >
                        -
                      </button>
                      <input
                        type="number"
                        className="quantity-input"
                        min="1"
                        value={item.quantity}
                        onChange={(e) => handleQuantityChange(item.id, Math.max(1, Number(e.target.value) || 1))}
                        disabled={isUpdating}
                      />
                      <button 
                        className="quantity-btn"
                        onClick={() => updateQuantity(item.id, item.quantity, 1)}
                        disabled={isUpdating}
                      >
                        +
                      </button>
                    </div>
                  </td>
                  <td>Rs. {formatPrice(item.totalPrice || (item.price || item.productPrice) * item.quantity)}</td>
                  <td>
                    <button 
                      className="remove-btn"
                      onClick={() => handleRemove(item.id)}
                      aria-label={`Remove ${item.productName || item.name} from cart`}
                      disabled={isUpdating}
                    >
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path d="M19 7L18.1327 19.1425C18.0579 20.1891 17.187 21 16.1378 21H7.86224C6.81296 21 5.94208 20.1891 5.86732 19.1425L5 7M10 11V17M14 11V17M15 7V4C15 3.44772 14.5523 3 14 3H10C9.44772 3 9 3.44772 9 4V7M4 7H20" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                      </svg>
                      Remove
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="cart-summary">
            <div className="cart-total">
              Total: Rs. {formatPrice(total)}
            </div>
            <button className="checkout-btn" onClick={handleCheckout}>
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M5 12H19M5 12L9 16M5 12L9 8M19 12L15 16M19 12L15 8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
              Proceed to Checkout
            </button>
          </div>
        </>
      )}
    </div>
  );
};

export default Cart;
