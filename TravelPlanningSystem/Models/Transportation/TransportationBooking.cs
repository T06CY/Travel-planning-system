using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.Models.Transportation;

[Table("TransportationBookings")]
public class TransportationBooking
{
    [Key]
    public int BookingId { get; set; }

    // 唯一电子车票订单号 (例如 "TB-20260907-8F2A")
    [Required, StringLength(50)]
    [Display(Name = "Booking Reference")]
    public string BookingReference { get; set; } = string.Empty;

    // 关联车次
    [Required]
    public int TripId { get; set; }

    [ForeignKey(nameof(TripId))]
    public Trip? Trip { get; set; }

    // ⭐ 准确匹配 ApplicationUser 的 Guid 类型
    [Required]
    public Guid UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    // 联络人信息 (Contact Person)
    [Required, StringLength(100)]
    [Display(Name = "Contact Name")]
    public string ContactName { get; set; } = string.Empty;

    [Required, StringLength(150), EmailAddress]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required, StringLength(30), Phone]
    [Display(Name = "Contact Phone")]
    public string ContactPhone { get; set; } = string.Empty;

    // 费用核算 (Pricing Breakdown)
    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseFareTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BaggageFeeTotal { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    public decimal InsuranceFeeTotal { get; set; } = 0;

    [StringLength(50)]
    public string? PromoCode { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    // 状态与支付方式
    [Required, StringLength(30)]
    public string BookingStatus { get; set; } = "Confirmed"; // Confirmed, Cancelled, Completed

    [Required, StringLength(30)]
    public string PaymentStatus { get; set; } = "Paid"; // Pending, Paid, Refunded

    [StringLength(50)]
    public string PaymentMethod { get; set; } = "Online Banking (FPX)";

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // 一张订单对应的多名乘客名册
    public ICollection<TransportationPassenger> Passengers { get; set; } = new List<TransportationPassenger>();
}