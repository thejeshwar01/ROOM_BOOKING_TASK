using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MeetingRooms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IUsageReportService _reports;

    public ReportsController(IUsageReportService reports)
    {
        _reports = reports;
    }

    // GET api/reports/room-usage?from=2026-09-25T00:00:00Z&to=2026-10-25T00:00:00Z
    [HttpGet("room-usage")]
    public async Task<ActionResult<RoomUsageReportDto>> GetRoomUsage(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken) =>
        Ok(await _reports.GetRoomUsageAsync(from, to, cancellationToken));
}
