using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("HotelRooms")]
public class HotelRoom
{
    [Key] public int HotelRoomId { get; set; }
    [Required, StringLength(120)] public string HotelName { get; set; } = string.Empty;
    [Required, StringLength(120)] public string RoomName { get; set; } = string.Empty;
    [NotMapped]
    [Required, StringLength(30)] public string RoomType { get; set; } = "Master Room";
    [Required, StringLength(80)] public string Destination { get; set; } = string.Empty;
    [Required, StringLength(180)] public string Address { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    [Column(TypeName = "decimal(10,2)"), Range(0.01, 999999)] public decimal PricePerNight { get; set; }
    [Range(1, 20)] public int Capacity { get; set; } = 2;
    [Range(1, 500)] public int TotalRooms { get; set; } = 1;
    [StringLength(500)] public string? Amenities { get; set; }
    [Range(1, 5)] public int StarRating { get; set; } = 3;
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<HotelRoomPhoto> Photos { get; set; } = new List<HotelRoomPhoto>();
    public ICollection<HotelReservation> Reservations { get; set; } = new List<HotelReservation>();
    public ICollection<HotelReview> Reviews { get; set; } = new List<HotelReview>();
}
