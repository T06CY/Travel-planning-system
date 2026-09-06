using System.ComponentModel.DataAnnotations;
using TravelPlanningSystem.Models.Transportation;

namespace TravelPlanningSystem.ViewModels;

public class TransportationSearchViewModel : IValidatableObject
{
    // Search Parameters
    [Display(Name = "From (Origin)")]
    public string? Origin { get; set; }

    [Display(Name = "To (Destination)")]
    public string? Destination { get; set; }

    [Display(Name = "Departure Date")]
    public DateTime? DepartureDate { get; set; }

    [Display(Name = "Return Date")]
    public DateTime? ReturnDate { get; set; }

    [Display(Name = "Search Text")]
    public string? Search { get; set; }

    // Filtering Parameters
    [Range(0, 999999, ErrorMessage = "Minimum price cannot be less than 0.")]
    [Display(Name = "Minimum Price")]
    public decimal? MinPrice { get; set; }

    [Range(0, 999999, ErrorMessage = "Maximum price cannot be less than 0.")]
    [Display(Name = "Maximum Price")]
    public decimal? MaxPrice { get; set; }

    [Display(Name = "Vehicle Type")]
    public string? VehicleType { get; set; }

    [Range(0, 5)]
    [Display(Name = "Minimum Rating")]
    public int? MinimumRating { get; set; }

    [Display(Name = "Departure Time From")]
    public TimeOnly? DepartureTimeFrom { get; set; }

    [Display(Name = "Departure Time To")]
    public TimeOnly? DepartureTimeTo { get; set; }

    [Display(Name = "Only Available Seats")]
    public bool OnlyAvailableSeats { get; set; } = true;

    // Sorting Parameters
    [Display(Name = "Sort By")]
    public string SortBy { get; set; } = "departure"; // departure, price-low, price-high, rating, duration

    // Pagination
    [Range(1, int.MaxValue)]
    [Display(Name = "Page")]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    [Display(Name = "Items Per Page")]
    public int PageSize { get; set; } = 12;

    // Results
    public IEnumerable<Trip> Trips { get; set; } = new List<Trip>();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    // For Dropdowns
    public List<string> Origins { get; set; } = new();
    public List<string> Destinations { get; set; } = new();
    public List<string> VehicleTypes { get; set; } = new();

    // Computed Properties
    public bool HasFiltersApplied =>
        !string.IsNullOrWhiteSpace(Origin) ||
        !string.IsNullOrWhiteSpace(Destination) ||
        !string.IsNullOrWhiteSpace(Search) ||
        MinPrice.HasValue ||
        MaxPrice.HasValue ||
        !string.IsNullOrWhiteSpace(VehicleType) ||
        MinimumRating.HasValue ||
        DepartureTimeFrom.HasValue ||
        DepartureTimeTo.HasValue;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice.HasValue && MinPrice.Value < 0)
        {
            yield return new ValidationResult(
                "Minimum price cannot be less than 0.",
                new[] { nameof(MinPrice) });
        }

        if (MinPrice.HasValue && MaxPrice.HasValue && MaxPrice.Value < MinPrice.Value)
        {
            yield return new ValidationResult(
                "Maximum price cannot be less than minimum price.",
                new[] { nameof(MaxPrice) });
        }
    }
}