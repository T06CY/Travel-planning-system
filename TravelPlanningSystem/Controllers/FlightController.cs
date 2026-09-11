using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.Services;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class FlightController(
    AppDbContext context,
    IRealTimeFlightService realTimeFlightService,
    IAirportLookupService airportLookupService,
    ILogger<FlightController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        FlightSearchViewModel model,
        CancellationToken cancellationToken)
    {
        model.Airports = await context.Airports.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Country)
            .ThenBy(a => a.City)
            .ToListAsync(cancellationToken);

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
            await BuildMultiCityResultsAsync(model, cancellationToken);
        }
        else
        {
            await BuildNormalResultsAsync(model, cancellationToken);
            EnsureTwoMultiCityRows(model);
            if (model.TripType != "Multi-city" && model.DepartureDate.HasValue)
                model.DatePriceOptions = await BuildDatePriceOptionsAsync(model, cancellationToken);
        }

        return View(model);
    }

    private async Task<List<FlightDatePriceOption>> BuildDatePriceOptionsAsync(
        FlightSearchViewModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.From) || string.IsNullOrWhiteSpace(model.To))
            return new List<FlightDatePriceOption>();

        var selectedDate = model.DepartureDate!.Value.Date;
        var firstDate = selectedDate.AddDays(-3) < DateTime.Today
            ? DateTime.Today
            : selectedDate.AddDays(-3);
        var lastDate = firstDate.AddDays(6);
        var flights = await context.Flights.AsNoTracking()
            .Where(f => f.From == model.From && f.To == model.To &&
                        f.DepartureTime >= firstDate &&
                        f.DepartureTime < lastDate.AddDays(1) &&
                        f.Status != FlightStatus.Cancelled &&
                        f.AvailableSeats >= model.Passengers)
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, 7).Select(offset =>
        {
            var date = firstDate.AddDays(offset);
            var dayFlights = flights.Where(f => f.DepartureTime.Date == date).ToList();
            return new FlightDatePriceOption
            {
                Date = date,
                FlightCount = dayFlights.Count(),
                LowestPrice = dayFlights.Count == 0 ? null : dayFlights.Select(f => FlightFareRules.GetPrice(f.Price, FlightFareRules.Economy, f.DiscountPercent)).Min()
            };
        }).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> SearchAirports(
        string? term,
        CancellationToken cancellationToken)
    {
        var cleanedTerm = term?.Trim() ?? string.Empty;

        if (cleanedTerm.Length < 3 || cleanedTerm.Length > 60)
        {
            return Json(new
            {
                source = "Database",
                message = "Enter between 3 and 60 characters.",
                airports = Array.Empty<object>()
            });
        }

        var liveResult = await airportLookupService.SearchAsync(
            cleanedTerm,
            cancellationToken);

        var databaseAirportRows = await context.Airports.AsNoTracking()
            .Where(a =>
                a.IsActive &&
                (a.City.Contains(cleanedTerm) ||
                 a.AirportName.Contains(cleanedTerm) ||
                 a.AirportCode.Contains(cleanedTerm)))
            .OrderBy(a => a.City)
            .Take(15)
            .ToListAsync(cancellationToken);

        var databaseAirports = databaseAirportRows
            .Select(a => new AirportSuggestion(
                a.AirportCode,
                a.AirportName,
                a.City,
                a.Country))
            .ToList();

        var airports = liveResult.Airports
            .Concat(databaseAirports)
            .Where(a =>
                !string.IsNullOrWhiteSpace(a.AirportCode) &&
                !string.IsNullOrWhiteSpace(a.City))
            .GroupBy(
                a => a.AirportCode,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Take(20)
            .Select(a => new
            {
                code = a.AirportCode,
                name = a.AirportName,
                city = a.City,
                country = a.CountryCode
            })
            .ToList();

        return Json(new
        {
            source = liveResult.IsLiveData
                ? "AeroDataBox"
                : "Database",
            message = liveResult.IsLiveData
                ? liveResult.Message
                : "Using database airport suggestions.",
            airports
        });
    }

    private async Task BuildNormalResultsAsync(
        FlightSearchViewModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(model.From) ||
            string.IsNullOrWhiteSpace(model.To) ||
            !model.DepartureDate.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "Select an origin, destination and departure date.");
            return;
        }

        var fromAirportCode = NormaliseAirportCode(
            model.FromAirportCode);
        var toAirportCode = NormaliseAirportCode(
            model.ToAirportCode);

        if (fromAirportCode is null || toAirportCode is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Select both airports from the suggestion list.");
            return;
        }

        if (string.Equals(
                fromAirportCode,
                toAirportCode,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                string.Empty,
                "Origin and destination must be different.");
            return;
        }

        if (model.DepartureDate.Value.Date < DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(model.DepartureDate),
                "Departure date cannot be in the past.");
            return;
        }

        var outbound = await FindFlightsAsync(
            model.From,
            model.To,
            fromAirportCode,
            toAirportCode,
            model.DepartureDate.Value.Date,
            model.Passengers,
            model.SortBy,
            cancellationToken);

        model.Sections.Add(CreateSection(
            1,
            model.TripType == "Return"
                ? "Outbound flight"
                : "Departure flight",
            model.From,
            model.To,
            model.DepartureDate.Value.Date,
            outbound));

        if (model.TripType != "Return")
        {
            return;
        }

        if (!model.ReturnDate.HasValue ||
            model.ReturnDate.Value.Date <
            model.DepartureDate.Value.Date)
        {
            ModelState.AddModelError(
                nameof(model.ReturnDate),
                "Return date must be on or after the departure date.");
            return;
        }

        var inbound = await FindFlightsAsync(
            model.To,
            model.From,
            toAirportCode,
            fromAirportCode,
            model.ReturnDate.Value.Date,
            model.Passengers,
            model.SortBy,
            cancellationToken);

        model.Sections.Add(CreateSection(
            2,
            "Return flight",
            model.To,
            model.From,
            model.ReturnDate.Value.Date,
            inbound));
    }

    private async Task BuildMultiCityResultsAsync(
        FlightSearchViewModel model,
        CancellationToken cancellationToken)
    {
        var count = Math.Min(
            Math.Min(model.SegmentFrom.Count, model.SegmentTo.Count),
            model.SegmentDate.Count);

        if (count < 2 || count > 5)
        {
            ModelState.AddModelError(
                string.Empty,
                "Multi-city requires between 2 and 5 flight segments.");
            EnsureTwoMultiCityRows(model);
            return;
        }

        for (var index = 0; index < count; index++)
        {
            var from = model.SegmentFrom[index]?.Trim();
            var to = model.SegmentTo[index]?.Trim();
            var fromCode = model.SegmentFromAirportCode
                .ElementAtOrDefault(index);
            var toCode = model.SegmentToAirportCode
                .ElementAtOrDefault(index);
            var normalisedFromCode = NormaliseAirportCode(fromCode);
            var normalisedToCode = NormaliseAirportCode(toCode);
            var date = model.SegmentDate[index];

            if (string.IsNullOrWhiteSpace(from) ||
                string.IsNullOrWhiteSpace(to) ||
                normalisedFromCode is null ||
                normalisedToCode is null ||
                !date.HasValue ||
                date.Value.Date < DateTime.Today ||
                string.Equals(
                    normalisedFromCode,
                    normalisedToCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Complete valid information for Flight {index + 1}.");
                continue;
            }

            if (index > 0 &&
                !string.Equals(
                    NormaliseAirportCode(
                        model.SegmentToAirportCode
                            .ElementAtOrDefault(index - 1)),
                    normalisedFromCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Flight {index + 1} must leave from the previous destination.");
                continue;
            }

            if (index > 0 &&
                model.SegmentDate[index - 1].HasValue &&
                date.Value.Date <
                model.SegmentDate[index - 1]!.Value.Date)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Multi-city dates must be in travel order.");
                continue;
            }

            var lookup = await FindFlightsAsync(
                from,
                to,
                normalisedFromCode,
                normalisedToCode,
                date.Value.Date,
                model.Passengers,
                model.SortBy,
                cancellationToken);

            model.Sections.Add(CreateSection(
                index + 1,
                $"Flight {index + 1}",
                from,
                to,
                date.Value.Date,
                lookup));
        }
    }

    private async Task<FlightLookupResult> FindFlightsAsync(
        string from,
        string to,
        string? fromAirportCode,
        string? toAirportCode,
        DateTime date,
        int passengers,
        string sortBy,
        CancellationToken cancellationToken)
    {
        var liveSync = await SyncRealTimeFlightsAsync(
            from,
            to,
            fromAirportCode,
            toAirportCode,
            date,
            passengers,
            cancellationToken);

        var nextDate = date.AddDays(1);
        var query = context.Flights.AsNoTracking()
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
                EF.Functions.DateDiffMinute(
                    f.DepartureTime,
                    f.ArrivalTime)),
            "seats" => query.OrderByDescending(f => f.AvailableSeats),
            _ => query.OrderBy(f => f.DepartureTime)
        };

        var flights = await query.ToListAsync(cancellationToken);

        if (liveSync.UsesLiveData)
        {
            flights = flights.Where(f =>
                liveSync.LiveFlightKeys.Contains(
                    CreateFlightKey(
                        f.FlightNumber,
                        f.DepartureTime)))
                .ToList();

            flights.ForEach(f => f.IsRealTimeResult = true);
        }

        return new FlightLookupResult(
            flights,
            liveSync.UsesLiveData,
            liveSync.Message,
            liveSync.CheckedAtUtc);
    }

    private async Task<LiveFlightSyncResult> SyncRealTimeFlightsAsync(
        string from,
        string to,
        string? submittedFromCode,
        string? submittedToCode,
        DateTime date,
        int passengers,
        CancellationToken cancellationToken)
    {
        var airports = await context.Airports.AsNoTracking()
            .Where(a =>
                a.IsActive &&
                (a.City == from || a.City == to))
            .OrderBy(a => a.AirportId)
            .ToListAsync(cancellationToken);

        var fromCode = NormaliseAirportCode(submittedFromCode) ??
            airports.FirstOrDefault(a => a.City == from)
                ?.AirportCode;
        var toCode = NormaliseAirportCode(submittedToCode) ??
            airports.FirstOrDefault(a => a.City == to)
                ?.AirportCode;

        if (string.IsNullOrWhiteSpace(fromCode) ||
            string.IsNullOrWhiteSpace(toCode))
        {
            return EmptyLiveResult(
                "Airport codes are unavailable. Showing database schedules.");
        }

        var liveResult = await realTimeFlightService.SearchAsync(
            fromCode,
            toCode,
            date,
            passengers,
            cancellationToken);

        if (!liveResult.WasSuccessful ||
            liveResult.Offers.Count == 0)
        {
            return EmptyLiveResult(
                liveResult.Message,
                liveResult.CheckedAtUtc);
        }

        var usableOffers = liveResult.Offers
            .Where(offer =>
                offer.DepartureTime.Date == date.Date)
            .ToList();

        if (usableOffers.Count == 0)
        {
            return EmptyLiveResult(
                "AeroDataBox returned no matching schedule. Showing database schedules.",
                liveResult.CheckedAtUtc);
        }

        var airlineCodes = usableOffers
            .Select(offer =>
                offer.AirlineCode.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        var airlines = await context.Airlines
            .Where(a => airlineCodes.Contains(a.AirlineCode))
            .ToListAsync(cancellationToken);

        foreach (var offer in usableOffers)
        {
            var code = offer.AirlineCode.Trim().ToUpperInvariant();

            if (airlines.Any(a => a.AirlineCode == code))
            {
                continue;
            }

            var airline = new Airline
            {
                AirlineCode = code,
                AirlineName = string.IsNullOrWhiteSpace(offer.AirlineName)
                    ? code
                    : offer.AirlineName,
                Country = "International",
                LogoUrl = GetAirlineLogoUrl(code),
                IsActive = true
            };

            context.Airlines.Add(airline);
            airlines.Add(airline);
        }

        await context.SaveChangesAsync(cancellationToken);

        var nextDate = date.AddDays(1);
        var existingFlights = await context.Flights
            .Where(f =>
                f.From == from &&
                f.To == to &&
                f.DepartureTime >= date &&
                f.DepartureTime < nextDate)
            .ToListAsync(cancellationToken);

        var liveKeys = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var offer in usableOffers)
        {
            var code = offer.AirlineCode.Trim().ToUpperInvariant();
            var airline = airlines.First(a => a.AirlineCode == code);
            var flightNumber =
                offer.FlightNumber.Trim().ToUpperInvariant();
            var key = CreateFlightKey(
                flightNumber,
                offer.DepartureTime);

            liveKeys.Add(key);

            var flight = existingFlights.FirstOrDefault(f =>
                CreateFlightKey(
                    f.FlightNumber,
                    f.DepartureTime) == key);

            var databaseTemplate = existingFlights
                .Where(f => f.AvailableSeats >= passengers)
                .OrderBy(f => Math.Abs(
                    (f.DepartureTime - offer.DepartureTime)
                    .TotalMinutes))
                .FirstOrDefault();

            var fallbackPrice =
                databaseTemplate?.Price ?? 299m;

            var fallbackCapacity =
                databaseTemplate?.SeatCapacity ?? 180;

            var fallbackSeats = Math.Clamp(
                databaseTemplate?.AvailableSeats ?? 30,
                passengers,
                fallbackCapacity);

            if (flight is null)
            {
                flight = new Flight
                {
                    AirlineId = airline.AirlineId,
                    FlightNumber = flightNumber,
                    From = from,
                    To = to,
                    DepartureTime = offer.DepartureTime,
                    ArrivalTime = offer.ArrivalTime,
                    Price = offer.PriceMyr ?? fallbackPrice,
                    SeatCapacity = fallbackCapacity,
                    AvailableSeats =
                        offer.AvailableSeats.HasValue
                            ? Math.Clamp(
                                offer.AvailableSeats.Value,
                                passengers,
                                fallbackCapacity)
                            : fallbackSeats,
                    AircraftModel = string.IsNullOrWhiteSpace(
                        offer.AircraftModel)
                        ? "Aircraft"
                        : offer.AircraftModel,
                    FlightLogoUrl =
                        airline.LogoUrl ??
                        GetAirlineLogoUrl(code),
                    Status = MapFlightStatus(
                        offer.FlightStatus),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Flights.Add(flight);
                existingFlights.Add(flight);
                continue;
            }

            flight.AirlineId = airline.AirlineId;
            flight.ArrivalTime = offer.ArrivalTime;

            if (offer.PriceMyr.HasValue)
            {
                flight.Price = offer.PriceMyr.Value;
            }

            if (offer.AvailableSeats.HasValue)
            {
                flight.AvailableSeats = Math.Min(
                    flight.AvailableSeats,
                    Math.Clamp(
                        offer.AvailableSeats.Value,
                        0,
                        flight.SeatCapacity));
            }

            var apiStatus = MapFlightStatus(offer.FlightStatus);
            // Preserve an administrator's local disruption decision. A later
            // API refresh must not change Delayed/Cancelled back to Scheduled.
            if (flight.Status is not FlightStatus.Delayed and not FlightStatus.Cancelled)
                flight.Status = apiStatus;
            flight.AircraftModel =
                string.IsNullOrWhiteSpace(offer.AircraftModel)
                    ? flight.AircraftModel
                    : offer.AircraftModel;
            flight.FlightLogoUrl =
                airline.LogoUrl ??
                GetAirlineLogoUrl(code);
            flight.IsActive = true;
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(
                exception,
                "Live offers were received but could not be cached.");

            return EmptyLiveResult(
                "Live offers could not be saved. Showing database schedules.",
                liveResult.CheckedAtUtc);
        }

        return new LiveFlightSyncResult(
            true,
            "Flight schedule and status refreshed successfully.",
            liveResult.CheckedAtUtc,
            liveKeys);
    }

    private static FlightSearchSection CreateSection(
        int segmentNumber,
        string label,
        string from,
        string to,
        DateTime date,
        FlightLookupResult lookup) =>
        new()
        {
            SegmentNumber = segmentNumber,
            Label = label,
            From = from,
            To = to,
            Date = date,
            Flights = lookup.Flights,
            UsesLiveData = lookup.UsesLiveData,
            DataSourceMessage = lookup.Message,
            LiveCheckedAtUtc = lookup.CheckedAtUtc
        };

    private static LiveFlightSyncResult EmptyLiveResult(
        string message,
        DateTime? checkedAtUtc = null) =>
        new(
            false,
            message,
            checkedAtUtc ?? DateTime.UtcNow,
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase));

    private static string CreateFlightKey(
        string flightNumber,
        DateTime departureTime) =>
        $"{flightNumber.Trim().ToUpperInvariant()}|" +
        $"{departureTime:yyyyMMddHHmmss}";

    private static string? GetAirlineLogoUrl(
        string airlineCode) =>
        airlineCode.ToUpperInvariant() switch
        {
            "MH" => "/images/flights/MalaysiaAirlines.png",
            "AK" => "/images/flights/AirAsia.png",
            "OD" => "/images/flights/BatikAir.png",
            "SQ" => "/images/flights/SingaporeAirlines.png",
            "TG" => "/images/flights/ThaiAirways.png",
            "JL" => "/images/flights/JapanAirlines.png",
            _ => null
        };

    private static FlightStatus MapFlightStatus(
        string? providerStatus) =>
        providerStatus?.Trim().ToLowerInvariant() switch
        {
            "active" or "enroute" or "departed" =>
                FlightStatus.Departed,
            "landed" or "arrived" => FlightStatus.Arrived,
            "boarding" or "gateclosed" or "checkin" =>
                FlightStatus.Boarding,
            "cancelled" or "canceled" or "canceleduncertain" =>
                FlightStatus.Cancelled,
            "incident" or "diverted" or "delayed" =>
                FlightStatus.Delayed,
            _ => FlightStatus.Scheduled
        };

    private static string NormaliseTripType(
        string? tripType) =>
        tripType is "One-way" or "Multi-city"
            ? tripType
            : "Return";

    private static string? NormaliseAirportCode(string? value)
    {
        var code = value?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(code) ||
            code.Length != 3 ||
            !code.All(char.IsLetterOrDigit))
        {
            return null;
        }

        return code;
    }

    private static void EnsureTwoMultiCityRows(
        FlightSearchViewModel model)
    {
        while (model.SegmentFrom.Count < 2)
        {
            model.SegmentFrom.Add(string.Empty);
        }

        while (model.SegmentTo.Count < 2)
        {
            model.SegmentTo.Add(string.Empty);
        }

        while (model.SegmentFromAirportCode.Count < 2)
        {
            model.SegmentFromAirportCode.Add(string.Empty);
        }

        while (model.SegmentToAirportCode.Count < 2)
        {
            model.SegmentToAirportCode.Add(string.Empty);
        }

        while (model.SegmentDate.Count < 2)
        {
            model.SegmentDate.Add(null);
        }
    }

    private sealed record FlightLookupResult(
        List<Flight> Flights,
        bool UsesLiveData,
        string Message,
        DateTime CheckedAtUtc);

    private sealed record LiveFlightSyncResult(
        bool UsesLiveData,
        string Message,
        DateTime CheckedAtUtc,
        HashSet<string> LiveFlightKeys);
}
