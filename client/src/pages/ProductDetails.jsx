import React, { useState } from "react";
import { useParams } from "react-router-dom";
import "./ProductDetails.css";

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

  // Render star rating
  const renderStars = (rating) => {
    return Array(5).fill(0).map((_, i) => (
      <span key={i} className={`star ${i < rating ? 'filled' : ''}`}>
        {i < rating ? '★' : '☆'}
      </span>
    ));
  };

  return (
    <div className="product-details-container">
      <div className="product-header">
        <h1>Product Details</h1>
      </div>

      <div className="product-content">
        <div className="product-gallery">
          <img 
            src={product.image} 
            alt={product.name} 
            className="product-image"
            onError={(e) => {
              e.target.onerror = null;
              e.target.src = 'https://via.placeholder.com/600x400?text=Product+Image';
            }}
          />
        </div>

        <div className="product-info">
          <h2 className="product-title">{product.name}</h2>
          
          <div className="product-price">Rs. {product.price.toLocaleString()}</div>
          
          <p className="product-description">{product.description}</p>
          
          <div className="quantity-control">
            <span className="quantity-label">Quantity:</span>
            <input
              type="number"
              min="1"
              value={qty}
              onChange={(e) => setQty(Math.max(1, Number(e.target.value) || 1))}
              className="quantity-input"
            />
          </div>

          <div className="action-buttons">
            <button 
              className="btn btn-primary"
              onClick={() => {
                // Add to cart functionality here
                alert(`${qty} ${product.name}(s) added to cart!`);
              }}
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M6 2L3 6V20C3 20.5304 3.21071 21.0391 3.58579 21.4142C3.96086 21.7893 4.46957 22 5 22H19C19.5304 22 20.0391 21.7893 20.4142 21.4142C20.7893 21.0391 21 20.5304 21 20V6L18 2H6Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <path d="M16 10C16 11.0609 15.5786 12.0783 14.8284 12.8284C14.0783 13.5786 13.0609 14 12 14C10.9391 14 9.92172 13.5786 9.17157 12.8284C8.42143 12.0783 8 11.0609 8 10" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
              Add to Cart
            </button>
            
            <button 
              className={`btn ${liked ? 'btn-primary' : 'btn-outline'}`}
              onClick={() => setLiked(!liked)}
            >
              {liked ? (
                <>
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 21.35L10.55 20.03C5.4 15.36 2 12.27 2 8.5C2 5.41 4.42 3 7.5 3C9.24 3 10.91 3.81 12 5.08C13.09 3.81 14.76 3 16.5 3C19.58 3 22 5.41 22 8.5C22 12.27 18.6 15.36 13.45 20.03L12 21.35Z" fill="currentColor"/>
                  </svg>
                  Saved
                </>
              ) : (
                <>
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 21.35L10.55 20.03C5.4 15.36 2 12.27 2 8.5C2 5.41 4.42 3 7.5 3C9.24 3 10.91 3.81 12 5.08C13.09 3.81 14.76 3 16.5 3C19.58 3 22 5.41 22 8.5C22 12.27 18.6 15.36 13.45 20.03L12 21.35Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                  Save to Wishlist
                </>
              )}
            </button>
          </div>
        </div>
      </div>

      <div className="reviews-section">
        <h2 className="section-title">Customer Reviews</h2>
        
        <div className="reviews-list">
          {product.reviews.length === 0 ? (
            <p className="no-reviews">No reviews yet. Be the first to review this product!</p>
          ) : (
            product.reviews.map((review, index) => (
              <div key={index} className="review-item">
                <div className="review-header">
                  <span className="reviewer-name">{review.user}</span>
                  <div className="review-rating">
                    {renderStars(review.rating)}
                  </div>
                </div>
                <p className="review-comment">{review.comment}</p>
              </div>
            ))
          )}
        </div>

        <div className="review-form-container">
          <h3 className="section-title">Write a Review</h3>
          <form onSubmit={handleReviewSubmit} className="review-form">
            <div className="form-group">
              <label htmlFor="reviewer-name" className="form-label">Your Name</label>
              <input
                type="text"
                id="reviewer-name"
                className="form-control"
                value={newReview.name}
                onChange={(e) => setNewReview({ ...newReview, name: e.target.value })}
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="review-rating" className="form-label">Rating</label>
              <div className="rating-stars">
                {[1, 2, 3, 4, 5].map((star) => (
                  <span 
                    key={star} 
                    className={`star ${star <= newReview.rating ? 'filled' : ''}`}
                    onClick={() => setNewReview({ ...newReview, rating: star })}
                  >
                    {star <= newReview.rating ? '★' : '☆'}
                  </span>
                ))}
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="review-comment" className="form-label">Your Review</label>
              <textarea
                id="review-comment"
                className="form-control"
                value={newReview.comment}
                onChange={(e) => setNewReview({ ...newReview, comment: e.target.value })}
                required
                minLength="10"
              />
            </div>

            <button type="submit" className="btn btn-primary">
              Submit Review
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default ProductDetails;
