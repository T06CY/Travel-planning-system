using Microsoft.AspNetCore.Mvc.Rendering;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class ActivitySearchViewModel
{
    public string? Search { get; set; }
    public string? Destination { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public DateTime? Date { get; set; }
    public string Sort { get; set; } = "featured";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 6;
    public int TotalItems { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public IReadOnlyList<Activity> Activities { get; set; } = [];
    public IEnumerable<SelectListItem> Categories { get; set; } = [];
    public IEnumerable<SelectListItem> Destinations { get; set; } = [];
}
