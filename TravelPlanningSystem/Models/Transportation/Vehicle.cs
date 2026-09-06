using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models.Transportation;

[Table("Vehicles")]
public class Vehicle
{
    [Key]
    public int VehicleId { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "License Plate")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Vehicle Model")]
    public string VehicleModel { get; set; } = string.Empty;

    [Required, StringLength(50)]
    [Display(Name = "Vehicle Type")]
    public string VehicleType { get; set; } = string.Empty; // Bus, Coach, Van, etc.

    [Range(1, 200)]
    [Display(Name = "Seating Capacity")]
    public int SeatingCapacity { get; set; }

    [Range(1900, 2100)]
    [Display(Name = "Year")]
    public int ManufactureYear { get; set; }

    [StringLength(500)]
    [Display(Name = "Amenities")]
    public string? Amenities { get; set; } // Comma-separated: WiFi, AC, Toilet, Power Outlets, etc.

    [StringLength(500)]
    [Display(Name = "Registration Number")]
    public string? RegistrationNumber { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Last Updated")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
