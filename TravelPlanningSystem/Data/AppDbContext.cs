using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.Models.Transportation;
using TransportationModels = TravelPlanningSystem.Models.Transportation;
using System;

namespace TravelPlanningSystem.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityCategory> ActivityCategories { get; set; }
    public DbSet<ActivitySession> ActivitySessions { get; set; }
    public DbSet<ActivityPhoto> ActivityPhotos { get; set; }
    public DbSet<ActivityBooking> ActivityBookings { get; set; }
    public DbSet<ActivityReview> ActivityReviews { get; set; }
    public DbSet<HotelRoom> HotelRooms { get; set; }
    public DbSet<HotelRoomPhoto> HotelRoomPhotos { get; set; }
    public DbSet<HotelReservation> HotelReservations { get; set; }
    public DbSet<HotelReview> HotelReviews { get; set; }

    // User and staff tables
    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<StaffUser> StaffUsers { get; set; }
    public DbSet<StaffRole> StaffRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Activity relationships
        modelBuilder.Entity<ActivityReview>()
        .HasOne(r => r.ActivityBooking)
        .WithOne(b => b.Review)
        .HasForeignKey<ActivityReview>(r => r.ActivityBookingId)
        .OnDelete(DeleteBehavior.NoAction);

        // Hotel relationships
        modelBuilder.Entity<HotelReview>()
            .HasOne(r => r.HotelReservation)
            .WithOne(b => b.Review)
            .HasForeignKey<HotelReview>(r => r.HotelReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        // Transportation relationships
        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Route)
            .WithMany(r => r.Trips)
            .HasForeignKey(t => t.RouteId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Vehicle)
            .WithMany(v => v.Trips)
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Seat>()
            .HasOne(s => s.Trip)
            .WithMany(t => t.Seats)
            .HasForeignKey(s => s.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TransportationReview>()
            .HasOne(r => r.Trip)
            .WithMany(t => t.Reviews)
            .HasForeignKey(r => r.TripId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TransportationReview>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Set unique constraint on Email addresses
        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<StaffUser>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<Airline>()
            .HasIndex(a => a.AirlineCode)
            .IsUnique();

        modelBuilder.Entity<Airport>()
            .HasIndex(a => a.AirportCode)
            .IsUnique();

        modelBuilder.Entity<Flight>()
            .HasIndex(f => new { f.FlightNumber, f.DepartureTime })
            .IsUnique();

        modelBuilder.Entity<Flight>()
            .HasOne(f => f.Airline)
            .WithMany(a => a.Flights)
            .HasForeignKey(f => f.AirlineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FlightBooking>()
            .HasIndex(b => b.BookingReference)
            .IsUnique();

        modelBuilder.Entity<FlightBookingSegment>()
            .HasIndex(s => new { s.FlightBookingId, s.SegmentOrder })
            .IsUnique();

        modelBuilder.Entity<FlightBookingSegment>()
            .HasOne(s => s.FlightBooking)
            .WithMany(b => b.Segments)
            .HasForeignKey(s => s.FlightBookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FlightBookingSegment>()
            .HasOne(s => s.Flight)
            .WithMany(f => f.BookingSegments)
            .HasForeignKey(s => s.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FlightPassenger>()
            .HasOne(p => p.FlightBooking)
            .WithMany(b => b.Passengers)
            .HasForeignKey(p => p.FlightBookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
