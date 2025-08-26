import React, { useState } from "react";

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

  return (
    <div className="container" style={{ padding: "2rem 0" }}>
      <h2 style={{ fontSize: "2rem", marginBottom: "1.5rem" }}>Your Cart</h2>

      {cartItems.length === 0 ? (
        <p>Your cart is empty.</p>
      ) : (
        <>
          <table className="cart-table">
            <thead>
              <tr>
                <th>Item</th>
                <th>Price (Rs.)</th>
                <th>Qty</th>
                <th>Subtotal (Rs.)</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {cartItems.map(item => (
                <tr key={item.id}>
                  <td>
                    <div style={{ display: "flex", alignItems: "center", gap: "10px" }}>
                      <img src={item.image} alt={item.name} width="60" />
                      <span>{item.name}</span>
                    </div>
                  </td>
                  <td>{item.price}</td>
                  <td>
                    <input
                      type="number"
                      min="1"
                      value={item.quantity}
                      onChange={(e) => handleQuantityChange(item.id, Number(e.target.value))}
                      style={{ width: "50px", padding: "4px" }}
                    />
                  </td>
                  <td>{item.price * item.quantity}</td>
                  <td>
                    <button className="remove-btn" onClick={() => handleRemove(item.id)}>
                      Remove
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div style={{ marginTop: "1.5rem", textAlign: "right" }}>
            <h3>Total: Rs. {total}</h3>
            <button className="home-btn" style={{ marginTop: "1rem" }}>
              Proceed to Checkout
            </button>
          </div>
        </>
      )}
    </div>
  );
};

export default Cart;
