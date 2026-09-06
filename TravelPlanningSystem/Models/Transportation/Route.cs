using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models.Transportation;

[Table("Routes")]
public class Route
{
    [Key]
    public int RouteId { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Origin")]
    public string Origin { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Destination")]
    public string Destination { get; set; } = string.Empty;

    [Range(1, 5000)]
    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(0.5, 48)]
    [Display(Name = "Estimated Duration (hours)")]
    public double EstimatedDurationHours { get; set; }

    [StringLength(1000)]
    [Display(Name = "Route Description")]
    public string? Description { get; set; }

    [StringLength(500)]
    [Display(Name = "Stops")]
    public string? Stops { get; set; } // Comma-separated list of stops

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Last Updated")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
