import React, { useState } from "react";
import "./Cart.css";

const initialCart = [
  {
    id: 1,
    name: "Wedding Sticker A",
    price: 250,
    quantity: 2,
    image: "https://via.placeholder.com/100"
  },
  {
    id: 2,
    name: "Car Sticker B",
    price: 300,
    quantity: 1,
    image: "https://via.placeholder.com/100"
  }
];

const Cart = () => {
  const [cartItems, setCartItems] = useState(initialCart);

  const handleQuantityChange = (id, qty) => {
    const updated = cartItems.map(item =>
      item.id === id ? { ...item, quantity: qty } : item
    );
    setCartItems(updated);
  };

  const handleRemove = (id) => {
    setCartItems(cartItems.filter(item => item.id !== id));
  };

  const total = cartItems.reduce((sum, item) => sum + item.price * item.quantity, 0);

  // Handle quantity increment/decrement
  const updateQuantity = (id, change) => {
    setCartItems(cartItems.map(item => {
      if (item.id === id) {
        const newQty = Math.max(1, item.quantity + change);
        return { ...item, quantity: newQty };
      }
      return item;
    }));
  };

  // Format price with commas
  const formatPrice = (price) => {
    return price.toLocaleString('en-IN');
  };

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
                        src={item.image} 
                        alt={item.name} 
                        onError={(e) => {
                          e.target.onerror = null;
                          e.target.src = 'https://via.placeholder.com/100?text=Product+Image';
                        }}
                      />
                      <div className="cart-item-info">
                        <span className="cart-item-name">{item.name}</span>
                        <span className="cart-item-price">Rs. {formatPrice(item.price)}</span>
                      </div>
                    </div>
                  </td>
                  <td>Rs. {formatPrice(item.price)}</td>
                  <td>
                    <div className="quantity-control">
                      <button 
                        className="quantity-btn"
                        onClick={() => updateQuantity(item.id, -1)}
                        disabled={item.quantity <= 1}
                      >
                        -
                      </button>
                      <input
                        type="number"
                        className="quantity-input"
                        min="1"
                        value={item.quantity}
                        onChange={(e) => handleQuantityChange(item.id, Math.max(1, Number(e.target.value) || 1))}
                      />
                      <button 
                        className="quantity-btn"
                        onClick={() => updateQuantity(item.id, 1)}
                      >
                        +
                      </button>
                    </div>
                  </td>
                  <td>Rs. {formatPrice(item.price * item.quantity)}</td>
                  <td>
                    <button 
                      className="remove-btn"
                      onClick={() => handleRemove(item.id)}
                      aria-label={`Remove ${item.name} from cart`}
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
            <button className="checkout-btn">
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
