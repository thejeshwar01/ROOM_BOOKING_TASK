namespace MeetingRooms.Api.Dtos;

public record UserDto(
    int Id,
    string Code,
    string FirstName,
    string LastName,
    string FullName,
    int ActiveReservations);
