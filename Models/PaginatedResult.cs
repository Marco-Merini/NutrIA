using System;
using System.Collections.Generic;

namespace NutriFlow.Models
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        public PaginatedResult()
        {
        }

        public PaginatedResult(List<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }
    }
}
