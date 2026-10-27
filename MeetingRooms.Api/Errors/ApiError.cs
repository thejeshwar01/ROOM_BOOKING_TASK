namespace MeetingRooms.Api.Errors;

// Body of every error response: a machine-readable code and a message the front end shows to the user
public record ApiError(string Code, string Message, string? Rule = null);
