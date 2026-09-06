using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models.Transportation;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class TransportationController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(TransportationSearchViewModel model)
    {
        // Ensure valid page number
        model.Page = Math.Max(1, model.Page);

        // Get all routes for dropdown
        var routes = await context.Routes
            .Where(r => r.IsActive)
            .ToListAsync();

        model.Origins = routes
            .Select(r => r.Origin)
            .Distinct()
            .OrderBy(o => o)
            .ToList();

        model.Destinations = routes
            .Select(r => r.Destination)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        model.VehicleTypes = await context.Vehicles
            .Where(v => v.IsActive)
            .Select(v => v.VehicleType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        // Build base query
        var query = context.Trips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Reviews.Where(r => r.IsVisible))
            .Where(t => t.IsActive && t.Route!.IsActive && t.Vehicle!.IsActive);

        // Apply search filters
        if (!string.IsNullOrWhiteSpace(model.Origin))
        {
            query = query.Where(t => t.Route!.Origin == model.Origin);
        }

        if (!string.IsNullOrWhiteSpace(model.Destination))
        {
            query = query.Where(t => t.Route!.Destination == model.Destination);
        }

        if (model.DepartureDate.HasValue)
        {
            var targetDate = model.DepartureDate.Value.Date;
            var nextDay = targetDate.AddDays(1);
            query = query.Where(t => t.DepartureTime >= targetDate && t.DepartureTime < nextDay);
        }

        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var search = model.Search.Trim().ToLower();
            query = query.Where(t =>
                t.Route!.Origin.ToLower().Contains(search) ||
                t.Route.Destination.ToLower().Contains(search) ||
                t.Vehicle!.VehicleModel.ToLower().Contains(search) ||
                t.Vehicle.VehicleType.ToLower().Contains(search));
        }

        // Apply price filter
        if (model.MinPrice.HasValue)
        {
            query = query.Where(t => t.BaseFare >= model.MinPrice.Value);
        }

        if (model.MaxPrice.HasValue)
        {
            query = query.Where(t => t.BaseFare <= model.MaxPrice.Value);
        }

        // Apply vehicle type filter
        if (!string.IsNullOrWhiteSpace(model.VehicleType))
        {
            query = query.Where(t => t.Vehicle!.VehicleType == model.VehicleType);
        }

        // Apply seat availability filter
        if (model.OnlyAvailableSeats)
        {
            query = query.Where(t => t.AvailableSeats > 0);
        }

        // Apply departure time filter
        if (model.DepartureTimeFrom.HasValue || model.DepartureTimeTo.HasValue)
        {
            var timeFrom = model.DepartureTimeFrom ?? TimeOnly.MinValue;
            var timeTo = model.DepartureTimeTo ?? TimeOnly.MaxValue;

            query = query.Where(t =>
                t.DepartureTime.TimeOfDay >= timeFrom.ToTimeSpan() &&
                t.DepartureTime.TimeOfDay <= timeTo.ToTimeSpan());
        }

        // Get total count before pagination
        model.TotalCount = await query.CountAsync();

        // Apply sorting
        query = model.SortBy switch
        {
            "price-high" => query.OrderByDescending(t => t.BaseFare),
            "price-low" => query.OrderBy(t => t.BaseFare),
            "rating" => query.OrderByDescending(t =>
                t.Reviews.Any() ? t.Reviews.Average(r => r.Rating) : 0),
            "duration" => query.OrderBy(t =>
                EF.Functions.DateDiffSecond(t.DepartureTime, t.ArrivalTime)),
            _ => query.OrderBy(t => t.DepartureTime)
        };

        // Apply pagination
        var trips = await query
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();

        model.Trips = trips;

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var trip = await context.Trips
            .AsNoTracking()
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Seats)
            .Include(t => t.Reviews.Where(r => r.IsVisible))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(t => t.TripId == id);

        if (trip == null)
            return NotFound();

        return View(trip);
    }
}
