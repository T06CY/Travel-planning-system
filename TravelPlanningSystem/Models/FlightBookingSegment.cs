using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("FlightBookingSegments")]
public class FlightBookingSegment
{
    [Key]
    public int FlightBookingSegmentId { get; set; }

    public int FlightBookingId { get; set; }
    public FlightBooking? FlightBooking { get; set; }

    public int FlightId { get; set; }
    public Flight? Flight { get; set; }

    [Range(1, 5)]
    public int SegmentOrder { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PricePerPassenger { get; set; }
}
