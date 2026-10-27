using MeetingRooms.Api.Models;

namespace MeetingRooms.Api.Data;

// Fills the in-memory database with demo data. Times are relative to "now" so the data always looks current.
// Seeded reservations bypass the booking rules on purpose (e.g. past meetings).
public static class DbSeeder
{
    private static readonly string[] MeetingTitles =
    {
        "Daily Stand-up", "Product Review", "Marketing Sync", "Client Call", "Sprint Planning",
        "Design Review", "Team Retro", "1:1", "Budget Review", "Hiring Interview"
    };

    // How each room is typically used in the generated history
    private record UsageProfile(double BookingChance, double CancelChance, double EndEarlyChance, double MinUsedShare, double MaxUsedShare);

    public static void Seed(MeetingRoomContext context, DateTime now)
    {
        if (context.Rooms.Any())
            return;

        var rooms = CreateRooms();
        var users = CreateUsers();

        context.Rooms.AddRange(rooms);
        context.Users.AddRange(users);
        context.Reservations.AddRange(CreatePastReservations(rooms, users, now));
        context.Reservations.AddRange(CreateCurrentAndUpcomingReservations(rooms, users, now));

        context.SaveChanges();
    }

    private static List<Room> CreateRooms() => new()
    {
        new Room
        {
            Code = "RM-001", Name = "BlueNova", Capacity = 8, Floor = "1st Floor", IsActive = true,
            Equipment = Equipment.Projector | Equipment.TvScreen | Equipment.VideoConferencing | Equipment.Whiteboard,
            Description = "Spacious meeting room with modern equipment, ideal for team meetings and presentations."
        },
        new Room
        {
            Code = "RM-002", Name = "Cyborg", Capacity = 12, Floor = "2nd Floor", IsActive = true,
            Equipment = Equipment.Projector | Equipment.VideoConferencing,
            Description = "Bright room with a large table, great for workshops and client calls."
        },
        new Room
        {
            Code = "RM-003", Name = "Daxi", Capacity = 6, Floor = "1st Floor", IsActive = false,
            Equipment = Equipment.TvScreen | Equipment.Projector | Equipment.Whiteboard,
            Description = "Compact room for small team discussions. Currently under renovation."
        },
        new Room
        {
            Code = "RM-004", Name = "Aurora", Capacity = 20, Floor = "3rd Floor", IsActive = true,
            Equipment = Equipment.TvScreen | Equipment.VideoConferencing | Equipment.Whiteboard,
            Description = "Our largest boardroom, suited for all-hands meetings and executive reviews."
        },
        new Room
        {
            Code = "RM-005", Name = "Phantom", Capacity = 10, Floor = "2nd Floor", IsActive = true,
            Equipment = Equipment.TvScreen | Equipment.Projector,
            Description = "Modern room with a wall-mounted screen, perfect for sprint planning."
        },
        new Room
        {
            Code = "RM-006", Name = "Tiger", Capacity = 4, Floor = "1st Floor", IsActive = true,
            Equipment = Equipment.TvScreen,
            Description = "Quiet focus room for one-on-ones, interviews and quick syncs."
        }
    };

    private static List<User> CreateUsers() => new()
    {
        new User { Code = "U-001", FirstName = "John", LastName = "Smith" },
        new User { Code = "U-002", FirstName = "Emma", LastName = "Johnson" },
        new User { Code = "U-003", FirstName = "Michael", LastName = "Brown" },
        new User { Code = "U-004", FirstName = "Sarah", LastName = "Davis" },
        new User { Code = "U-005", FirstName = "Daniel", LastName = "Wilson" },
        new User { Code = "U-006", FirstName = "Olivia", LastName = "Martinez" },
        new User { Code = "U-007", FirstName = "James", LastName = "Taylor" }
    };

    private static UsageProfile ProfileFor(Room room) => room.Code switch
    {
        // The big boardroom is booked a lot but meetings usually end early
        "RM-004" => new UsageProfile(0.8, 0.15, 0.85, 0.2, 0.5),
        "RM-006" => new UsageProfile(0.5, 0.1, 0.3, 0.5, 0.9),
        _ => new UsageProfile(0.6, 0.08, 0.15, 0.6, 0.9)
    };

    // Four weeks of finished weekday meetings
    private static IEnumerable<Reservation> CreatePastReservations(List<Room> rooms, List<User> users, DateTime now)
    {
        var random = new Random(42);
        var slots = new[] { (StartHour: 9, Hours: 1), (StartHour: 11, Hours: 1), (StartHour: 14, Hours: 2) };
        var activeRooms = rooms.Where(r => r.IsActive).ToList();

        for (var daysAgo = 28; daysAgo >= 1; daysAgo--)
        {
            var day = now.Date.AddDays(-daysAgo);
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            for (var slot = 0; slot < slots.Length; slot++)
            {
                for (var roomIndex = 0; roomIndex < activeRooms.Count; roomIndex++)
                {
                    var room = activeRooms[roomIndex];
                    var profile = ProfileFor(room);
                    if (random.NextDouble() > profile.BookingChance)
                        continue;

                    var start = DateTime.SpecifyKind(day.AddHours(slots[slot].StartHour), DateTimeKind.Utc);
                    var end = start.AddHours(slots[slot].Hours);

                    var reservation = new Reservation
                    {
                        Room = room,
                        // Different rooms in the same slot get different organizers, so nobody is double-booked
                        Organizer = users[(roomIndex + daysAgo + slot) % users.Count],
                        Title = MeetingTitles[random.Next(MeetingTitles.Length)],
                        StartTime = start,
                        EndTime = end,
                        Attendees = random.Next(2, room.Capacity + 1),
                        Status = ReservationStatus.Completed
                    };

                    if (random.NextDouble() < profile.CancelChance)
                    {
                        reservation.Status = ReservationStatus.Cancelled;
                    }
                    else if (random.NextDouble() < profile.EndEarlyChance)
                    {
                        var usedShare = profile.MinUsedShare + random.NextDouble() * (profile.MaxUsedShare - profile.MinUsedShare);
                        var usedMinutes = Math.Max(5, Math.Round((end - start).TotalMinutes * usedShare / 5) * 5);
                        reservation.EndedAt = start.AddMinutes(usedMinutes);
                    }

                    yield return reservation;
                }
            }
        }
    }

    private static IEnumerable<Reservation> CreateCurrentAndUpcomingReservations(List<Room> rooms, List<User> users, DateTime now)
    {
        Room RoomByCode(string code) => rooms.Single(r => r.Code == code);
        DateTime At(int daysFromToday, double hour) =>
            DateTime.SpecifyKind(now.Date.AddDays(daysFromToday).AddHours(hour), DateTimeKind.Utc);

        var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

        Reservation Booked(string roomCode, int userIndex, string title, DateTime start, DateTime end, int attendees) => new()
        {
            Room = RoomByCode(roomCode),
            Organizer = users[userIndex],
            Title = title,
            StartTime = start,
            EndTime = end,
            Attendees = attendees,
            Status = ReservationStatus.Booked
        };

        // In progress right now, so "End early" can be tried straight away
        yield return Booked("RM-001", 0, "Product Review", currentHour, currentHour.AddHours(2), 6);

        yield return Booked("RM-002", 1, "Marketing Sync", At(1, 9), At(1, 10), 8);
        yield return Booked("RM-004", 2, "Client Meeting", At(1, 13), At(1, 14.5), 12);
        yield return Booked("RM-006", 4, "Hiring Interview", At(1, 15), At(1, 16), 3);
        yield return Booked("RM-005", 3, "Sprint Planning", At(2, 9), At(2, 11), 7);
        yield return Booked("RM-005", 6, "Design Review", At(4, 14), At(4, 15), 5);
        yield return Booked("RM-001", 5, "Quarterly Planning", At(7, 10), At(7, 12), 8);

        // Multi-day booking
        yield return Booked("RM-004", 5, "Onboarding Workshop", At(10, 9), At(12, 17), 18);

        var cancelled = Booked("RM-002", 0, "Team Lunch", At(3, 12), At(3, 13), 10);
        cancelled.Status = ReservationStatus.Cancelled;
        yield return cancelled;
    }
}
