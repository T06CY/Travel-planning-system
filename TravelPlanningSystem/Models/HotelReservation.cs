using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TravelPlanningSystem.Models;
public enum HotelReservationStatus { Confirmed, Completed, Cancelled }
[Table("HotelReservations")]
public class HotelReservation
{
    [Key] public int HotelReservationId { get; set; }
    [Required, StringLength(20)] public string ReservationReference { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int HotelRoomId { get; set; }
    public HotelRoom? HotelRoom { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public TimeSpan CheckInTime { get; set; } = new(15, 0, 0);
    public TimeSpan CheckOutTime { get; set; } = new(12, 0, 0);
    [Range(1, 20)] public int GuestCount { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal PricePerNight { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal TotalAmount { get; set; }
    public DateTime ReservationDate { get; set; } = DateTime.Now;
    [Required, StringLength(100)] public string ContactName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(120)] public string ContactEmail { get; set; } = string.Empty;
    [Required, Phone, StringLength(30)] public string ContactPhone { get; set; } = string.Empty;
    public HotelReservationStatus ReservationStatus { get; set; } = HotelReservationStatus.Confirmed;
    [StringLength(400)] public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public HotelReview? Review { get; set; }
}
