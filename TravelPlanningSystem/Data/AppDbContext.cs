using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Models;
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

    // User and staff tables
    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<StaffUser> StaffUsers { get; set; }
    public DbSet<StaffRole> StaffRoles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ActivityReview>()
            .HasOne(r => r.ActivityBooking)
            .WithMany()
            .HasForeignKey(r => r.ActivityBookingId)
            .OnDelete(DeleteBehavior.NoAction);

        // Set unique constraint on Email addresses
        modelBuilder.Entity<ApplicationUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<StaffUser>()
            .HasIndex(s => s.Email)
            .IsUnique();
    }
}