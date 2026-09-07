using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class FlightBookingCreateViewModel
{
    public List<int> FlightIds { get; set; } = new();
    public List<Flight> Flights { get; set; } = new();

    [Required]
    public string TripType { get; set; } = "One-way";

    [Range(1, 9)]
    public int PassengerCount { get; set; } = 1;

    [Required, StringLength(120), RegularExpression(@"^[A-Za-zÀ-ÿ][A-Za-zÀ-ÿ' -]{1,119}$", ErrorMessage = "Use letters, spaces, apostrophes or hyphens only.")]
    public string ContactName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required, StringLength(20), RegularExpression(@"^\+?[0-9][0-9\s-]{7,19}$", ErrorMessage = "Enter a valid phone number.")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required]
    public string PhoneCountry { get; set; } = "MY";

    public List<FlightPassengerInput> Passengers { get; set; } = new();
    public int SeatCapacity { get; set; } = 180;
    public List<string> OccupiedSeats { get; set; } = new();
    public decimal TotalAmount { get; set; }

    // ⭐ 新增支付渠道、行李加购、保险与优惠字段
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "Online Banking (FPX)";

    [Display(Name = "Promo Code")]
    public string? PromoCode { get; set; }
    public decimal DiscountAmount { get; set; } = 0;

    [Display(Name = "Baggage Option")]
    public string BaggageOption { get; set; } = "Cabin Baggage 7kg (Free)";
    public decimal BaggagePrice { get; set; } = 0;

    [Display(Name = "Travel Insurance")]
    public bool HasTravelInsurance { get; set; } = false;
    public decimal AddonFee { get; set; } = 0;
}

public class FlightPassengerInput
{
    [Required, StringLength(80), RegularExpression(@"^[A-Za-zÀ-ÿ][A-Za-zÀ-ÿ' -]{1,79}$", ErrorMessage = "Use letters, spaces, apostrophes or hyphens only.")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80), RegularExpression(@"^[A-Za-zÀ-ÿ][A-Za-zÀ-ÿ' -]{1,79}$", ErrorMessage = "Use letters, spaces, apostrophes or hyphens only.")]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(30), RegularExpression(@"^[A-Za-z0-9]{6,30}$", ErrorMessage = "Use 6–30 letters and numbers only.")]
    public string PassportNumber { get; set; } = string.Empty;

    [Required, StringLength(60), RegularExpression(@"^[A-Za-zÀ-ÿ][A-Za-zÀ-ÿ' -]{1,59}$", ErrorMessage = "Use letters and spaces only.")]
    public string Nationality { get; set; } = "Malaysian";

    [Required, DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Required, StringLength(20)]
    public string PassengerType { get; set; } = "Adult";

    [Required(ErrorMessage = "Please select a seat for every passenger."), StringLength(5)]
    public string SeatNumber { get; set; } = string.Empty;

    [StringLength(5)]
    public string? ReturnSeatNumber { get; set; }

    [Required]
    public string MealPreference { get; set; } = "Standard Meal";
}

public class FlightBookingHistoryViewModel
{
    public List<FlightBooking> Upcoming { get; set; } = new();
    public List<FlightBooking> Past { get; set; } = new();
    public List<FlightBooking> Cancelled { get; set; } = new();
}

public class FlightFormViewModel
{
    public int FlightId { get; set; }

    [Required, StringLength(15), RegularExpression(@"^[A-Za-z0-9-]{2,15}$", ErrorMessage = "Use 2–15 letters, numbers or hyphens only.")]
    public string FlightNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AirlineId { get; set; }

    [Required]
    public string From { get; set; } = string.Empty;

    [Required]
    public string To { get; set; } = string.Empty;

    [Required, DataType(DataType.DateTime)]
    public DateTime DepartureTime { get; set; } = DateTime.Today.AddDays(1).AddHours(8);

    [Required, DataType(DataType.DateTime)]
    public DateTime ArrivalTime { get; set; } = DateTime.Today.AddDays(1).AddHours(10);

    [Range(0.01, 999999)]
    public decimal Price { get; set; }

    [Range(1, 600)]
    public int SeatCapacity { get; set; } = 180;

    [Required, StringLength(80), RegularExpression(@"^[A-Za-z0-9 .'-]{2,80}$", ErrorMessage = "Use letters, numbers, spaces, dots or hyphens only.")]
    public string AircraftModel { get; set; } = "Airbus A320";

    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;
    public bool IsActive { get; set; } = true;
    public IFormFile? AirlineImage { get; set; }
    public IFormFile? FlightImage { get; set; }
    public string? FlightImagePath { get; set; }
    public string? ExistingFlightImageUrl { get; set; }
    public List<string> AvailableFlightImages { get; set; } = new();

    public List<Airline> Airlines { get; set; } = new();
    public List<Airport> Airports { get; set; } = new();
}