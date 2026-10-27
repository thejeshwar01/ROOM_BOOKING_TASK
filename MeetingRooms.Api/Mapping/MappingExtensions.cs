using System.Text.Json;
using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Models;

namespace MeetingRooms.Api.Mapping;

// Hand-written mapping from entities to the DTOs returned by the API
public static class MappingExtensions
{
    private static readonly Equipment[] AllEquipment =
        Enum.GetValues<Equipment>().Where(e => e != Equipment.None).ToArray();

    public static RoomDto ToDto(this Room room) => new(
        room.Id,
        room.Code,
        room.Name,
        room.Capacity,
        room.Floor,
        room.Equipment.ToNames(),
        room.IsActive,
        room.Description);

    public static UserDto ToDto(this User user, int activeReservations) => new(
        user.Id,
        user.Code,
        user.FirstName,
        user.LastName,
        user.FullName,
        activeReservations);

    // The reservation must be loaded with its Room and Organizer
    public static ReservationDto ToDto(this Reservation reservation, DateTime now)
    {
        var upcoming = reservation.IsUpcoming(now);
        var inProgress = reservation.IsInProgress(now);

        return new ReservationDto(
            reservation.Id,
            reservation.RoomId,
            reservation.Room!.Code,
            reservation.Room.Name,
            reservation.OrganizerId,
            reservation.Organizer!.FullName,
            reservation.Title,
            reservation.StartTime,
            reservation.EndTime,
            reservation.EndedAt,
            reservation.Attendees,
            reservation.GetStatus(now),
            inProgress,
            reservation.IsEndedEarly,
            CanEdit: upcoming,
            CanCancel: upcoming,
            CanEndEarly: inProgress);
    }

    // Equipment flags as camelCase names, e.g. ["projector", "tvScreen"]
    public static IReadOnlyList<string> ToNames(this Equipment equipment) =>
        AllEquipment
            .Where(e => equipment.HasFlag(e))
            .Select(e => JsonNamingPolicy.CamelCase.ConvertName(e.ToString()))
            .ToList();
}
