import React, { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import { productApi } from "../services/productApi";
import { reviewApi } from "../services/reviewApi";
import { cartApi } from "../services/cartApi";
import { wishlistApi } from "../services/wishlistApi";
import "./ProductDetails.css";

const ProductDetails = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [product, setProduct] = useState(null);
  const [reviews, setReviews] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [qty, setQty] = useState(1);
  const [liked, setLiked] = useState(false);
  const [isAddingToCart, setIsAddingToCart] = useState(false);
  const [newReview, setNewReview] = useState({ comment: "", rating: 5 });
  const [isSubmittingReview, setIsSubmittingReview] = useState(false);

  // Fetch product details and reviews
  useEffect(() => {
    const fetchProduct = async () => {
      setIsLoading(true);
      setError("");
      try {
        const productData = await productApi.getProductById(id);
        setProduct(productData);
        
        // Fetch reviews
        const reviewsData = await reviewApi.getProductReviews(id);
        setReviews(reviewsData.reviews || []);
      } catch (err) {
        console.error("Error fetching product:", err);
        setError("Failed to load product. Please try again.");
      } finally {
        setIsLoading(false);
      }
    };

    if (id) {
      fetchProduct();
    }
  }, [id]);

  // Check if product is in wishlist
  useEffect(() => {
    const checkWishlist = async () => {
      if (!isAuthenticated || !product) return;
      
      try {
        const wishlistData = await wishlistApi.getWishlist();
        const isInWishlist = wishlistData.items?.some(
          item => item.productId === product.id
        );
        setLiked(isInWishlist);
      } catch (err) {
        console.error("Error checking wishlist:", err);
      }
    };

    checkWishlist();
  }, [isAuthenticated, product]);

  const handleAddToCart = async () => {
    if (!isAuthenticated) {
      navigate("/login", { state: { from: `/products/${id}` } });
      return;
    }

    setIsAddingToCart(true);
    try {
      await cartApi.addToCart(product.id, qty);
      alert(`${qty} ${product.name}(s) added to cart!`);
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to add to cart";
      alert(errorMessage);
    } finally {
      setIsAddingToCart(false);
    }
  };

  const handleToggleWishlist = async () => {
    if (!isAuthenticated) {
      navigate("/login", { state: { from: `/products/${id}` } });
      return;
    }

    try {
      if (liked) {
        await wishlistApi.removeFromWishlist(product.id);
        setLiked(false);
      } else {
        await wishlistApi.addToWishlist(product.id);
        setLiked(true);
      }
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to update wishlist";
      alert(errorMessage);
    }
  };

  const handleReviewSubmit = async (e) => {
    e.preventDefault();

    if (!isAuthenticated) {
      navigate("/login", { state: { from: `/products/${id}` } });
      return;
    }

    if (!newReview.comment) {
      alert("Please enter a review comment.");
      return;
    }

    setIsSubmittingReview(true);
    try {
      await reviewApi.createReview(id, {
        rating: newReview.rating,
        comment: newReview.comment,
      });

      // Refresh reviews
      const reviewsData = await reviewApi.getProductReviews(id);
      setReviews(reviewsData.reviews || []);
      setNewReview({ comment: "", rating: 5 });
      alert("Review submitted successfully!");
    } catch (err) {
      const errorMessage = err.response?.data?.message || "Failed to submit review";
      alert(errorMessage);
    } finally {
      setIsSubmittingReview(false);
    }
  };

  if (isLoading) {
    return (
      <div className="product-details-container">
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading product details...</p>
        </div>
      </div>
    );
  }

  if (error || !product) {
    return (
      <div className="product-details-container">
        <div style={{ textAlign: 'center', padding: '3rem', color: 'red' }}>
          <p>{error || "Product not found"}</p>
        </div>
      </div>
    );
  }

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
            src={product.imageUrl || product.image || 'https://via.placeholder.com/600x400?text=Product+Image'} 
            alt={product.name} 
            className="product-image"
            onError={(e) => {
              e.target.onerror = null;
              e.target.src = 'https://via.placeholder.com/600x400?text=Product+Image';
            }}
          />
          {product.stock === 0 && (
            <div style={{ position: 'absolute', top: '10px', right: '10px', background: 'red', color: 'white', padding: '0.5rem 1rem', borderRadius: '4px' }}>
              Out of Stock
            </div>
          )}
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
              onClick={handleAddToCart}
              disabled={isAddingToCart || product.stock === 0}
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M6 2L3 6V20C3 20.5304 3.21071 21.0391 3.58579 21.4142C3.96086 21.7893 4.46957 22 5 22H19C19.5304 22 20.0391 21.7893 20.4142 21.4142C20.7893 21.0391 21 20.5304 21 20V6L18 2H6Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                <path d="M16 10C16 11.0609 15.5786 12.0783 14.8284 12.8284C14.0783 13.5786 13.0609 14 12 14C10.9391 14 9.92172 13.5786 9.17157 12.8284C8.42143 12.0783 8 11.0609 8 10" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
              </svg>
              {isAddingToCart ? "Adding..." : product.stock === 0 ? "Out of Stock" : "Add to Cart"}
            </button>
            
            <button 
              className={`btn ${liked ? 'btn-primary' : 'btn-outline'}`}
              onClick={handleToggleWishlist}
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
          {reviews.length === 0 ? (
            <p className="no-reviews">No reviews yet. Be the first to review this product!</p>
          ) : (
            reviews.map((review) => (
              <div key={review.id} className="review-item">
                <div className="review-header">
                  <span className="reviewer-name">{review.userName || review.userEmail}</span>
                  <div className="review-rating">
                    {renderStars(review.rating)}
                  </div>
                  <span className="review-date" style={{ fontSize: '0.85rem', color: '#666', marginLeft: '1rem' }}>
                    {new Date(review.createdAt).toLocaleDateString()}
                  </span>
                </div>
                <p className="review-comment">{review.comment}</p>
              </div>
            ))
          )}
        </div>

        {isAuthenticated ? (
          <div className="review-form-container">
            <h3 className="section-title">Write a Review</h3>
            <form onSubmit={handleReviewSubmit} className="review-form">
              <div className="form-group">
                <label htmlFor="review-rating" className="form-label">Rating</label>
                <div className="rating-stars">
                  {[1, 2, 3, 4, 5].map((star) => (
                    <span 
                      key={star} 
                      className={`star ${star <= newReview.rating ? 'filled' : ''}`}
                      onClick={() => setNewReview({ ...newReview, rating: star })}
                      style={{ cursor: 'pointer' }}
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
                  placeholder="Share your thoughts about this product..."
                />
              </div>

              <button type="submit" className="btn btn-primary" disabled={isSubmittingReview}>
                {isSubmittingReview ? "Submitting..." : "Submit Review"}
              </button>
            </form>
          </div>
        ) : (
          <div className="review-form-container">
            <p style={{ textAlign: 'center', padding: '2rem' }}>
              Please <button onClick={() => navigate("/login")} style={{ background: 'none', border: 'none', color: 'blue', textDecoration: 'underline', cursor: 'pointer' }}>login</button> to write a review.
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

export default ProductDetails;
