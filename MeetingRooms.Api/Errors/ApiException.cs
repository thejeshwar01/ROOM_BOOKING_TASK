namespace MeetingRooms.Api.Errors;

// Thrown by services when a request cannot be completed; turned into an ApiError response by ApiExceptionHandler
public class ApiException : Exception
{
    public ApiException(int statusCode, string code, string message, string? rule = null)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Rule = rule;
    }

    public int StatusCode { get; }

    public string Code { get; }

    // Booking rule that was broken (R1-R7), if any
    public string? Rule { get; }

    public static ApiException NotFound(string code, string message) =>
        new(StatusCodes.Status404NotFound, code, message);

    public static ApiException Conflict(string code, string message) =>
        new(StatusCodes.Status409Conflict, code, message);
}
