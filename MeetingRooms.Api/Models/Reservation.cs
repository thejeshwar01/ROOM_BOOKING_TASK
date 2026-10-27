namespace MeetingRooms.Api.Models;

public class Reservation
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public Room? Room { get; set; }

    public int OrganizerId { get; set; }

    public User? Organizer { get; set; }

    public string Title { get; set; } = string.Empty;

    // All times are stored in UTC
    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    // Set when a meeting is ended early; EndTime keeps the originally booked end
    public DateTime? EndedAt { get; set; }

    public int Attendees { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Booked;

    // When the room is actually released
    public DateTime ActualEndTime => EndedAt ?? EndTime;

    public bool IsEndedEarly => EndedAt.HasValue;

    // A booked meeting whose end time has passed counts as completed
    public ReservationStatus GetStatus(DateTime now) =>
        Status == ReservationStatus.Booked && EndTime <= now ? ReservationStatus.Completed : Status;

    public bool IsInProgress(DateTime now) =>
        Status == ReservationStatus.Booked && StartTime <= now && now < EndTime;

    public bool IsUpcoming(DateTime now) =>
        Status == ReservationStatus.Booked && StartTime > now;

    // Booked and not finished yet: blocks the room and counts towards the organizer's active reservations
    public bool IsActive(DateTime now) =>
        Status == ReservationStatus.Booked && EndTime > now;
}
