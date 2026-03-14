using Microsoft.AspNetCore.Http;

namespace UserService.API.Models.Services
{
    public sealed class ServiceResult<T>
    {
        public bool Success { get; private init; }
        public int StatusCode { get; private init; }
        public string Message { get; private init; }
        public T Data { get; private init; }

        public static ServiceResult<T> Ok(T data, string message = null)
            => new()
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = message,
                Data = data
            };

        public static ServiceResult<T> Fail(int statusCode, string message)
            => new()
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Data = default
            };
    }
}
