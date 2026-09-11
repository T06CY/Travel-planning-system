using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminFlightsController(
    AppDbContext context,
    IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index(
        string? search,
        FlightStatus? status,
        DateTime? date,
        int page = 1)
    {
        const int pageSize = 30;
        page = Math.Max(1, page);
        var query = context.Flights
            .AsNoTracking()
            .Include(f => f.Airline)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f =>
                f.FlightNumber.Contains(search) ||
                f.From.Contains(search) ||
                f.To.Contains(search) ||
                (f.Airline != null && f.Airline.AirlineName.Contains(search)));
        }

        if (status.HasValue) query = query.Where(f => f.Status == status);
        if (date.HasValue)
        {
            var next = date.Value.Date.AddDays(1);
            query = query.Where(f =>
                f.DepartureTime >= date.Value.Date &&
                f.DepartureTime < next);
        }

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");
        var totalRecords = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
        page = Math.Min(page, totalPages);

        var flights = await query
            .OrderBy(f => f.DepartureTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.TotalPages = totalPages;

        var airports = await context.Airports.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.City)
            .ToListAsync();

        return View(new AdminFlightIndexViewModel
        {
            Flights = flights,
            Airports = airports
        });
    }

    [NonAction]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new FlightFormViewModel();
        await LoadListsAsync(model);
        return View(model);
    }

    [NonAction]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FlightFormViewModel model)
    {
        await ValidateFlightAsync(model);
        if (!ModelState.IsValid)
        {
            await LoadListsAsync(model);
            return View(model);
        }

        var flight = new Flight
        {
            AvailableSeats = model.SeatCapacity,
            CreatedAt = DateTime.UtcNow
        };
        MapFlight(flight, model);
        context.Flights.Add(flight);
        await context.SaveChangesAsync();
        await SaveFlightImageAsync(flight, model.FlightImage);

        TempData["Message"] = "Flight schedule created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [NonAction]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var flight = await context.Flights.FindAsync(id);
        if (flight is null) return NotFound();

        var model = new FlightFormViewModel
        {
            FlightId = flight.FlightId,
            FlightNumber = flight.FlightNumber,
            AirlineId = flight.AirlineId,
            From = flight.From,
            To = flight.To,
            DepartureTime = flight.DepartureTime,
            ArrivalTime = flight.ArrivalTime,
            Price = flight.Price,
            DiscountPercent = flight.DiscountPercent,
            SeatCapacity = flight.SeatCapacity,
            AircraftModel = flight.AircraftModel,
            Status = flight.Status,
            IsActive = flight.IsActive,
            ExistingFlightImageUrl = flight.FlightLogoUrl
            ,
            FlightImagePath = flight.FlightLogoUrl
        };
        await LoadListsAsync(model);
        return View(model);
    }

    [NonAction]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FlightFormViewModel model)
    {
        if (id != model.FlightId) return BadRequest();
        var flight = await context.Flights.FindAsync(id);
        if (flight is null) return NotFound();

        await ValidateFlightAsync(model, id);
        var reservedSeats = flight.SeatCapacity - flight.AvailableSeats;
        if (model.SeatCapacity < reservedSeats)
        {
            ModelState.AddModelError(nameof(model.SeatCapacity),
                $"Capacity cannot be lower than {reservedSeats} reserved seat(s).");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(model);
            return View(model);
        }

        var capacityDifference = model.SeatCapacity - flight.SeatCapacity;
        MapFlight(flight, model);
        flight.AvailableSeats += capacityDifference;
        await SaveFlightImageAsync(flight, model.FlightImage);
        await context.SaveChangesAsync();

        TempData["Message"] = "Flight schedule updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var flight = await context.Flights
            .AsNoTracking()
            .Include(f => f.Airline)
            .FirstOrDefaultAsync(f => f.FlightId == id);
        return flight is null ? NotFound() : View(flight);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var flight = await context.Flights.FindAsync(id);
        if (flight is null) return NotFound();

        if (await context.FlightBookingSegments.AnyAsync(s => s.FlightId == id))
        {
            flight.IsActive = false;
            flight.Status = FlightStatus.Cancelled;
            TempData["Message"] = "Flight has booking history, so it was cancelled and deactivated.";
        }
        else
        {
            context.Flights.Remove(flight);
            TempData["Message"] = "Flight schedule removed successfully.";
        }

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, FlightStatus status)
    {
        if (!Enum.IsDefined(status)) return BadRequest();
        var flight = await context.Flights.FindAsync(id);
        if (flight is null) return NotFound();
        flight.Status = status;
        flight.IsActive = status != FlightStatus.Cancelled;
        await context.SaveChangesAsync();
        TempData["Message"] = $"{flight.FlightNumber} status changed to {status}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateInventory(int id, decimal price, int seatCapacity, decimal discountPercent)
    {
        var flight = await context.Flights.FindAsync(id);
        if (flight is null) return NotFound();

        var reservedSeats = Math.Max(0, flight.SeatCapacity - flight.AvailableSeats);
        if (price <= 0 || discountPercent < 0 || discountPercent > 100 || seatCapacity < reservedSeats || seatCapacity > 600)
        {
            TempData["Error"] =
                $"Price must be positive and capacity must be between {reservedSeats} and 600.";
            return RedirectToAction(nameof(Index));
        }

        flight.Price = price;
        flight.DiscountPercent = discountPercent;
        flight.SeatCapacity = seatCapacity;
        flight.AvailableSeats = seatCapacity - reservedSeats;
        await context.SaveChangesAsync();

        TempData["Message"] = $"{flight.FlightNumber} price and seat capacity updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateFlightAsync(FlightFormViewModel model, int? existingId = null)
    {
        model.FlightNumber = (model.FlightNumber ?? string.Empty).Trim().ToUpperInvariant();
        model.From = (model.From ?? string.Empty).Trim();
        model.To = (model.To ?? string.Empty).Trim();
        model.AircraftModel = (model.AircraftModel ?? string.Empty).Trim();
        if (model.From == model.To)
            ModelState.AddModelError(nameof(model.To), "Destination must be different from origin.");
        if (model.ArrivalTime <= model.DepartureTime)
            ModelState.AddModelError(nameof(model.ArrivalTime), "Arrival must be after departure.");
        if (!existingId.HasValue && model.DepartureTime <= DateTime.Now)
            ModelState.AddModelError(nameof(model.DepartureTime), "A new schedule must be in the future.");
        if (!await context.Airlines.AnyAsync(a => a.AirlineId == model.AirlineId && a.IsActive))
            ModelState.AddModelError(nameof(model.AirlineId), "Select an active airline.");
        if (await context.Flights.AnyAsync(f =>
            (!existingId.HasValue || f.FlightId != existingId.Value) &&
            f.FlightNumber == model.FlightNumber &&
            f.DepartureTime == model.DepartureTime))
            ModelState.AddModelError(nameof(model.FlightNumber), "This flight number and departure time already exist.");

        if (model.AirlineImage is not null)
        {
            var extension = Path.GetExtension(model.AirlineImage.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension) ||
                model.AirlineImage.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.AirlineImage),
                    "Upload a JPG, PNG or WebP image no larger than 5 MB.");
            }
        }

        if (model.FlightImage is not null)
        {
            var extension = Path.GetExtension(model.FlightImage.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension) || model.FlightImage.Length > 5 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.FlightImage), "Upload a JPG, PNG or WebP image no larger than 5 MB.");
        }

        if (string.IsNullOrWhiteSpace(model.FlightImagePath) && (model.FlightImage is null || model.FlightImage.Length == 0))
            ModelState.AddModelError(nameof(model.FlightImagePath), "Select or upload an image for this flight.");
    }

    private async Task LoadListsAsync(FlightFormViewModel model)
    {
        model.Airlines = await context.Airlines.AsNoTracking()
            .Where(a => a.IsActive).OrderBy(a => a.AirlineName).ToListAsync();
        model.Airports = await context.Airports.AsNoTracking()
            .Where(a => a.IsActive).OrderBy(a => a.Country).ThenBy(a => a.City).ToListAsync();
        var folder = Path.Combine(environment.WebRootPath, "images", "flights");
        model.AvailableFlightImages = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder)
                .Where(file => new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .Select(file => "/images/flights/" + Path.GetFileName(file))
                .OrderBy(path => path)
                .ToList()
            : new List<string>();
        if (string.IsNullOrWhiteSpace(model.FlightImagePath) && model.AvailableFlightImages.Count > 0 && model.FlightId == 0)
            model.FlightImagePath = model.AvailableFlightImages[0];
    }

    private static void MapFlight(Flight flight, FlightFormViewModel model)
    {
        flight.FlightNumber = model.FlightNumber.Trim().ToUpperInvariant();
        flight.AirlineId = model.AirlineId;
        flight.From = model.From;
        flight.To = model.To;
        flight.DepartureTime = model.DepartureTime;
        flight.ArrivalTime = model.ArrivalTime;
        flight.Price = model.Price;
        flight.DiscountPercent = model.DiscountPercent;
        flight.SeatCapacity = model.SeatCapacity;
        flight.AircraftModel = model.AircraftModel.Trim();
        if (!string.IsNullOrWhiteSpace(model.FlightImagePath))
            flight.FlightLogoUrl = model.FlightImagePath;
        flight.Status = model.Status;
        flight.IsActive = model.IsActive;
    }

    private async Task SaveAirlineImageAsync(int airlineId, IFormFile? image)
    {
        if (image is null || image.Length == 0) return;
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension) ||
            image.Length > 5 * 1024 * 1024)
        {
            return;
        }

        var airline = await context.Airlines.FindAsync(airlineId);
        if (airline is null) return;
        var folder = Path.Combine(environment.WebRootPath, "images", "airlines", "uploads");
        Directory.CreateDirectory(folder);
        var fileName = $"{airline.AirlineCode.ToLowerInvariant()}-{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
        await image.CopyToAsync(stream);
        airline.LogoUrl = $"/images/airlines/uploads/{fileName}";
    }

    private async Task SaveFlightImageAsync(Flight flight, IFormFile? image)
    {
        if (image is null || image.Length == 0) return;
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension) || image.Length > 5 * 1024 * 1024) return;
        var folder = Path.Combine(environment.WebRootPath, "images", "flights");
        Directory.CreateDirectory(folder);
        var fileName = $"flight-{flight.FlightNumber.ToLowerInvariant()}-{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
        await image.CopyToAsync(stream);
        flight.FlightLogoUrl = $"/images/flights/{fileName}";
        await context.SaveChangesAsync();
    }
}
