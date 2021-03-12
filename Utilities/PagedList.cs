using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Spice.Utilities {
    public class PagedList<T> : List<T> {
        public int CurrentPage { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;

        public PagedList(IEnumerable<T> list,int pageNum,int pageSize,int total,int totalPages) {
            CurrentPage = pageNum;
            PageSize = pageSize;
            TotalCount = total;
            TotalPages = totalPages;
            AddRange(list);
        }

        public static PagedList<T> Paging(IEnumerable<T> src, int pageNum, int pageSize) {
            var enumerable = src.ToList();
            var count = enumerable.Count();
            var totalPages = (int)Math.Ceiling((double) count / pageSize);
            
            return new PagedList<T>(enumerable.Skip((pageNum - 1) * pageSize).Take(pageSize)
                , pageNum, pageSize, count, totalPages);
        }
    }
}