import React, { useState } from "react";

const initialWishlist = [
  {
    id: 1,
    name: "Wedding Sticker A",
    price: 250,
    image: "https://via.placeholder.com/150"
  },
  {
    id: 2,
    name: "Car Sticker B",
    price: 300,
    image: "https://via.placeholder.com/150"
  }
];

const Wishlist = () => {
  const [wishlist, setWishlist] = useState(initialWishlist);

  const handleRemove = (id) => {
    setWishlist(wishlist.filter(item => item.id !== id));
  };

  const handleAddToCart = (item) => {
    // Placeholder for cart logic
    alert(`${item.name} added to cart!`);
  };

  return (
    <div className="container" style={{ padding: "2rem 0" }}>
      <h2 style={{ fontSize: "2rem", marginBottom: "1.5rem" }}>Your Wishlist</h2>

      {wishlist.length === 0 ? (
        <p>Your wishlist is empty.</p>
      ) : (
        <div className="product-grid">
          {wishlist.map(item => (
            <div key={item.id} className="product-card">
              <img src={item.image} alt={item.name} />
              <h3>{item.name}</h3>
              <p>Rs. {item.price}</p>
              <div style={{ display: "flex", justifyContent: "center", gap: "0.5rem", marginTop: "1rem" }}>
                <button
                  className="home-btn"
                  onClick={() => handleAddToCart(item)}
                >
                  Add to Cart
                </button>
                <button
                  className="remove-btn"
                  onClick={() => handleRemove(item.id)}
                >
                  Remove
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default Wishlist;
