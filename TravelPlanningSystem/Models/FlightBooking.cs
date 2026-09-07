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

    // ⭐ 新增支付、行李、保险与优惠字段
    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Online Banking (FPX)";

    [StringLength(30)]
    public string PaymentStatus { get; set; } = "Paid"; // Paid, Pending, Refunded

    [StringLength(50)]
    public string? PromoCode { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Column(TypeName = "decimal(12,2)")]
    public decimal AddonFee { get; set; } = 0;

    [StringLength(50)]
    public string BaggageOption { get; set; } = "Cabin Baggage 7kg (Free)";

    public bool HasTravelInsurance { get; set; } = false;
}
