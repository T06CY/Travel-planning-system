using TravelPlanningSystem.Models.Transportation;
using TransportRoute = TravelPlanningSystem.Models.Transportation.Route;

namespace TravelPlanningSystem.ViewModels;

public class AdminTransportationViewModel
{
    // 1. KPI & Operational Metrics
    public int TotalVehicles { get; set; }
    public int TotalRoutes { get; set; }
    public int ActiveTripsCount { get; set; }
    public int TotalTicketsSold { get; set; }
    public decimal TotalRevenue { get; set; }

    // 2. Operations & Inventory Collections
    public List<Vehicle> Vehicles { get; set; } = new();

    // Using TransportRoute alias to avoid conflict with ASP.NET Core Routing.Route
    public List<TransportRoute> Routes { get; set; } = new();
    public List<Trip> Trips { get; set; } = new();
    public List<TransportationBooking> RecentBookings { get; set; } = new();

    // Passenger reviews collection for administrative moderation stream
    public List<TransportationReview> Reviews { get; set; } = new();

    // 3. Modal Form Binding Models (For quick creation in admin hub)
    public VehicleFormModel NewVehicle { get; set; } = new();
    public RouteFormModel NewRoute { get; set; } = new();
    public TripFormModel NewTrip { get; set; } = new();
}

public class VehicleFormModel
{
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "Coach";
    public int SeatingCapacity { get; set; } = 30;
    public int ManufactureYear { get; set; } = DateTime.UtcNow.Year;
    public string Amenities { get; set; } = "WiFi, Air Conditioning, USB Charging";
}

public class RouteFormModel
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public double EstimatedDurationHours { get; set; }
    public string? Stops { get; set; }
}

public class TripFormModel
{
    public int RouteId { get; set; }
    public int VehicleId { get; set; }
    public DateTime DepartureTime { get; set; } = DateTime.UtcNow.AddDays(1).Date.AddHours(9);
    public decimal BaseFare { get; set; } = 35.00m;
    public int DiscountPercentage { get; set; } = 0;
}