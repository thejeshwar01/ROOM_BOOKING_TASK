using MeetingRooms.Api.Data;
using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Mapping;
using MeetingRooms.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(string? search, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    private readonly MeetingRoomContext _db;
    private readonly TimeProvider _clock;

    public UserService(MeetingRoomContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(string? search, CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => (u.Code + " " + u.FirstName + " " + u.LastName).ToLower().Contains(term));
        }

        var users = await query.OrderBy(u => u.Code).ToListAsync(cancellationToken);

        // Active = booked and not finished yet (upcoming or in progress)
        var activeCounts = await _db.Reservations
            .Where(r => r.Status == ReservationStatus.Booked && r.EndTime > now)
            .GroupBy(r => r.OrganizerId)
            .Select(g => new { OrganizerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrganizerId, x => x.Count, cancellationToken);

        return users
            .Select(u => u.ToDto(activeCounts.GetValueOrDefault(u.Id)))
            .ToList();
    }
}
