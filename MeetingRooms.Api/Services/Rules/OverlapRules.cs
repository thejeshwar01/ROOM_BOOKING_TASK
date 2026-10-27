using MeetingRooms.Api.Data;
using MeetingRooms.Api.Errors;
using MeetingRooms.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Services.Rules;

public static class OverlapQueries
{
    // Reservations that still hold their time slot and overlap the candidate.
    // Cancelled reservations do not block; meetings ended early only block until they ended.
    public static IQueryable<Reservation> OverlappingWith(this IQueryable<Reservation> reservations, BookingCandidate candidate) =>
        reservations.Where(r =>
            r.Status != ReservationStatus.Cancelled &&
            r.Id != candidate.ReservationId &&
            r.StartTime < candidate.EndTime &&
            candidate.StartTime < (r.EndedAt ?? r.EndTime));
}

// R2: a room cannot have two overlapping active reservations
public class RoomOverlapRule : IBookingRule
{
    private readonly MeetingRoomContext _db;

    public RoomOverlapRule(MeetingRoomContext db)
    {
        _db = db;
    }

    public async Task<RuleViolation?> CheckAsync(BookingCandidate candidate, CancellationToken cancellationToken = default)
    {
        var clash = await _db.Reservations
            .Where(r => r.RoomId == candidate.Room.Id)
            .OverlappingWith(candidate)
            .OrderBy(r => r.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        return clash is null
            ? null
            : new("R2", ErrorCodes.RoomAlreadyBooked,
                $"Room {candidate.Room.Name} is already booked at that time for \"{clash.Title}\".",
                StatusCodes.Status409Conflict);
    }
}

// R3: the same user cannot organize two overlapping meetings, even in different rooms
public class OrganizerOverlapRule : IBookingRule
{
    private readonly MeetingRoomContext _db;

    public OrganizerOverlapRule(MeetingRoomContext db)
    {
        _db = db;
    }

    public async Task<RuleViolation?> CheckAsync(BookingCandidate candidate, CancellationToken cancellationToken = default)
    {
        var clash = await _db.Reservations
            .Include(r => r.Room)
            .Where(r => r.OrganizerId == candidate.Organizer.Id)
            .OverlappingWith(candidate)
            .OrderBy(r => r.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        return clash is null
            ? null
            : new("R3", ErrorCodes.OrganizerDoubleBooked,
                $"{candidate.Organizer.FullName} already organizes \"{clash.Title}\" in room {clash.Room!.Name} at that time.",
                StatusCodes.Status409Conflict);
    }
}
