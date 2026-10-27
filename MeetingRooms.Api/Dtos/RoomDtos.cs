namespace MeetingRooms.Api.Dtos;

public record RoomDto(
    int Id,
    string Code,
    string Name,
    int Capacity,
    string Floor,
    IReadOnlyList<string> Equipment,
    bool IsActive,
    string Description);

public record RoomFilter(int? MinCapacity, IReadOnlyList<Models.Equipment>? Equipment, string? Floor, bool ActiveOnly);
