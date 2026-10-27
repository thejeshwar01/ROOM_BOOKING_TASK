namespace MeetingRooms.Api.Models;

public class Room
{
    public int Id { get; set; }

    // Human-readable unique identifier, e.g. "RM-001"
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public string Floor { get; set; } = string.Empty;

    public Equipment Equipment { get; set; }

    // Inactive rooms (e.g. under renovation) cannot be booked
    public bool IsActive { get; set; } = true;

    public string Description { get; set; } = string.Empty;
}
