using MeetingRooms.Api.Data;
using MeetingRooms.Api.Dtos;
using MeetingRooms.Api.Models;
using MeetingRooms.Api.Options;
using MeetingRooms.Api.Services;
using MeetingRooms.Api.Services.Rules;
using Microsoft.EntityFrameworkCore;

namespace MeetingRooms.Api.Tests;

// Clock that tests can set and move
public class FakeClock : TimeProvider
{
    public FakeClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; set; }

    public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
}

// A fresh in-memory database with a few rooms and users, plus the services under test
public sealed class TestEnvironment : IDisposable
{
    public static readonly DateTime Now = new(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc);

    public TestEnvironment(BookingRulesOptions? bookingRules = null, UsageReportOptions? reportOptions = null)
    {
        var dbOptions = new DbContextOptionsBuilder<MeetingRoomContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Db = new MeetingRoomContext(dbOptions);
        Clock = new FakeClock(Now);

        var rulesOptions = Microsoft.Extensions.Options.Options.Create(bookingRules ?? new BookingRulesOptions());
        var usageOptions = Microsoft.Extensions.Options.Options.Create(reportOptions ?? new UsageReportOptions());

        // Same rules, same order as Program.cs
        var rules = new IBookingRule[]
        {
            new ValidTimeRangeRule(),
            new StartInFutureRule(Clock),
            new BookingHorizonRule(Clock, rulesOptions),
            new MaxDurationRule(rulesOptions),
            new ActiveRoomRule(),
            new CapacityRule(),
            new RoomOverlapRule(Db),
            new OrganizerOverlapRule(Db)
        };

        Reservations = new ReservationService(Db, new BookingValidator(rules), Clock);
        Users = new UserService(Db, Clock);
        Reports = new UsageReportService(Db, Clock, usageOptions);

        LargeRoom = new Room { Code = "RM-1", Name = "Large", Capacity = 10, Floor = "1st Floor", IsActive = true };
        SmallRoom = new Room { Code = "RM-2", Name = "Small", Capacity = 4, Floor = "1st Floor", IsActive = true };
        InactiveRoom = new Room { Code = "RM-3", Name = "Closed", Capacity = 10, Floor = "2nd Floor", IsActive = false };
        Alice = new User { Code = "U-1", FirstName = "Alice", LastName = "Adams" };
        Bob = new User { Code = "U-2", FirstName = "Bob", LastName = "Brown" };

        Db.Rooms.AddRange(LargeRoom, SmallRoom, InactiveRoom);
        Db.Users.AddRange(Alice, Bob);
        Db.SaveChanges();
    }

    public MeetingRoomContext Db { get; }

    public FakeClock Clock { get; }

    public ReservationService Reservations { get; }

    public UserService Users { get; }

    public UsageReportService Reports { get; }

    public Room LargeRoom { get; }

    public Room SmallRoom { get; }

    public Room InactiveRoom { get; }

    public User Alice { get; }

    public User Bob { get; }

    // A request starting the given number of hours from now
    public SaveReservationRequest Request(Room room, User organizer, double startInHours, double durationHours, int attendees = 2) => new()
    {
        RoomId = room.Id,
        OrganizerId = organizer.Id,
        Title = "Meeting",
        StartTime = Now.AddHours(startInHours),
        EndTime = Now.AddHours(startInHours + durationHours),
        Attendees = attendees
    };

    // Adds a reservation directly, bypassing the rules (e.g. for past meetings)
    public Reservation AddReservation(Room room, User organizer, DateTime start, DateTime end,
        ReservationStatus status = ReservationStatus.Booked, DateTime? endedAt = null)
    {
        var reservation = new Reservation
        {
            RoomId = room.Id,
            OrganizerId = organizer.Id,
            Title = "Existing",
            StartTime = start,
            EndTime = end,
            EndedAt = endedAt,
            Attendees = 2,
            Status = status
        };
        Db.Reservations.Add(reservation);
        Db.SaveChanges();
        return reservation;
    }

    public void Dispose() => Db.Dispose();
}
