using TravelPlanningSystem.Models.Transportation;

namespace TravelPlanningSystem.ViewModels;

public class AdminTransportationViewModel
{
    // 1. 运营核心指标 (KPI Stats)
    public int TotalVehicles { get; set; }
    public int TotalRoutes { get; set; }
    public int ActiveTripsCount { get; set; }
    public int TotalTicketsSold { get; set; }
    public decimal TotalRevenue { get; set; }

    // 2. 列表数据
    public List<Vehicle> Vehicles { get; set; } = new();
    public List<Route> Routes { get; set; } = new();
    public List<Trip> Trips { get; set; } = new();
    public List<TransportationBooking> RecentBookings { get; set; } = new();

    // 3. 表单绑定模型 (用于后台快速添加)
    public VehicleFormModel NewVehicle { get; set; } = new();
    public RouteFormModel NewRoute { get; set; } = new();
    public TripFormModel NewTrip { get; set; } = new();
}

public class VehicleFormModel
{
    public string LicensePlate { get; set; } = string.Empty;
    public string VehicleModel { get; set; } = string.Empty;
    public string VehicleType { get; set; } = "Coach"; // Luxury Coach, Express Bus, VIP Van, Electric Bus
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