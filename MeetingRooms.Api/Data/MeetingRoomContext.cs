using Microsoft.EntityFrameworkCore;
using MeetingRooms.Api.Models;

namespace MeetingRooms.Api.Data;

public class MeetingRoomContext : DbContext
{
    public MeetingRoomContext(DbContextOptions<MeetingRoomContext> options)
        : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>().HasIndex(r => r.Code).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Code).IsUnique();
        modelBuilder.Entity<User>().Ignore(u => u.FullName);

        modelBuilder.Entity<Reservation>(reservation =>
        {
            reservation.HasOne(r => r.Room).WithMany().HasForeignKey(r => r.RoomId);
            reservation.HasOne(r => r.Organizer).WithMany().HasForeignKey(r => r.OrganizerId);
            reservation.Ignore(r => r.ActualEndTime);
            reservation.Ignore(r => r.IsEndedEarly);
        });
    }
}
