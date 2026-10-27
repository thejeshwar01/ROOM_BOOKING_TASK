using MeetingRooms.Api.Data;
using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Errors;
using MeetingRooms.Api.Mapping;
using MeetingRooms.Api.Models;
using MeetingRooms.Api.Services.Rules;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Services;

public interface IReservationService
{
    Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(ReservationFilter filter, CancellationToken cancellationToken = default);

    Task<ReservationDto> GetReservationAsync(int id, CancellationToken cancellationToken = default);

    Task<ReservationDto> CreateAsync(SaveReservationRequest request, CancellationToken cancellationToken = default);

    Task<ReservationDto> UpdateAsync(int id, SaveReservationRequest request, CancellationToken cancellationToken = default);

    Task<ReservationDto> CancelAsync(int id, CancellationToken cancellationToken = default);

    Task<ReservationDto> EndEarlyAsync(int id, CancellationToken cancellationToken = default);
}

public class ReservationService : IReservationService
{
    private readonly MeetingRoomContext _db;
    private readonly IBookingValidator _validator;
    private readonly TimeProvider _clock;

    public ReservationService(MeetingRoomContext db, IBookingValidator validator, TimeProvider clock)
    {
        _db = db;
        _validator = validator;
        _clock = clock;
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    public async Task<IReadOnlyList<ReservationDto>> GetReservationsAsync(ReservationFilter filter, CancellationToken cancellationToken = default)
    {
        var now = Now;
        var from = filter.From?.ToUtc();
        var to = filter.To?.ToUtc();

        if (from.HasValue && to.HasValue && from >= to)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, ErrorCodes.InvalidDateRange,
                "The \"from\" date must be before the \"to\" date.");
        }

        var query = _db.Reservations
            .AsNoTracking()
            .Include(r => r.Room)
            .Include(r => r.Organizer)
            .AsQueryable();

        if (filter.RoomId.HasValue)
        {
            query = query.Where(r => r.RoomId == filter.RoomId);
        }

        if (filter.OrganizerId.HasValue)
        {
            query = query.Where(r => r.OrganizerId == filter.OrganizerId);
        }

        // Reservations that overlap the date range at least partly
        if (from.HasValue)
        {
            query = query.Where(r => r.EndTime > from);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.StartTime < to);
        }

        // Booked meetings whose end has passed are reported as completed
        query = filter.Status switch
        {
            ReservationStatus.Booked => query.Where(r => r.Status == ReservationStatus.Booked && r.EndTime > now),
            ReservationStatus.Completed => query.Where(r => r.Status == ReservationStatus.Completed ||
                                                           (r.Status == ReservationStatus.Booked && r.EndTime <= now)),
            ReservationStatus.Cancelled => query.Where(r => r.Status == ReservationStatus.Cancelled),
            _ => query
        };

        var reservations = await query
            .OrderByDescending(r => r.StartTime)
            .ToListAsync(cancellationToken);

        return reservations.Select(r => r.ToDto(now)).ToList();
    }

    public async Task<ReservationDto> GetReservationAsync(int id, CancellationToken cancellationToken = default)
    {
        var reservation = await FindReservationAsync(id, cancellationToken);
        return reservation.ToDto(Now);
    }

    public async Task<ReservationDto> CreateAsync(SaveReservationRequest request, CancellationToken cancellationToken = default)
    {
        var candidate = await BuildCandidateAsync(null, request, cancellationToken);
        await _validator.ValidateAsync(candidate, cancellationToken);

        var reservation = new Reservation
        {
            Room = candidate.Room,
            Organizer = candidate.Organizer,
            Title = request.Title.Trim(),
            StartTime = candidate.StartTime,
            EndTime = candidate.EndTime,
            Attendees = candidate.Attendees,
            Status = ReservationStatus.Booked
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(cancellationToken);

        return reservation.ToDto(Now);
    }

    public async Task<ReservationDto> UpdateAsync(int id, SaveReservationRequest request, CancellationToken cancellationToken = default)
    {
        var reservation = await FindReservationAsync(id, cancellationToken);

        if (!reservation.IsUpcoming(Now))
        {
            throw ApiException.Conflict(ErrorCodes.ReservationNotEditable,
                "Only booked reservations that have not started yet can be edited.");
        }

        var candidate = await BuildCandidateAsync(id, request, cancellationToken);
        await _validator.ValidateAsync(candidate, cancellationToken);

        reservation.Room = candidate.Room;
        reservation.Organizer = candidate.Organizer;
        reservation.Title = request.Title.Trim();
        reservation.StartTime = candidate.StartTime;
        reservation.EndTime = candidate.EndTime;
        reservation.Attendees = candidate.Attendees;

        await _db.SaveChangesAsync(cancellationToken);

        return reservation.ToDto(Now);
    }

    public async Task<ReservationDto> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var now = Now;
        var reservation = await FindReservationAsync(id, cancellationToken);

        if (!reservation.IsUpcoming(now))
        {
            var reason = reservation.GetStatus(now) switch
            {
                ReservationStatus.Cancelled => "This reservation is already cancelled.",
                ReservationStatus.Completed => "A completed meeting cannot be cancelled.",
                _ => "This meeting has already started. End it early instead."
            };
            throw ApiException.Conflict(ErrorCodes.CannotCancel, reason);
        }

        reservation.Status = ReservationStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);

        return reservation.ToDto(now);
    }

    public async Task<ReservationDto> EndEarlyAsync(int id, CancellationToken cancellationToken = default)
    {
        var now = Now;
        var reservation = await FindReservationAsync(id, cancellationToken);

        if (!reservation.IsInProgress(now))
        {
            throw ApiException.Conflict(ErrorCodes.CannotEndEarly, "Only a meeting that is in progress can be ended early.");
        }

        // The room becomes free from now on; EndTime keeps the booked end for the usage report
        reservation.EndedAt = now;
        reservation.Status = ReservationStatus.Completed;
        await _db.SaveChangesAsync(cancellationToken);

        return reservation.ToDto(now);
    }

    private async Task<Reservation> FindReservationAsync(int id, CancellationToken cancellationToken) =>
        await _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.Organizer)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
        ?? throw ApiException.NotFound(ErrorCodes.ReservationNotFound, $"Reservation {id} was not found.");

    private async Task<BookingCandidate> BuildCandidateAsync(int? reservationId, SaveReservationRequest request, CancellationToken cancellationToken)
    {
        var room = await _db.Rooms.FindAsync([request.RoomId], cancellationToken)
            ?? throw ApiException.NotFound(ErrorCodes.RoomNotFound, $"Room {request.RoomId} was not found.");

        var organizer = await _db.Users.FindAsync([request.OrganizerId], cancellationToken)
            ?? throw ApiException.NotFound(ErrorCodes.UserNotFound, $"User {request.OrganizerId} was not found.");

        return new BookingCandidate(
            reservationId,
            room,
            organizer,
            request.StartTime!.Value.ToUtc(),
            request.EndTime!.Value.ToUtc(),
            request.Attendees);
    }
}
