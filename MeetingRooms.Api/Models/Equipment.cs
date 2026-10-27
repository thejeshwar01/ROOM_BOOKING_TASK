namespace MeetingRooms.Api.Models;

// A room can have several pieces of equipment, so the values are combinable flags
[Flags]
public enum Equipment
{
    None = 0,
    Projector = 1,
    TvScreen = 2,
    VideoConferencing = 4,
    Whiteboard = 8
}
