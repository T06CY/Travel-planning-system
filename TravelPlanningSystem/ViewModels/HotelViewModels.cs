using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class HotelRoomFormViewModel
{
    public int HotelRoomId { get; set; }
    [Required, StringLength(120), Display(Name = "Hotel name")] public string HotelName { get; set; } = string.Empty;
    [Required, StringLength(120), Display(Name = "Room name")] public string RoomName { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Destination { get; set; } = string.Empty;
    [Required, StringLength(180)] public string Address { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Range(0.01, 999999), Display(Name = "Price per night (RM)")] public decimal PricePerNight { get; set; }
    [Range(1, 20)] public int Capacity { get; set; } = 2;
    [Range(1, 500), Display(Name = "Rooms available")] public int TotalRooms { get; set; } = 1;
    [Range(1, 5), Display(Name = "Hotel stars")] public int StarRating { get; set; } = 3;
    [Required, StringLength(30), Display(Name = "Room type")] public string RoomType { get; set; } = "Master Room";
    [StringLength(500), Display(Name = "Amenities (comma separated)")] public string? Amenities { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    [Display(Name = "Room photos")] public List<IFormFile> Photos { get; set; } = new List<IFormFile>();
}

public class HotelSearchViewModel : IValidatableObject
{
    public string? Search { get; set; }
    public string? Destination { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    [DataType(DataType.Time)] public TimeSpan? CheckInTime { get; set; }
    [DataType(DataType.Time)] public TimeSpan? CheckOutTime { get; set; }
    public int? Adults { get; set; }
    public int? Children { get; set; }
    // Number of rooms requested by the user (renamed to avoid conflict with Rooms collection)
    public int? RequestedRooms { get; set; }
    // Backing property for simple forms that bind a single Guests field
    public int? Guests { get; set; }
    public decimal? MaxPrice { get; set; } = 300m;
    public string Currency { get; set; } = "MYR";
    public string Sort { get; set; } = "featured";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
    public int TotalItems { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    // Collection of rooms returned by the search
    public IReadOnlyList<HotelRoom> Rooms { get; set; } = Array.Empty<HotelRoom>();
    public IReadOnlyList<string> Destinations { get; set; } = Array.Empty<string>();
    // Computed DateTime values combining date + time for server-side availability checks
    public DateTime? CheckInDateTime => CheckIn.HasValue ? CheckIn.Value.Date.Add(CheckInTime ?? new TimeSpan(15, 0, 0)) : null;
    public DateTime? CheckOutDateTime => CheckOut.HasValue ? CheckOut.Value.Date.Add(CheckOutTime ?? new TimeSpan(12, 0, 0)) : null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckInDateTime.HasValue && CheckOutDateTime.HasValue)
        {
            if (CheckOutDateTime <= CheckInDateTime)
            {
                yield return new ValidationResult("Check-out must be after check-in.", new[] { nameof(CheckOut), nameof(CheckOutTime) });
            }
        }
    }
}

public class HotelReservationViewModel : IValidatableObject
{
    public int HotelRoomId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Check-in date")] public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);
    [Required, DataType(DataType.Date), Display(Name = "Check-out date")] public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);
    [Required, DataType(DataType.Time), Display(Name = "Check-in time")] public TimeSpan CheckInTime { get; set; } = new TimeSpan(15, 0, 0);
    [Required, DataType(DataType.Time), Display(Name = "Check-out time")] public TimeSpan CheckOutTime { get; set; } = new TimeSpan(12, 0, 0);
    [Range(1, 20), Display(Name = "Guests")] public int GuestCount { get; set; } = 1;
    [Required, StringLength(100), Display(Name = "Contact name")] public string ContactName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(120), Display(Name = "Contact email")] public string ContactEmail { get; set; } = string.Empty;
    [Required, Phone, StringLength(30), Display(Name = "Contact phone")] public string ContactPhone { get; set; } = string.Empty;

    public DateTime CheckInDateTime => CheckInDate.Date.Add(CheckInTime);
    public DateTime CheckOutDateTime => CheckOutDate.Date.Add(CheckOutTime);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckOutDateTime <= CheckInDateTime)
        {
            yield return new ValidationResult("Check-out must be after check-in.", new[] { nameof(CheckOutDate), nameof(CheckOutTime) });
        }
    }
}

public class HotelReviewViewModel
{
    public int HotelReservationId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    [Range(1, 5)] public int Rating { get; set; } = 5;
    [Required, StringLength(800)] public string Comment { get; set; } = string.Empty;
}
