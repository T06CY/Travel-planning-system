using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("FlightPassengers")]
public class FlightPassenger
{
    [Key]
    public int FlightPassengerId { get; set; }

    public int FlightBookingId { get; set; }
    public FlightBooking? FlightBooking { get; set; }

    [Required, StringLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string PassportNumber { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Nationality { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required, StringLength(20)]
    public string PassengerType { get; set; } = "Adult";

    [Required, StringLength(5)]
    public string SeatNumber { get; set; } = string.Empty;

    [StringLength(5)]
    public string? ReturnSeatNumber { get; set; }

    [Required, StringLength(40)]
    public string MealPreference { get; set; } = "Standard Meal";
}
