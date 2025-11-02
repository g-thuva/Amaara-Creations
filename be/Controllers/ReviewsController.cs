using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using be.Data;
using be.DTOs.Review;
using be.Models;
using System.Security.Claims;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(ApplicationDbContext context, ILogger<ReviewsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/products/{productId}/reviews - Get product reviews
        [HttpGet("/api/products/{productId}/reviews")]
        public async Task<ActionResult<ProductReviewsResponse>> GetProductReviews(int productId)
        {
            try
            {
                // Check if product exists
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.ProductId == productId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReviewResponse
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        ProductName = product.Name,
                        UserId = r.UserId,
                        UserName = r.User!.Name,
                        UserEmail = r.User.Email,
                        UserAvatarUrl = r.User.AvatarUrl,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync();

                var totalCount = reviews.Count;
                var averageRating = totalCount > 0 ? reviews.Average(r => r.Rating) : 0.0;
                
                // Calculate rating distribution
                var ratingDistribution1 = reviews.Count(r => r.Rating == 1);
                var ratingDistribution2 = reviews.Count(r => r.Rating == 2);
                var ratingDistribution3 = reviews.Count(r => r.Rating == 3);
                var ratingDistribution4 = reviews.Count(r => r.Rating == 4);
                var ratingDistribution5 = reviews.Count(r => r.Rating == 5);

                var response = new ProductReviewsResponse
                {
                    Reviews = reviews,
                    TotalCount = totalCount,
                    AverageRating = Math.Round(averageRating, 2),
                    RatingDistribution1 = ratingDistribution1,
                    RatingDistribution2 = ratingDistribution2,
                    RatingDistribution3 = ratingDistribution3,
                    RatingDistribution4 = ratingDistribution4,
                    RatingDistribution5 = ratingDistribution5
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product reviews for product {ProductId}", productId);
                return StatusCode(500, new { message = "An error occurred while retrieving reviews" });
            }
        }

        // POST: api/products/{productId}/reviews - Add review (authenticated)
        [HttpPost("/api/products/{productId}/reviews")]
        [Authorize]
        public async Task<ActionResult<ReviewResponse>> CreateReview(int productId, [FromBody] CreateReviewRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Check if product exists and is active
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Check if user already reviewed this product
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);

                if (existingReview != null)
                {
                    return Conflict(new { message = "You have already reviewed this product. Use PUT to update your review." });
                }

                // Create new review
                var review = new Review
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Reload with related data
                await _context.Entry(review)
                    .Reference(r => r.User)
                    .LoadAsync();

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    ProductId = review.ProductId,
                    ProductName = product.Name,
                    UserId = review.UserId,
                    UserName = review.User!.Name,
                    UserEmail = review.User.Email,
                    UserAvatarUrl = review.User.AvatarUrl,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return CreatedAtAction(nameof(GetProductReviews), new { productId = review.ProductId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for product {ProductId}", productId);
                return StatusCode(500, new { message = "An error occurred while creating the review" });
            }
        }

        // PUT: api/reviews/{id} - Update review (owner only)
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ReviewResponse>> UpdateReview(int id, [FromBody] UpdateReviewRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var review = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (review == null)
                {
                    return NotFound(new { message = "Review not found" });
                }

                // Check if user owns the review
                if (review.UserId != userId)
                {
                    return StatusCode(403, new { message = "You can only update your own reviews" });
                }

                // Update review
                review.Rating = request.Rating;
                review.Comment = request.Comment;
                review.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new ReviewResponse
                {
                    Id = review.Id,
                    ProductId = review.ProductId,
                    ProductName = review.Product!.Name,
                    UserId = review.UserId,
                    UserName = review.User!.Name,
                    UserEmail = review.User.Email,
                    UserAvatarUrl = review.User.AvatarUrl,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the review" });
            }
        }

        // DELETE: api/reviews/{id} - Delete review (owner/admin)
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var isAdmin = User.IsInRole("Admin");

                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (review == null)
                {
                    return NotFound(new { message = "Review not found" });
                }

                // Check if user owns the review or is admin
                if (review.UserId != userId && !isAdmin)
                {
                    return StatusCode(403, new { message = "You can only delete your own reviews" });
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the review" });
            }
        }

        // GET: api/admin/reviews - Get all reviews (Admin only)
        [HttpGet("/api/admin/reviews")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<ReviewResponse>>> GetAllReviews(
            [FromQuery] int? productId = null,
            [FromQuery] int? rating = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .AsQueryable();

                // Filter by product
                if (productId.HasValue)
                {
                    query = query.Where(r => r.ProductId == productId.Value);
                }

                // Filter by rating
                if (rating.HasValue)
                {
                    query = query.Where(r => r.Rating == rating.Value);
                }

                // Search by user name, email, or comment
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(r =>
                        r.User!.Name.Contains(search) ||
                        r.User!.Email!.Contains(search) ||
                        (r.Comment != null && r.Comment.Contains(search)));
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReviewResponse
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        ProductName = r.Product!.Name,
                        UserId = r.UserId,
                        UserName = r.User!.Name,
                        UserEmail = r.User.Email,
                        UserAvatarUrl = r.User.AvatarUrl,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Reviews = reviews,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all reviews");
                return StatusCode(500, new { message = "An error occurred while retrieving reviews" });
            }
        }

        // DELETE: api/admin/reviews/{id} - Delete review (Admin only)
        [HttpDelete("/api/admin/reviews/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteReviewAdmin(int id)
        {
            try
            {
                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (review == null)
                {
                    return NotFound(new { message = "Review not found" });
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId} (Admin)", id);
                return StatusCode(500, new { message = "An error occurred while deleting the review" });
            }
        }

        // GET: api/admin/reviews/stats - Get review statistics (Admin only)
        [HttpGet("/api/admin/reviews/stats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ReviewStatsResponse>> GetReviewStats()
        {
            try
            {
                var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var thisYear = new DateTime(DateTime.UtcNow.Year, 1, 1);

                // Total Reviews
                var totalReviews = await _context.Reviews.CountAsync();

                // Average Rating
                var averageRating = await _context.Reviews
                    .Select(r => (double?)r.Rating)
                    .AverageAsync() ?? 0.0;

                // Rating Distribution
                var rating1Count = await _context.Reviews
                    .Where(r => r.Rating == 1)
                    .CountAsync();

                var rating2Count = await _context.Reviews
                    .Where(r => r.Rating == 2)
                    .CountAsync();

                var rating3Count = await _context.Reviews
                    .Where(r => r.Rating == 3)
                    .CountAsync();

                var rating4Count = await _context.Reviews
                    .Where(r => r.Rating == 4)
                    .CountAsync();

                var rating5Count = await _context.Reviews
                    .Where(r => r.Rating == 5)
                    .CountAsync();

                // Reviews This Month
                var reviewsThisMonth = await _context.Reviews
                    .Where(r => r.CreatedAt >= thisMonth)
                    .CountAsync();

                // Reviews This Year
                var reviewsThisYear = await _context.Reviews
                    .Where(r => r.CreatedAt >= thisYear)
                    .CountAsync();

                // Products With Reviews
                var productsWithReviews = await _context.Reviews
                    .Select(r => r.ProductId)
                    .Distinct()
                    .CountAsync();

                var response = new ReviewStatsResponse
                {
                    TotalReviews = totalReviews,
                    AverageRating = Math.Round(averageRating, 2),
                    Rating1Count = rating1Count,
                    Rating2Count = rating2Count,
                    Rating3Count = rating3Count,
                    Rating4Count = rating4Count,
                    Rating5Count = rating5Count,
                    ReviewsThisMonth = reviewsThisMonth,
                    ReviewsThisYear = reviewsThisYear,
                    ProductsWithReviews = productsWithReviews
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving review statistics" });
            }
        }
    }
}

