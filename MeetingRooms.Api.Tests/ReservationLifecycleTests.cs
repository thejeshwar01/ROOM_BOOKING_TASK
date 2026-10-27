using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Errors;
using MeetingRooms.Api.Models;

namespace MeetingRooms.Api.Tests;

public class ReservationLifecycleTests
{
    private static readonly DateTime Now = TestEnvironment.Now;

    [Fact]
    public async Task Upcoming_reservation_can_be_cancelled()
    {
        using var env = new TestEnvironment();
        var created = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 1));

        var cancelled = await env.Reservations.CancelAsync(created.Id);

        Assert.Equal(ReservationStatus.Cancelled, cancelled.Status);
        Assert.False(cancelled.CanCancel);
    }

    [Fact]
    public async Task Meeting_in_progress_cannot_be_cancelled()
    {
        using var env = new TestEnvironment();
        var running = env.AddReservation(env.LargeRoom, env.Alice, Now.AddHours(-1), Now.AddHours(1));

        var error = await Assert.ThrowsAsync<ApiException>(() => env.Reservations.CancelAsync(running.Id));

        Assert.Equal(ErrorCodes.CannotCancel, error.Code);
    }

    [Fact]
    public async Task Meeting_in_progress_can_be_ended_early()
    {
        using var env = new TestEnvironment();
        var running = env.AddReservation(env.LargeRoom, env.Alice, Now.AddHours(-1), Now.AddHours(1));

        var ended = await env.Reservations.EndEarlyAsync(running.Id);

        Assert.Equal(ReservationStatus.Completed, ended.Status);
        Assert.True(ended.IsEndedEarly);
        Assert.Equal(Now, ended.EndedAt);
        Assert.Equal(Now.AddHours(1), ended.EndTime);
    }

    [Fact]
    public async Task Upcoming_meeting_cannot_be_ended_early()
    {
        using var env = new TestEnvironment();
        var created = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 24, 1));

        var error = await Assert.ThrowsAsync<ApiException>(() => env.Reservations.EndEarlyAsync(created.Id));

        Assert.Equal(ErrorCodes.CannotEndEarly, error.Code);
    }

    [Fact]
    public async Task Booked_meeting_whose_end_has_passed_is_reported_as_completed()
    {
        using var env = new TestEnvironment();
        var created = await env.Reservations.CreateAsync(env.Request(env.LargeRoom, env.Alice, 1, 1));

        env.Clock.UtcNow = Now.AddHours(3);
        var later = await env.Reservations.GetReservationAsync(created.Id);

        Assert.Equal(ReservationStatus.Completed, later.Status);
        Assert.False(later.CanEdit);
    }

    [Fact]
    public async Task Reservations_can_be_filtered_by_status_and_date_range()
    {
        using var env = new TestEnvironment();
        env.AddReservation(env.LargeRoom, env.Alice, Now.AddDays(-2), Now.AddDays(-2).AddHours(1), ReservationStatus.Completed);
        env.AddReservation(env.LargeRoom, env.Alice, Now.AddDays(1), Now.AddDays(1).AddHours(1));
        env.AddReservation(env.SmallRoom, env.Bob, Now.AddDays(1), Now.AddDays(1).AddHours(1), ReservationStatus.Cancelled);

        var booked = await env.Reservations.GetReservationsAsync(new ReservationFilter(null, null, ReservationStatus.Booked, null, null));
        var tomorrow = await env.Reservations.GetReservationsAsync(new ReservationFilter(null, null, null, Now.AddHours(12), Now.AddDays(2)));
        var bobs = await env.Reservations.GetReservationsAsync(new ReservationFilter(null, env.Bob.Id, null, null, null));

        Assert.Single(booked);
        Assert.Equal(2, tomorrow.Count);
        Assert.Single(bobs);
    }

    [Fact]
    public async Task Users_active_reservations_count_only_booked_meetings_that_have_not_finished()
    {
        using var env = new TestEnvironment();
        env.AddReservation(env.LargeRoom, env.Alice, Now.AddDays(1), Now.AddDays(1).AddHours(1));
        env.AddReservation(env.SmallRoom, env.Alice, Now.AddHours(-1), Now.AddHours(1));
        env.AddReservation(env.LargeRoom, env.Alice, Now.AddDays(2), Now.AddDays(2).AddHours(1), ReservationStatus.Cancelled);
        env.AddReservation(env.LargeRoom, env.Alice, Now.AddDays(-2), Now.AddDays(-2).AddHours(1));

        var users = await env.Users.GetUsersAsync(null);

        Assert.Equal(2, users.Single(u => u.Id == env.Alice.Id).ActiveReservations);
        Assert.Equal(0, users.Single(u => u.Id == env.Bob.Id).ActiveReservations);
    }

    [Fact]
    public async Task Usage_report_flags_rooms_that_are_booked_but_rarely_used()
    {
        using var env = new TestEnvironment();
        var start = Now.AddDays(-3);

        // Large room: 4 booked hours, used 1
        env.AddReservation(env.LargeRoom, env.Alice, start, start.AddHours(4), ReservationStatus.Completed, endedAt: start.AddHours(1));
        // Small room: 2 booked hours, used 2
        env.AddReservation(env.SmallRoom, env.Bob, start, start.AddHours(2), ReservationStatus.Completed);

        var report = await env.Reports.GetRoomUsageAsync(null, null);
        var large = report.Rooms.Single(r => r.RoomId == env.LargeRoom.Id);
        var small = report.Rooms.Single(r => r.RoomId == env.SmallRoom.Id);

        Assert.Equal(4, large.BookedHours);
        Assert.Equal(1, large.UsedHours);
        Assert.Equal(25, large.UtilizationPercent);
        Assert.True(large.IsUnderused);
        Assert.Equal(100, small.UtilizationPercent);
        Assert.False(small.IsUnderused);
    }
}
