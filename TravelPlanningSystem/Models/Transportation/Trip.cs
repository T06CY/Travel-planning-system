using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models.Transportation;

[Table("Trips")]
public class Trip
{
    [Key]
    public int TripId { get; set; }

    [Required]
    [Display(Name = "Route")]
    public int RouteId { get; set; }

    [Required]
    [Display(Name = "Vehicle")]
    public int VehicleId { get; set; }

    [Required]
    [Display(Name = "Departure Date & Time")]
    public DateTime DepartureTime { get; set; }

    [Required]
    [Display(Name = "Arrival Date & Time")]
    public DateTime ArrivalTime { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, 999999)]
    [Display(Name = "Base Fare")]
    public decimal BaseFare { get; set; }

    [Range(0, 100)]
    [Column(TypeName = "decimal(5,2)")]
    [Display(Name = "Discount Percentage")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Range(0, 200)]
    [Display(Name = "Available Seats")]
    public int AvailableSeats { get; set; }

    [Range(0, 200)]
    [Display(Name = "Total Seats")]
    public int TotalSeats { get; set; }

    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Scheduled"; // Scheduled, On-time, Delayed, Cancelled, Completed

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Last Updated")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [ForeignKey(nameof(RouteId))]
    public Route? Route { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle? Vehicle { get; set; }

    // Navigation properties
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<TransportationReview> Reviews { get; set; } = new List<TransportationReview>();
}
