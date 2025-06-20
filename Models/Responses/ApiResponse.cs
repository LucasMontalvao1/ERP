using API.Constants;

namespace API.Models.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> SuccessResult(T data, string message = "")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = string.IsNullOrEmpty(message) ? ApiConstants.SuccessMessages.Retrieved : message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResult(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Data = default
            };
        }
    }

    public class PagedResponse<T> : ApiResponse<IEnumerable<T>>
    {
        public List<T> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        public static PagedResponse<T> Create(List<T> data, int page, int pageSize, int totalRecords, string message = "")
        {
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            return new PagedResponse<T>
            {
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasPreviousPage = page > 1,
                HasNextPage = page < totalPages,
                Message = message
            };
        }
    }
}