using MeetingRooms.Api.Errors;

namespace MeetingRooms.Api.Services.Rules;

// R7: inactive rooms cannot be booked
public class ActiveRoomRule : IBookingRule
{
    public Task<RuleViolation?> CheckAsync(BookingCandidate candidate, CancellationToken cancellationToken = default)
    {
        RuleViolation? violation = !candidate.Room.IsActive
            ? new("R7", ErrorCodes.RoomInactive, $"Room {candidate.Room.Name} is inactive and cannot be booked.")
            : null;

        return Task.FromResult(violation);
    }
}

// R5: the number of attendees must not exceed the room capacity
public class CapacityRule : IBookingRule
{
    public Task<RuleViolation?> CheckAsync(BookingCandidate candidate, CancellationToken cancellationToken = default)
    {
        var room = candidate.Room;

        RuleViolation? violation = candidate.Attendees > room.Capacity
            ? new("R5", ErrorCodes.CapacityExceeded,
                $"Room {room.Name} holds up to {room.Capacity} people, but {candidate.Attendees} attendees were requested.")
            : null;

        return Task.FromResult(violation);
    }
}
