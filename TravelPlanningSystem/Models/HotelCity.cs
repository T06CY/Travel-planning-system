using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("HotelCities")]
public class HotelCity
{
    [Key]
    public int HotelCityId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;
}
