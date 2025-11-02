import React, { useState, useEffect } from "react";
import { FiStar, FiTrash2, FiRefreshCw } from "react-icons/fi";
import { reviewApi } from "../../services/reviewApi";
import "./AdminStyles.css";

const Reviews = () => {
  const [reviews, setReviews] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [filters, setFilters] = useState({
    rating: "",
    search: "",
    pageNumber: 1,
    pageSize: 20
  });

  useEffect(() => {
    fetchReviews();
  }, [filters.rating, filters.search, filters.pageNumber]);

  const fetchReviews = async () => {
    setIsLoading(true);
    try {
      const response = await reviewApi.getAllReviews({
        rating: filters.rating || undefined,
        search: filters.search || undefined,
        pageNumber: filters.pageNumber,
        pageSize: filters.pageSize
      });
      setReviews(response.reviews || response || []);
    } catch (err) {
      console.error("Error fetching reviews:", err);
      alert("Failed to load reviews");
    } finally {
      setIsLoading(false);
    }
  };

  const handleDelete = async (reviewId) => {
    if (window.confirm("Are you sure you want to delete this review?")) {
      try {
        await reviewApi.deleteReview(reviewId);
        alert("Review deleted successfully!");
        fetchReviews();
      } catch (err) {
        const errorMessage = err.response?.data?.message || "Failed to delete review";
        alert(errorMessage);
      }
    }
  };

  const renderStars = (rating) => {
    return Array(5).fill(0).map((_, i) => (
      <span key={i} style={{ color: i < rating ? '#ffc107' : '#ddd' }}>
        <FiStar fill={i < rating ? '#ffc107' : 'none'} />
      </span>
    ));
  };

  return (
    <div className="admin-page">
      <div className="page-header">
        <div className="header-content">
          <div>
            <h2>Review Management</h2>
            <p>View and manage all product reviews</p>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div style={{ marginBottom: '1.5rem', display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
        <input
          type="text"
          placeholder="Search reviews..."
          value={filters.search}
          onChange={(e) => setFilters({ ...filters, search: e.target.value, pageNumber: 1 })}
          style={{ flex: 1, minWidth: '200px', padding: '0.75rem', borderRadius: '4px', border: '1px solid #ddd' }}
        />
        <select
          value={filters.rating}
          onChange={(e) => setFilters({ ...filters, rating: e.target.value, pageNumber: 1 })}
          style={{ padding: '0.75rem', borderRadius: '4px', border: '1px solid #ddd' }}
        >
          <option value="">All Ratings</option>
          <option value="5">5 Stars</option>
          <option value="4">4 Stars</option>
          <option value="3">3 Stars</option>
          <option value="2">2 Stars</option>
          <option value="1">1 Star</option>
        </select>
        <button onClick={fetchReviews} className="btn btn-outline" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
          <FiRefreshCw /> Refresh
        </button>
      </div>

      {isLoading ? (
        <div style={{ textAlign: 'center', padding: '3rem' }}>
          <p>Loading reviews...</p>
        </div>
      ) : (
        <div className="table-responsive">
          <table className="admin-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>Customer</th>
                <th>Rating</th>
                <th>Comment</th>
                <th>Date</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {reviews.length === 0 ? (
                <tr>
                  <td colSpan="6" style={{ textAlign: 'center', padding: '2rem' }}>
                    No reviews found
                  </td>
                </tr>
              ) : (
                reviews.map((review) => (
                  <tr key={review.id}>
                    <td>
                      <strong>{review.productName || 'N/A'}</strong>
                    </td>
                    <td>
                      <div>
                        <div>{review.userName || review.userEmail || 'Anonymous'}</div>
                        <small style={{ color: '#666' }}>{review.userEmail || ''}</small>
                      </div>
                    </td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.25rem' }}>
                        {renderStars(review.rating)}
                        <span style={{ marginLeft: '0.5rem' }}>({review.rating})</span>
                      </div>
                    </td>
                    <td>
                      <div style={{ maxWidth: '300px', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                        {review.comment || 'No comment'}
                      </div>
                    </td>
                    <td>{new Date(review.createdAt).toLocaleDateString()}</td>
                    <td>
                      <button
                        className="btn-delete"
                        onClick={() => handleDelete(review.id)}
                        title="Delete Review"
                      >
                        <FiTrash2 /> Delete
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export default Reviews;
