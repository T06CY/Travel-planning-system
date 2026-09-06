using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models.Transportation;

[Table("Seats")]
public class Seat
{
    [Key]
    public int SeatId { get; set; }

    [Required]
    [Display(Name = "Trip")]
    public int TripId { get; set; }

    [Required, StringLength(10)]
    [Display(Name = "Seat Number")]
    public string SeatNumber { get; set; } = string.Empty; // e.g., "1A", "2B", "3C"

    [StringLength(20)]
    [Display(Name = "Seat Class")]
    public string? SeatClass { get; set; } // Economy, Premium, VIP, etc.

    [StringLength(50)]
    [Display(Name = "Seat Type")]
    public string? SeatType { get; set; } // Window, Aisle, Middle

    [Display(Name = "Is Available")]
    public bool IsAvailable { get; set; } = true;

    [StringLength(50)]
    [Display(Name = "Status")]
    public string Status { get; set; } = "Available"; // Available, Booked, Blocked, Maintenance

    [Display(Name = "Created At")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Last Updated")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    [ForeignKey(nameof(TripId))]
    public Trip? Trip { get; set; }
}
