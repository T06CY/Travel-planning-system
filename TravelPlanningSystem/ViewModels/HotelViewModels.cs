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
    [StringLength(500), Display(Name = "Amenities (comma separated)")] public string? Amenities { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    [Display(Name = "Room photos")] public List<IFormFile> Photos { get; set; } = [];
}

public class HotelSearchViewModel
{
    public string? Search { get; set; }
    public string? Destination { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int? Guests { get; set; }
    public decimal? MaxPrice { get; set; }
    public string Sort { get; set; } = "featured";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
    public int TotalItems { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public IReadOnlyList<HotelRoom> Rooms { get; set; } = [];
    public IReadOnlyList<string> Destinations { get; set; } = [];
}

public class HotelReservationViewModel
{
    public int HotelRoomId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }
    [Required, DataType(DataType.Date)] public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);
    [Required, DataType(DataType.Date)] public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);
    [Range(1, 20), Display(Name = "Guests")] public int GuestCount { get; set; } = 1;
    [Required, StringLength(100), Display(Name = "Contact name")] public string ContactName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(120), Display(Name = "Contact email")] public string ContactEmail { get; set; } = string.Empty;
    [Required, Phone, StringLength(30), Display(Name = "Contact phone")] public string ContactPhone { get; set; } = string.Empty;
}

public class HotelReviewViewModel
{
    public int HotelReservationId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    [Range(1, 5)] public int Rating { get; set; } = 5;
    [Required, StringLength(800)] public string Comment { get; set; } = string.Empty;
}
