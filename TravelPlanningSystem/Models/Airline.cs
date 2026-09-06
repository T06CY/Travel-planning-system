using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("Airlines")]
public class Airline
{
    [Key]
    public int AirlineId { get; set; }

    [Required, StringLength(10)]
    public string AirlineCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string AirlineName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Country { get; set; } = string.Empty;

    [StringLength(350)]
    public string? LogoUrl { get; set; }

    [EmailAddress, StringLength(150)]
    public string? SupportEmail { get; set; }

    [Phone, StringLength(30)]
    public string? ContactNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Flight> Flights { get; set; } = new List<Flight>();
}
