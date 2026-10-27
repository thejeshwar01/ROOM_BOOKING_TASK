namespace MeetingRooms.Api.Services;

public static class DateTimeExtensions
{
    // Times without a zone are treated as UTC; local times are converted to UTC
    public static DateTime ToUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
