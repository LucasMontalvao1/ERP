using API.Constants;

namespace API.Models.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
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
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }

        public static PagedResponse<T> Create(IEnumerable<T> data, int page, int pageSize, int totalRecords)
        {
            return new PagedResponse<T>
            {
                Success = true,
                Message = ApiConstants.SuccessMessages.Retrieved,
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                Timestamp = DateTime.UtcNow
            };
        }
    }
}