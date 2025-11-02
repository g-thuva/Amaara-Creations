namespace be.DTOs.Review
{
    public class ReviewStatsResponse
    {
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public int Rating1Count { get; set; }
        public int Rating2Count { get; set; }
        public int Rating3Count { get; set; }
        public int Rating4Count { get; set; }
        public int Rating5Count { get; set; }
        public int ReviewsThisMonth { get; set; }
        public int ReviewsThisYear { get; set; }
        public int ProductsWithReviews { get; set; }
    }
}

