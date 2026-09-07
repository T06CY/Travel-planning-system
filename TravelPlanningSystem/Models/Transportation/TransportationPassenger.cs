using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models.Transportation;

[Table("TransportationPassengers")]
public class TransportationPassenger
{
    [Key]
    public int PassengerId { get; set; }

    // 归属的订单
    [Required]
    public int BookingId { get; set; }

    [ForeignKey(nameof(BookingId))]
    public TransportationBooking? Booking { get; set; }

    // 乘客真实身份信息
    [Required, StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    [Display(Name = "IC / Passport Number")]
    public string IdNumber { get; set; } = string.Empty;

    [StringLength(20)]
    public string PassengerType { get; set; } = "Adult"; // Adult, Child, Senior

    // 绑定的座位号与座位实体
    public int? SeatId { get; set; }

    [ForeignKey(nameof(SeatId))]
    public Seat? Seat { get; set; }

    [Required, StringLength(10)]
    public string SeatNumber { get; set; } = string.Empty; // 例如 "1A", "2B"

    // 行李额与附加保障 (Baggage & Add-on Manager)
    [StringLength(50)]
    public string BaggageOption { get; set; } = "Standard (20kg Included)";

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaggagePrice { get; set; } = 0;

    public bool HasTravelInsurance { get; set; } = false;

    [Column(TypeName = "decimal(18,2)")]
    public decimal InsurancePrice { get; set; } = 0;

    [StringLength(255)]
    public string? SpecialRequests { get; set; } // 特殊需求（轮椅、老人协助等）

    // ⭐ 是否已登车检票
    public bool IsBoarded { get; set; } = false;
    public DateTime? BoardedAt { get; set; }
}