using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

public enum FlightStatus
{
    Scheduled,
    Boarding,
    Departed,
    Arrived,
    Delayed,
    Cancelled
}

[Table("Flights")]
public class Flight
{
    [Key]
    public int FlightId { get; set; }

    public int AirlineId { get; set; }
    public Airline? Airline { get; set; }

    [Required, StringLength(15)]
    public string FlightNumber { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string From { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string To { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, 999999)]
    public decimal Price { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercent { get; set; }

    [Range(1, 600)]
    public int SeatCapacity { get; set; }

    [Range(0, 600)]
    public int AvailableSeats { get; set; }

    [Required, StringLength(80)]
    public string AircraftModel { get; set; } = string.Empty;

    [StringLength(350)]
    public string? FlightLogoUrl { get; set; }

    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FlightBookingSegment> BookingSegments { get; set; } =
        new List<FlightBookingSegment>();

    [NotMapped]
    public TimeSpan Duration => ArrivalTime - DepartureTime;

    [NotMapped]
    public bool IsRealTimeResult { get; set; }
}
