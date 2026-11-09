using ClinicSystem.Application.Exceptions;
using ClinicSystem.Web.Responses;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Web.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = Map(exception);

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception");
            }
            else if (exception is DbUpdateException)
            {
                _logger.LogWarning(exception, "Database conflict");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message));
        }

        private static (int StatusCode, string Message) Map(Exception exception)
        {
            switch (exception)
            {
                case UnauthorizedAccessException:
                    return (StatusCodes.Status401Unauthorized, exception.Message);

                case ForbiddenException:
                    return (StatusCodes.Status403Forbidden, exception.Message);

                case KeyNotFoundException:
                    return (StatusCodes.Status404NotFound, exception.Message);

                case ArgumentException:
                    return (StatusCodes.Status400BadRequest, exception.Message);

                case InvalidOperationException:
                    return (StatusCodes.Status409Conflict, exception.Message);

                // ჯერ concurrency, რადგან ის DbUpdateException-ის შვილია
                case DbUpdateConcurrencyException:
                    return (StatusCodes.Status409Conflict,
                        "The record was changed by someone else. Please reload and try again.");

                case DbUpdateException dbException:
                    return MapDatabaseException(dbException);

                default:
                    return (StatusCodes.Status500InternalServerError,
                        "Unexpected error occurred while processing your request.");
            }
        }

        private static (int StatusCode, string Message) MapDatabaseException(DbUpdateException exception)
        {
            if (exception.GetBaseException() is SqlException sqlException)
            {
                switch (sqlException.Number)
                {
                    // უნიკალური ინდექსი ან შეზღუდვა: დუბლირებული მნიშვნელობა,
                    // ან დრო/მიმართვა ამავე წამს სხვამ დაიკავა
                    case 2601:
                    case 2627:
                        return (StatusCodes.Status409Conflict,
                            "This value or time slot is already taken (possibly just now by another request). Please try again.");

                    // კავშირი (FK): ჩანაწერი სხვა ჩანაწერებზეა დაკავშირებული
                    case 547:
                        return (StatusCodes.Status409Conflict,
                            "The operation conflicts with related data.");
                }
            }

            return (StatusCodes.Status500InternalServerError,
                "Unexpected error occurred while processing your request.");
        }
    }
}