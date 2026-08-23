using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TravelPlanningSystem.Models;
[Table("HotelReviews")]
public class HotelReview
{
    [Key] public int HotelReviewId { get; set; }
    public int HotelRoomId { get; set; }
    public HotelRoom? HotelRoom { get; set; }
    public int HotelReservationId { get; set; }
    public HotelReservation? HotelReservation { get; set; }
    public int UserId { get; set; }
    [Range(1, 5)] public int Rating { get; set; }
    [Required, StringLength(800)] public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsVisible { get; set; } = true;
}
