using MeetingRooms.Api.Data;
using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Mapping;
using MeetingRooms.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Services;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetRoomsAsync(RoomFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetFloorsAsync(CancellationToken cancellationToken = default);
}

public class RoomService : IRoomService
{
    private readonly MeetingRoomContext _db;

    public RoomService(MeetingRoomContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoomDto>> GetRoomsAsync(RoomFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _db.Rooms.AsNoTracking();

        if (filter.MinCapacity.HasValue)
        {
            query = query.Where(r => r.Capacity >= filter.MinCapacity.Value);
        }

        // The room must have every requested piece of equipment
        if (filter.Equipment is { Count: > 0 })
        {
            var required = filter.Equipment.Aggregate(Equipment.None, (all, item) => all | item);
            query = query.Where(r => (r.Equipment & required) == required);
        }

        if (!string.IsNullOrWhiteSpace(filter.Floor))
        {
            query = query.Where(r => r.Floor == filter.Floor);
        }

        if (filter.ActiveOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        var rooms = await query.OrderBy(r => r.Code).ToListAsync(cancellationToken);
        return rooms.Select(r => r.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<string>> GetFloorsAsync(CancellationToken cancellationToken = default) =>
        await _db.Rooms
            .Select(r => r.Floor)
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync(cancellationToken);
}
