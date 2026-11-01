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
                var (status, message) = ex switch
                {
                    KeyNotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                    ArgumentException => (StatusCodes.Status400BadRequest, ex.Message),
                    InvalidOperationException => (StatusCodes.Status409Conflict, ex.Message),
                    _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
                };

                if (status == StatusCodes.Status500InternalServerError)
                {
                    _logger.LogError(ex, "Unhandled exception");
                }

                context.Response.StatusCode = status;
                await context.Response.WriteAsJsonAsync(new { error = message });
            }
        }
    }
}