using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TravelPlanningSystem.Models;
[Table("HotelRoomPhotos")]
public class HotelRoomPhoto
{
    [Key] public int HotelRoomPhotoId { get; set; }
    public int HotelRoomId { get; set; }
    public HotelRoom? HotelRoom { get; set; }
    [Required, StringLength(350)] public string PhotoUrl { get; set; } = string.Empty;
    [StringLength(120)] public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}
