using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Models;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ActivityReview>()
            .HasOne(r => r.ActivityBooking)
            .WithMany()
            .HasForeignKey(r => r.ActivityBookingId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}