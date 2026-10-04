namespace be.DTOs.Common
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages { get; set; }

        public bool HasPreviousPage => Page > 1;

        public bool HasNextPage => Page < TotalPages;

        public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
        {
            return new PagedResult<T>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }
    }
}
