using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Models;
using MeetingRooms.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRooms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _rooms;

    public RoomsController(IRoomService rooms)
    {
        _rooms = rooms;
    }

    // GET api/rooms?minCapacity=8&equipment=projector&equipment=whiteboard&floor=1st Floor&activeOnly=true
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetRooms(
        [FromQuery] int? minCapacity,
        [FromQuery] Equipment[]? equipment,
        [FromQuery] string? floor,
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        var filter = new RoomFilter(minCapacity, equipment, floor, activeOnly);
        return Ok(await _rooms.GetRoomsAsync(filter, cancellationToken));
    }

    // GET api/rooms/floors
    [HttpGet("floors")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetFloors(CancellationToken cancellationToken) =>
        Ok(await _rooms.GetFloorsAsync(cancellationToken));
}
