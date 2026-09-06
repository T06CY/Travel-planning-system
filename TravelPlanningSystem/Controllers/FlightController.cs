using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class FlightController(AppDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(FlightSearchViewModel model)
    {
        model.Airports = await context.Airports
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Country)
            .ThenBy(a => a.City)
            .ToListAsync();

        model.SearchPerformed = Request.Query.ContainsKey("search");

        if (!model.SearchPerformed)
        {
            model.TripType = "Return";
            model.DepartureDate = DateTime.Today.AddDays(1);
            model.ReturnDate = DateTime.Today.AddDays(3);
            EnsureTwoMultiCityRows(model);
            return View(model);
        }

        model.Passengers = Math.Clamp(model.Passengers, 1, 9);
        model.TripType = NormaliseTripType(model.TripType);

        if (model.TripType == "Multi-city")
        {
            await BuildMultiCityResultsAsync(model);
        }
        else
        {
            await BuildNormalResultsAsync(model);
            EnsureTwoMultiCityRows(model);
        }

        return View(model);
    }

    private async Task BuildNormalResultsAsync(FlightSearchViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.From) ||
            string.IsNullOrWhiteSpace(model.To) ||
            !model.DepartureDate.HasValue)
        {
            ModelState.AddModelError(string.Empty,
                "Select an origin, destination and departure date.");
            return;
        }

        if (model.From == model.To)
        {
            ModelState.AddModelError(string.Empty,
                "Origin and destination must be different.");
            return;
        }

        if (model.DepartureDate.Value.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.DepartureDate),
                "Departure date cannot be in the past.");
            return;
        }

        model.Sections.Add(new FlightSearchSection
        {
            SegmentNumber = 1,
            Label = model.TripType == "Return" ? "Outbound flight" : "Departure flight",
            From = model.From,
            To = model.To,
            Date = model.DepartureDate.Value.Date,
            Flights = await FindFlightsAsync(
                model.From,
                model.To,
                model.DepartureDate.Value.Date,
                model.Passengers,
                model.SortBy)
        });

        if (model.TripType != "Return")
        {
            return;
        }

        if (!model.ReturnDate.HasValue ||
            model.ReturnDate.Value.Date < model.DepartureDate.Value.Date)
        {
            ModelState.AddModelError(nameof(model.ReturnDate),
                "Return date must be on or after the departure date.");
            return;
        }

        model.Sections.Add(new FlightSearchSection
        {
            SegmentNumber = 2,
            Label = "Return flight",
            From = model.To,
            To = model.From,
            Date = model.ReturnDate.Value.Date,
            Flights = await FindFlightsAsync(
                model.To,
                model.From,
                model.ReturnDate.Value.Date,
                model.Passengers,
                model.SortBy)
        });
    }

    private async Task BuildMultiCityResultsAsync(FlightSearchViewModel model)
    {
        var count = Math.Min(
            Math.Min(model.SegmentFrom.Count, model.SegmentTo.Count),
            model.SegmentDate.Count);

        if (count < 2 || count > 5)
        {
            ModelState.AddModelError(string.Empty,
                "Multi-city requires between 2 and 5 flight segments.");
            EnsureTwoMultiCityRows(model);
            return;
        }

        for (var index = 0; index < count; index++)
        {
            var from = model.SegmentFrom[index]?.Trim();
            var to = model.SegmentTo[index]?.Trim();
            var date = model.SegmentDate[index];

            if (string.IsNullOrWhiteSpace(from) ||
                string.IsNullOrWhiteSpace(to) ||
                !date.HasValue ||
                date.Value.Date < DateTime.Today ||
                from == to)
            {
                ModelState.AddModelError(string.Empty,
                    $"Complete valid information for Flight {index + 1}.");
                continue;
            }

            if (index > 0 &&
                !string.Equals(model.SegmentTo[index - 1], from,
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty,
                    $"Flight {index + 1} must leave from the previous destination.");
                continue;
            }

            if (index > 0 && model.SegmentDate[index - 1].HasValue &&
                date.Value.Date < model.SegmentDate[index - 1]!.Value.Date)
            {
                ModelState.AddModelError(string.Empty,
                    "Multi-city dates must be in travel order.");
                continue;
            }

            model.Sections.Add(new FlightSearchSection
            {
                SegmentNumber = index + 1,
                Label = $"Flight {index + 1}",
                From = from,
                To = to,
                Date = date.Value.Date,
                Flights = await FindFlightsAsync(
                    from,
                    to,
                    date.Value.Date,
                    model.Passengers,
                    model.SortBy)
            });
        }
    }

    private async Task<List<Flight>> FindFlightsAsync(
        string from,
        string to,
        DateTime date,
        int passengers,
        string sortBy)
    {
        var nextDate = date.AddDays(1);

        var query = context.Flights
            .AsNoTracking()
            .Include(f => f.Airline)
            .Where(f =>
                f.IsActive &&
                f.Airline != null &&
                f.Airline.IsActive &&
                f.From == from &&
                f.To == to &&
                f.DepartureTime >= date &&
                f.DepartureTime < nextDate &&
                f.AvailableSeats >= passengers &&
                (f.Status == FlightStatus.Scheduled ||
                 f.Status == FlightStatus.Delayed));

        query = sortBy switch
        {
            "price-low" => query.OrderBy(f => f.Price),
            "price-high" => query.OrderByDescending(f => f.Price),
            "duration" => query.OrderBy(f =>
                EF.Functions.DateDiffMinute(f.DepartureTime, f.ArrivalTime)),
            "seats" => query.OrderByDescending(f => f.AvailableSeats),
            _ => query.OrderBy(f => f.DepartureTime)
        };

        return await query.ToListAsync();
    }

    private static string NormaliseTripType(string? tripType) =>
        tripType is "One-way" or "Multi-city" ? tripType : "Return";

    private static void EnsureTwoMultiCityRows(FlightSearchViewModel model)
    {
        while (model.SegmentFrom.Count < 2) model.SegmentFrom.Add(string.Empty);
        while (model.SegmentTo.Count < 2) model.SegmentTo.Add(string.Empty);
        while (model.SegmentDate.Count < 2) model.SegmentDate.Add(null);
    }
}
