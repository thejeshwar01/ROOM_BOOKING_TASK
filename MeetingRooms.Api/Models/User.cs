namespace MeetingRooms.Api.Models;

public class User
{
    public int Id { get; set; }

    // Human-readable unique identifier, e.g. "U-001"
    public string Code { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";
}
