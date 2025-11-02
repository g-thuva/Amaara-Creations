namespace be.DTOs.Review
{
    public class ProductReviewsResponse
    {
        public List<ReviewResponse> Reviews { get; set; } = new List<ReviewResponse>();
        public int TotalCount { get; set; }
        public double AverageRating { get; set; }
        public int RatingDistribution1 { get; set; } // Count of 1-star reviews
        public int RatingDistribution2 { get; set; } // Count of 2-star reviews
        public int RatingDistribution3 { get; set; } // Count of 3-star reviews
        public int RatingDistribution4 { get; set; } // Count of 4-star reviews
        public int RatingDistribution5 { get; set; } // Count of 5-star reviews
    }
}

