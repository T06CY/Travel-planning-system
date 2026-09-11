using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class AdminFlightIndexViewModel
{
    public List<Flight> Flights { get; set; } = new();
    public List<Airport> Airports { get; set; } = new();
}
