using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Errors;
using MeetingRooms.Api.Models;
using MeetingRooms.Api.Options;
using MeetingRooms.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MeetingRooms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ProducesResponseType<ApiError>(StatusCodes.Status400BadRequest)]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    // GET api/reservations?roomId=1&organizerId=2&status=Booked&from=2026-10-01T00:00:00Z&to=2026-11-01T00:00:00Z
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReservationDto>>> GetReservations(
        [FromQuery] int? roomId,
        [FromQuery] int? organizerId,
        [FromQuery] ReservationStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var filter = new ReservationFilter(roomId, organizerId, status, from, to);
        return Ok(await _reservations.GetReservationsAsync(filter, cancellationToken));
    }

    // GET api/reservations/rules - the configured booking limits
    [HttpGet("rules")]
    public ActionResult<BookingRulesDto> GetRules([FromServices] IOptions<BookingRulesOptions> options) =>
        Ok(new BookingRulesDto(options.Value.MaxDurationDays, options.Value.BookingHorizonDays));

    [HttpGet("{id:int}")]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> GetReservation(int id, CancellationToken cancellationToken) =>
        Ok(await _reservations.GetReservationAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> Create(SaveReservationRequest request, CancellationToken cancellationToken)
    {
        var reservation = await _reservations.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetReservation), new { id = reservation.Id }, reservation);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> Update(int id, SaveReservationRequest request, CancellationToken cancellationToken) =>
        Ok(await _reservations.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> Cancel(int id, CancellationToken cancellationToken) =>
        Ok(await _reservations.CancelAsync(id, cancellationToken));

    [HttpPost("{id:int}/end")]
    [ProducesResponseType<ApiError>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiError>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> EndEarly(int id, CancellationToken cancellationToken) =>
        Ok(await _reservations.EndEarlyAsync(id, cancellationToken));
}
