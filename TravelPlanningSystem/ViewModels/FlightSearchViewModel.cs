using System.ComponentModel.DataAnnotations;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class FlightSearchViewModel
{
    public string TripType { get; set; } = "Return";
    public string? From { get; set; }
    public string? To { get; set; }
    public string? FromAirportCode { get; set; }
    public string? ToAirportCode { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DepartureDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ReturnDate { get; set; }

    [Range(1, 9)]
    public int Passengers { get; set; } = 1;

    public string SortBy { get; set; } = "departure";
    public bool SearchPerformed { get; set; }

    public List<string> SegmentFrom { get; set; } = new();
    public List<string> SegmentTo { get; set; } = new();
    public List<string> SegmentFromAirportCode { get; set; } = new();
    public List<string> SegmentToAirportCode { get; set; } = new();
    public List<DateTime?> SegmentDate { get; set; } = new();

    public List<Airport> Airports { get; set; } = new();
    public List<FlightSearchSection> Sections { get; set; } = new();
    public List<FlightDatePriceOption> DatePriceOptions { get; set; } = new();
}

public class FlightDatePriceOption
{
    public DateTime Date { get; set; }
    public decimal? LowestPrice { get; set; }
    public int FlightCount { get; set; }
}

public class FlightSearchSection
{
    public int SegmentNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<Flight> Flights { get; set; } = new();
    public bool UsesLiveData { get; set; }
    public string DataSourceMessage { get; set; } = string.Empty;
    public DateTime? LiveCheckedAtUtc { get; set; }
}
