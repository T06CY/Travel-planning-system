using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

public enum FlightBookingStatus
{
    Pending,
    Confirmed,
    CheckedIn,
    Completed,
    Cancelled
}

[Table("FlightBookings")]
public class FlightBooking
{
    [Key]
    public int FlightBookingId { get; set; }

    [Required, StringLength(20)]
    public string BookingReference { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string UserEmail { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string TripType { get; set; } = "One-way";

    [Required, StringLength(120)]
    public string ContactName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string ContactPhone { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    public FlightBookingStatus Status { get; set; } =
        FlightBookingStatus.Confirmed;

    [StringLength(400)]
    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<FlightBookingSegment> Segments { get; set; } =
        new List<FlightBookingSegment>();

    public ICollection<FlightPassenger> Passengers { get; set; } =
        new List<FlightPassenger>();
}
