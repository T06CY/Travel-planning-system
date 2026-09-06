using System.ComponentModel.DataAnnotations;
using TravelPlanningSystem.Models.Transportation;

namespace TravelPlanningSystem.ViewModels;

public class TransportationBookingViewModel
{
    // 车次基础信息 (只读展示)
    public int TripId { get; set; }
    public Trip? Trip { get; set; }

    // 车内所有座位列表 (用于渲染座位图)
    public List<Seat> Seats { get; set; } = new();

    // 用户选中的座位号集合 (例如 ["1A", "1B"])
    [Required(ErrorMessage = "Please select at least one seat.")]
    public List<string> SelectedSeatNumbers { get; set; } = new();

    // 联络人基本信息 (Contact Information)
    [Required(ErrorMessage = "Contact name is required")]
    [Display(Name = "Contact Name")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact email is required"), EmailAddress]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact phone number is required"), Phone]
    [Display(Name = "Contact Phone Number")]
    public string ContactPhone { get; set; } = string.Empty;

    // 乘客名册明细 (每位乘客对应一个座位)
    public List<PassengerItemViewModel> Passengers { get; set; } = new();

    // 优惠码与结算
    [Display(Name = "Promo Code")]
    public string? PromoCode { get; set; }
    public decimal DiscountAmount { get; set; } = 0;

    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "Online Banking (FPX)";

    public decimal BaseFarePerSeat { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PassengerItemViewModel
{
    public string SeatNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passenger name is required")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "IC/Passport number is required")]
    public string IdNumber { get; set; } = string.Empty;

    public string PassengerType { get; set; } = "Adult"; // Adult, Child, Senior

    public string BaggageOption { get; set; } = "Standard (20kg Included)";
    public decimal BaggagePrice { get; set; } = 0;

    public bool HasInsurance { get; set; } = false;
    public decimal InsurancePrice { get; set; } = 0;

    public string? SpecialRequests { get; set; }
}