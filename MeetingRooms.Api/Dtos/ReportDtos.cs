namespace MeetingRooms.Api.Dtos;

public record RoomUsageDto(
    int RoomId,
    string RoomCode,
    string RoomName,
    int Capacity,
    bool IsActive,
    int TotalReservations,
    int CompletedReservations,
    int CancelledReservations,
    int EndedEarlyReservations,
    double BookedHours,
    double UsedHours,
    double? UtilizationPercent,
    double CancellationPercent,
    bool IsUnderused);

public record RoomUsageReportDto(
    DateTime From,
    DateTime To,
    int LowUtilizationPercent,
    IReadOnlyList<RoomUsageDto> Rooms);
