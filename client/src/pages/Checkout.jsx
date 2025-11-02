import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { cartApi } from "../services/cartApi";
import { orderApi } from "../services/orderApi";
import { userApi } from "../services/userApi";
import "./Checkout.css";

const Checkout = () => {
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();
  const [cartItems, setCartItems] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isPlacingOrder, setIsPlacingOrder] = useState(false);
  const [userProfile, setUserProfile] = useState(null);
  const [formData, setFormData] = useState({
    shippingAddress: "",
    shippingCity: "",
    shippingPostalCode: "",
    shippingCountry: "",
    notes: ""
  });

  useEffect(() => {
    if (!isAuthenticated) {
      navigate("/login", { state: { from: "/checkout" } });
      return;
    }

    const fetchData = async () => {
      setIsLoading(true);
      try {
        // Fetch cart
        const cartData = await cartApi.getCart();
        setCartItems(cartData.items || []);

        // Fetch user profile for default shipping info
        const profile = await userApi.getProfile();
        setUserProfile(profile);
        setFormData({
          shippingAddress: profile.address || "",
          shippingCity: "",
          shippingPostalCode: "",
          shippingCountry: "Sri Lanka",
          notes: ""
        });
      } catch (err) {
        console.error("Error fetching checkout data:", err);
        alert("Failed to load checkout data");
      } finally {
        setIsLoading(false);
      }
    };

    fetchData();
  }, [isAuthenticated, navigate]);

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handlePlaceOrder = async (e) => {
    e.preventDefault();

    if (cartItems.length === 0) {
      alert("Your cart is empty!");
      navigate("/cart");
      return;
    }

    setIsPlacingOrder(true);
    try {
      const orderData = {
        shippingAddress: formData.shippingAddress,
        shippingCity: formData.shippingCity,
        shippingPostalCode: formData.shippingPostalCode,
        shippingCountry: formData.shippingCountry || "Sri Lanka",
        notes: formData.notes
      };

      const order = await orderApi.createOrder(orderData);
      
      // Clear cart after successful order
      await cartApi.clearCart();
      
      // Navigate to order confirmation
      navigate(`/orders/${order.id}`, {
        state: { message: "Order placed successfully!" }
      });
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to place order";
      alert(errorMessage);
      console.error("Error placing order:", err);
    } finally {
      setIsPlacingOrder(false);
    }
  };

  const formatPrice = (price) => {
    return price.toLocaleString('en-IN');
  };

  const subtotal = cartItems.reduce((sum, item) => sum + (item.totalPrice || (item.price || item.productPrice) * item.quantity), 0);
  const total = subtotal; // Add shipping, tax, etc. here if needed

  if (isLoading) {
    return (
      <div className="checkout-container">
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading checkout...</p>
        </div>
      </div>
    );
  }

  if (cartItems.length === 0) {
    return (
      <div className="checkout-container">
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Your cart is empty!</p>
          <button onClick={() => navigate("/products")} className="btn btn-primary" style={{ marginTop: '1rem' }}>
            Continue Shopping
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="checkout-container">
      <div className="checkout-header">
        <h1>Checkout</h1>
        <p>Complete your order</p>
      </div>

      <div className="checkout-content">
        {/* Shipping Information Form */}
        <div className="checkout-form-section">
          <h2>Shipping Information</h2>
          <form onSubmit={handlePlaceOrder}>
            <div className="form-group">
              <label htmlFor="shippingAddress">Address *</label>
              <textarea
                id="shippingAddress"
                name="shippingAddress"
                value={formData.shippingAddress}
                onChange={handleInputChange}
                required
                rows="3"
                placeholder="Enter your delivery address"
              />
            </div>

            <div className="form-row">
              <div className="form-group">
                <label htmlFor="shippingCity">City *</label>
                <input
                  type="text"
                  id="shippingCity"
                  name="shippingCity"
                  value={formData.shippingCity}
                  onChange={handleInputChange}
                  required
                  placeholder="Enter city"
                />
              </div>

              <div className="form-group">
                <label htmlFor="shippingPostalCode">Postal Code</label>
                <input
                  type="text"
                  id="shippingPostalCode"
                  name="shippingPostalCode"
                  value={formData.shippingPostalCode}
                  onChange={handleInputChange}
                  placeholder="Enter postal code"
                />
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="shippingCountry">Country *</label>
              <input
                type="text"
                id="shippingCountry"
                name="shippingCountry"
                value={formData.shippingCountry}
                onChange={handleInputChange}
                required
                placeholder="Enter country"
              />
            </div>

            <div className="form-group">
              <label htmlFor="notes">Order Notes (Optional)</label>
              <textarea
                id="notes"
                name="notes"
                value={formData.notes}
                onChange={handleInputChange}
                rows="3"
                placeholder="Any special instructions for your order..."
              />
            </div>

            <div className="checkout-actions">
              <button
                type="button"
                onClick={() => navigate("/cart")}
                className="btn btn-outline"
              >
                Back to Cart
              </button>
              <button
                type="submit"
                className="btn btn-primary"
                disabled={isPlacingOrder}
              >
                {isPlacingOrder ? "Placing Order..." : `Place Order - Rs. ${formatPrice(total)}`}
              </button>
            </div>
          </form>
        </div>

        {/* Order Summary */}
        <div className="checkout-summary">
          <h2>Order Summary</h2>
          <div className="order-items">
            {cartItems.map(item => (
              <div key={item.id} className="order-item">
                <img
                  src={item.productImageUrl || item.image || 'https://via.placeholder.com/60'}
                  alt={item.productName || item.name}
                  className="order-item-image"
                  onError={(e) => {
                    e.target.onerror = null;
                    e.target.src = 'https://via.placeholder.com/60?text=No+Image';
                  }}
                />
                <div className="order-item-details">
                  <h4>{item.productName || item.name}</h4>
                  <p>Quantity: {item.quantity}</p>
                  <p className="order-item-price">Rs. {formatPrice(item.totalPrice || (item.price || item.productPrice) * item.quantity)}</p>
                </div>
              </div>
            ))}
          </div>

          <div className="order-totals">
            <div className="total-row">
              <span>Subtotal</span>
              <span>Rs. {formatPrice(subtotal)}</span>
            </div>
            <div className="total-row">
              <span>Shipping</span>
              <span>Free</span>
            </div>
            <div className="total-row total">
              <span>Total</span>
              <span>Rs. {formatPrice(total)}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Checkout;

