using Microsoft.AspNetCore.Diagnostics;

namespace MeetingRooms.Api.Errors;

// Converts exceptions into consistent ApiError JSON responses
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ApiError error;

        if (exception is ApiException apiException)
        {
            httpContext.Response.StatusCode = apiException.StatusCode;
            error = new ApiError(apiException.Code, apiException.Message, apiException.Rule);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception");
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            error = new ApiError("SERVER_ERROR", "Something went wrong on the server. Please try again.");
        }

        await httpContext.Response.WriteAsJsonAsync(error, cancellationToken);
        return true;
    }
}
