using System.ComponentModel.DataAnnotations;
using MeetingRooms.Api.Models;

namespace MeetingRooms.Api.Dtos;

public record ReservationDto(
    int Id,
    int RoomId,
    string RoomCode,
    string RoomName,
    int OrganizerId,
    string OrganizerName,
    string Title,
    DateTime StartTime,
    DateTime EndTime,
    DateTime? EndedAt,
    int Attendees,
    ReservationStatus Status,
    bool IsInProgress,
    bool IsEndedEarly,
    bool CanEdit,
    bool CanCancel,
    bool CanEndEarly);

// Body for creating or editing a reservation
public class SaveReservationRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a room.")]
    public int RoomId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose an organizer.")]
    public int OrganizerId { get; set; }

    [Required(ErrorMessage = "Meeting title is required.")]
    [StringLength(100, ErrorMessage = "Meeting title can be at most 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start time is required.")]
    public DateTime? StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public DateTime? EndTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "There must be at least 1 attendee.")]
    public int Attendees { get; set; }
}

public record ReservationFilter(
    int? RoomId,
    int? OrganizerId,
    ReservationStatus? Status,
    DateTime? From,
    DateTime? To);

// Limits shown in the reservation form so users know them before submitting
public record BookingRulesDto(int MaxDurationDays, int BookingHorizonDays);
