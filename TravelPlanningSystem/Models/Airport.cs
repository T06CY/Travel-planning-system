using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("Airports")]
public class Airport
{
    [Key]
    public int AirportId { get; set; }

    [Required, StringLength(5)]
    public string AirportCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string AirportName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Country { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
