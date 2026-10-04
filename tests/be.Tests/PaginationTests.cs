using be.DTOs.Common;

namespace be.Tests
{
    public class PaginationTests
    {
        [Fact]
        public void PaginationQueryClampsUnsafeValues()
        {
            var query = new PaginationQuery
            {
                Page = -5,
                PageSize = 500
            };

            Assert.Equal(1, query.SafePage);
            Assert.Equal(PaginationQuery.MaxPageSize, query.SafePageSize);
            Assert.Equal(0, query.Skip);
        }

        [Fact]
        public void PagedResultCalculatesNavigationFlags()
        {
            var result = PagedResult<string>.Create(new[] { "a", "b" }, page: 2, pageSize: 2, totalItems: 5);

            Assert.Equal(3, result.TotalPages);
            Assert.True(result.HasPreviousPage);
            Assert.True(result.HasNextPage);
        }
    }
}
