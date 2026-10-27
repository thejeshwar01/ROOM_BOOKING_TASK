using MeetingRooms.Api.Data;
using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Errors;
using MeetingRooms.Api.Models;
using MeetingRooms.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MeetingRooms.Api.Services;

public interface IUsageReportService
{
    Task<RoomUsageReportDto> GetRoomUsageAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}

// Compares how long each room was booked with how long it was actually used,
// so the office can spot rooms that are often booked but rarely used
public class UsageReportService : IUsageReportService
{
    private readonly MeetingRoomContext _db;
    private readonly TimeProvider _clock;
    private readonly UsageReportOptions _options;

    public UsageReportService(MeetingRoomContext db, TimeProvider clock, IOptions<UsageReportOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<RoomUsageReportDto> GetRoomUsageAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var periodEnd = to?.ToUtc() ?? now;
        var periodStart = from?.ToUtc() ?? periodEnd.AddDays(-_options.DefaultPeriodDays);

        if (periodStart >= periodEnd)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, ErrorCodes.InvalidDateRange,
                "The \"from\" date must be before the \"to\" date.");
        }

        var rooms = await _db.Rooms.AsNoTracking().OrderBy(r => r.Code).ToListAsync(cancellationToken);

        // Meetings that were scheduled to start within the period
        var reservations = await _db.Reservations
            .AsNoTracking()
            .Where(r => r.StartTime >= periodStart && r.StartTime < periodEnd)
            .ToListAsync(cancellationToken);

        var byRoom = reservations.ToLookup(r => r.RoomId);
        var usage = rooms.Select(room => BuildRoomUsage(room, byRoom[room.Id].ToList(), now)).ToList();

        return new RoomUsageReportDto(periodStart, periodEnd, _options.LowUtilizationPercent, usage);
    }

    private RoomUsageDto BuildRoomUsage(Room room, IReadOnlyList<Reservation> reservations, DateTime now)
    {
        var completed = reservations.Where(r => r.GetStatus(now) == ReservationStatus.Completed).ToList();
        var cancelled = reservations.Count(r => r.Status == ReservationStatus.Cancelled);

        // Booked = time the room was reserved for; used = time until the meeting actually ended
        var bookedHours = completed.Sum(r => (r.EndTime - r.StartTime).TotalHours);
        var usedHours = completed.Sum(r => (r.ActualEndTime - r.StartTime).TotalHours);

        double? utilization = bookedHours > 0 ? Math.Round(usedHours / bookedHours * 100, 1) : null;
        var cancellationPercent = reservations.Count > 0 ? Math.Round(cancelled * 100.0 / reservations.Count, 1) : 0;

        return new RoomUsageDto(
            room.Id,
            room.Code,
            room.Name,
            room.Capacity,
            room.IsActive,
            reservations.Count,
            completed.Count,
            cancelled,
            completed.Count(r => r.IsEndedEarly),
            Math.Round(bookedHours, 1),
            Math.Round(usedHours, 1),
            utilization,
            cancellationPercent,
            IsUnderused: utilization < _options.LowUtilizationPercent);
    }
}
