using MeetingRooms.Api.Errors;
using MeetingRooms.Api.Models;
using MeetingRooms.Api.Options;

namespace MeetingRooms.Api.Tests;

public class BookingRulesTests
{
    private static async Task<ApiException> AssertRejected(Task action, string rule, string code)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(() => action);
        Assert.Equal(rule, exception.Rule);
        Assert.Equal(code, exception.Code);
        return exception;
    }

    [Fact]
    public async Task Valid_reservation_is_booked()
    {
        using var env = new TestEnvironment();

        var result = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 1));

        Assert.Equal(ReservationStatus.Booked, result.Status);
        Assert.Equal(env.LargeRoom.Id, result.RoomId);
        Assert.Single(env.Db.Reservations);
    }

    // R1
    [Fact]
    public async Task Reservation_of_exactly_the_max_duration_is_allowed()
    {
        using var env = new TestEnvironment();

        var result = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 7 * 24));

        Assert.Equal(ReservationStatus.Booked, result.Status);
    }

    [Fact]
    public async Task Reservation_longer_than_the_max_duration_is_rejected()
    {
        using var env = new TestEnvironment();
        var request = env.Request(env.LargeRoom, env.Alice, 24, 7 * 24);
        request.EndTime = request.EndTime!.Value.AddMinutes(1);

        await AssertRejected(env.Reservations.CreateAsync(request), "R1", ErrorCodes.MaxDurationExceeded);
    }

    [Fact]
    public async Task Max_duration_comes_from_configuration()
    {
        using var env = new TestEnvironment(new BookingRulesOptions { MaxDurationDays = 1, BookingHorizonDays = 90 });

        await AssertRejected(env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 25)),
            "R1", ErrorCodes.MaxDurationExceeded);
    }

    // R2
    [Fact]
    public async Task Overlapping_booking_of_the_same_room_is_rejected()
    {
        using var env = new TestEnvironment();
        await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 2));

        await AssertRejected(env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Bob, 25, 2)),
            "R2", ErrorCodes.RoomAlreadyBooked);
    }

    [Fact]
    public async Task Cancelled_reservation_does_not_block_the_room()
    {
        using var env = new TestEnvironment();
        var first = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 2));
        await env.Reservations.CancelAsync(first.Id);

        var second = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Bob, 24, 2));

        Assert.Equal(ReservationStatus.Booked, second.Status);
    }

    [Fact]
    public async Task Back_to_back_bookings_are_allowed()
    {
        using var env = new TestEnvironment();
        await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 1));

        var next = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Bob, 25, 1));

        Assert.Equal(ReservationStatus.Booked, next.Status);
    }

    [Fact]
    public async Task Meeting_ended_early_frees_the_room()
    {
        using var env = new TestEnvironment();
        var running = env.AddReservation(env.LargeRoom, env.Alice, TestEnvironment.Now.AddHours(-1), TestEnvironment.Now.AddHours(3));
        await env.Reservations.EndEarlyAsync(running.Id);

        var next = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Bob, 1, 1));

        Assert.Equal(ReservationStatus.Booked, next.Status);
    }

    // R3
    [Fact]
    public async Task Organizer_cannot_have_overlapping_meetings_in_different_rooms()
    {
        using var env = new TestEnvironment();
        await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 2));

        await AssertRejected(env.Reservations.CreateAsync(env.Request(env.SmallRoom, env.Alice, 25, 2)),
            "R3", ErrorCodes.OrganizerDoubleBooked);
    }

    // R4
    [Fact]
    public async Task Start_in_the_past_is_rejected()
    {
        using var env = new TestEnvironment();

        await AssertRejected(env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, -1, 2)),
            "R4", ErrorCodes.StartNotInFuture);
    }

    [Fact]
    public async Task End_before_start_is_rejected()
    {
        using var env = new TestEnvironment();

        await AssertRejected(env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, -1)),
            "R4", ErrorCodes.InvalidTimeRange);
    }

    // R5
    [Fact]
    public async Task Attendees_over_capacity_are_rejected()
    {
        using var env = new TestEnvironment();

        var error = await AssertRejected(env.Reservations.CreateAsync(env.Request(env.SmallRoom, env.Alice, 24, 1, attendees: 30)),
            "R5", ErrorCodes.CapacityExceeded);
        Assert.Contains("4", error.Message);
    }

    [Fact]
    public async Task Attendees_equal_to_capacity_are_allowed()
    {
        using var env = new TestEnvironment();

        var result = await env.Reservations.CreateAsync(env.Request(env.SmallRoom, env.Alice, 24, 1, attendees: 4));

        Assert.Equal(4, result.Attendees);
    }

    // R6
    [Fact]
    public async Task Start_beyond_the_booking_horizon_is_rejected()
    {
        using var env = new TestEnvironment();

        await AssertRejected(env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 91 * 24, 1)),
            "R6", ErrorCodes.BeyondBookingHorizon);
    }

    [Fact]
    public async Task Start_exactly_at_the_booking_horizon_is_allowed()
    {
        using var env = new TestEnvironment();

        var result = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 90 * 24, 1));

        Assert.Equal(ReservationStatus.Booked, result.Status);
    }

    // R7
    [Fact]
    public async Task Inactive_room_cannot_be_booked()
    {
        using var env = new TestEnvironment();

        await AssertRejected(env.Reservations.CreateAsync(env.Request(env.InactiveRoom, env.Alice, 24, 1)),
            "R7", ErrorCodes.RoomInactive);
    }

    // Edited reservations go through the same rules
    [Fact]
    public async Task Editing_a_reservation_does_not_conflict_with_itself()
    {
        using var env = new TestEnvironment();
        var created = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 2));

        var updated = await env.Reservations.UpdateAsync(created.Id, env.Request(env.LargeRoom, env.Alice, 25, 2));

        Assert.Equal(TestEnvironment.Now.AddHours(25), updated.StartTime);
    }

    [Fact]
    public async Task Editing_a_reservation_is_validated()
    {
        using var env = new TestEnvironment();
        var created = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 2));

        await AssertRejected(env.Reservations.UpdateAsync(created.Id, env.Request(env.SmallRoom, env.Alice, 24, 2, attendees: 8)),
            "R5", ErrorCodes.CapacityExceeded);
    }
}
