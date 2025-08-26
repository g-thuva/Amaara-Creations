import React, { useState } from "react";
import { useParams } from "react-router-dom";

const dummyProducts = [
  {
    id: 1,
    name: "Wedding Sticker A",
    price: 250,
    image: "https://via.placeholder.com/400",
    description: "Perfect for wedding envelopes and decorations.",
    reviews: [
      { user: "Thuva", comment: "Nice quality!", rating: 5 },
      { user: "Nishan", comment: "Loved it!", rating: 4 },
    ],
  },
  {
    id: 2,
    name: "Car Sticker B",
    price: 300,
    image: "https://via.placeholder.com/400",
    description: "Custom sticker for car windows.",
    reviews: [
      { user: "Kavi", comment: "Looks great on my car!", rating: 4 },
    ],
  },
];

const ProductDetails = () => {
  const { id } = useParams();
  const product = dummyProducts.find((p) => p.id === parseInt(id));
  const [qty, setQty] = useState(1);
  const [liked, setLiked] = useState(false);
  const [newReview, setNewReview] = useState({ name: "", comment: "", rating: 5 });

  const handleReviewSubmit = (e) => {
  e.preventDefault();

  if (!newReview.name || !newReview.comment) {
    alert("Please fill out all fields.");
    return;
  }

  product.reviews.push({
    user: newReview.name,
    comment: newReview.comment,
    rating: newReview.rating,
  });

  setNewReview({ name: "", comment: "", rating: 5 });
  alert("Review submitted!");
};

  if (!product) return <p>Product not found</p>;

  return (
    <div className="container" style={{ padding: "2rem 0" }}>
      <div style={{ display: "flex", gap: "2rem", flexWrap: "wrap" }}>
        <div style={{ flex: 1 }}>
          <img src={product.image} alt={product.name} style={{ width: "100%", maxWidth: "400px" }} />
        </div>

        <div style={{ flex: 2 }}>
          <h2>{product.name}</h2>
          <p>{product.description}</p>
          <p><strong>Price:</strong> Rs. {product.price}</p>

          <label>Quantity: </label>
          <input
            type="number"
            min="1"
            value={qty}
            onChange={(e) => setQty(Number(e.target.value))}
            style={{ width: "60px", marginLeft: "10px", padding: "4px" }}
          />

          <div style={{ marginTop: "1rem" }}>
            <button className="home-btn" style={{ marginRight: "1rem" }}>
              Add to Cart
            </button>
            <button className="home-btn" onClick={() => setLiked(!liked)}>
              {liked ? "Liked ♥" : "Like ♡"}
            </button>
          </div>
        </div>
      </div>

      <div style={{ marginTop: "2rem" }}>
        <h3>Reviews:</h3>
        {product.reviews.length === 0 ? (
          <p>No reviews yet.</p>
        ) : (
          <ul>
            {product.reviews.map((rev, i) => (
              <li key={i} style={{ marginBottom: "1rem" }}>
                <strong>{rev.user}</strong>: {rev.comment} (Rating: {rev.rating}/5)
              </li>
            ))}
          </ul>
        )}
         <h3>Write a Review</h3>
  <form onSubmit={handleReviewSubmit} style={{ marginTop: "1rem", maxWidth: "500px" }}>
    <label>Name:</label>
    <input
      type="text"
      value={newReview.name}
      onChange={(e) => setNewReview({ ...newReview, name: e.target.value })}
      style={{ width: "100%", padding: "8px", marginBottom: "10px" }}
    />

    <label>Comment:</label>
    <textarea
      value={newReview.comment}
      onChange={(e) => setNewReview({ ...newReview, comment: e.target.value })}
      rows="4"
      style={{ width: "100%", padding: "8px", marginBottom: "10px" }}
    />

    <label>Rating:</label>
    <select
      value={newReview.rating}
      onChange={(e) => setNewReview({ ...newReview, rating: Number(e.target.value) })}
      style={{ width: "100%", padding: "8px", marginBottom: "10px" }}
    >
      <option value={5}>5 - Excellent</option>
      <option value={4}>4 - Good</option>
      <option value={3}>3 - Average</option>
      <option value={2}>2 - Poor</option>
      <option value={1}>1 - Terrible</option>
    </select>

    <button type="submit" className="home-btn">Submit Review</button>
  </form>
      </div>
    </div>
  );
};

export default ProductDetails;
