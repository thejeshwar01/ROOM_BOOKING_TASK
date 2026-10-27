using MeetingRooms.Api.Models;

namespace MeetingRooms.Api.Services.Rules;

// A new or edited reservation, checked against the booking rules before it is saved.
// ReservationId is set when editing, so the reservation does not conflict with itself.
public record BookingCandidate(
    int? ReservationId,
    Room Room,
    User Organizer,
    DateTime StartTime,
    DateTime EndTime,
    int Attendees);

public record RuleViolation(string Rule, string Code, string Message, int StatusCode = StatusCodes.Status400BadRequest);

// One booking rule (R1-R7). Returns null when the candidate passes.
public interface IBookingRule
{
    Task<RuleViolation?> CheckAsync(BookingCandidate candidate, CancellationToken cancellationToken = default);
}
