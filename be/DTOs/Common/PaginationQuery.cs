using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Common
{
    public class PaginationQuery
    {
        public const int MaxPageSize = 100;

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, MaxPageSize)]
        public int PageSize { get; set; } = 20;

        public int SafePage => Math.Max(1, Page);

        public int SafePageSize => Math.Clamp(PageSize, 1, MaxPageSize);

        public int Skip => (SafePage - 1) * SafePageSize;
    }
}
