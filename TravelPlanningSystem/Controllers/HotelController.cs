using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class HotelController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(HotelSearchViewModel model)
    {
        model.Page = Math.Max(1, model.Page);

        var query = context.HotelRooms
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Photos)
            .Include(r => r.Reviews.Where(x => x.IsVisible))
            .Where(r => r.IsActive);

        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var term = model.Search.Trim();
            query = query.Where(r =>
                r.HotelName.Contains(term) ||
                r.RoomName.Contains(term) ||
                r.Destination.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(model.Destination))
            query = query.Where(r => r.Destination == model.Destination);

        var requestedRooms = Math.Max(1, model.RequestedRooms ?? 1);
        var guests = model.Guests ?? ((model.Adults ?? 0) + (model.Children ?? 0));

        if (guests > 0)
        {
            var guestsPerRoom = (int)Math.Ceiling(guests / (double)requestedRooms);
            query = query.Where(r => r.Capacity >= guestsPerRoom);
        }

        if (model.MaxPrice.HasValue)
            query = query.Where(r =>
                r.PricePerNight <= model.MaxPrice.Value / CurrencyRate(model.Currency));

        query = model.Sort switch
        {
            "price-low" => query.OrderBy(r => r.PricePerNight),
            "price-high" => query.OrderByDescending(r => r.PricePerNight),
            "rating" => query.OrderByDescending(r =>
                r.Reviews.Any() ? r.Reviews.Average(x => x.Rating) : 0),
            "name" => query.OrderBy(r => r.HotelName).ThenBy(r => r.RoomName),
            _ => query.OrderByDescending(r => r.IsFeatured).ThenBy(r => r.HotelName)
        };

        // Availability checking requires combining DateTime + TimeSpan.
        // EF Core SQL Server cannot translate DateTime.Add(TimeSpan), so the
        // exact overlap check is performed after the candidate rooms and their
        // reservations have been loaded into memory.
        if (model.CheckIn.HasValue && model.CheckOut.HasValue)
        {
            var checkInAt = model.CheckIn.Value.Date.Add(
                model.CheckInTime ?? new TimeSpan(15, 0, 0));

            var checkOutAt = model.CheckOut.Value.Date.Add(
                model.CheckOutTime ?? new TimeSpan(12, 0, 0));

            if (checkOutAt > checkInAt)
            {
                var candidates = await query
                    .Include(r => r.Reservations)
                    .ToListAsync();

                var availableRooms = candidates
                    .Where(room =>
                    {
                        var occupied = room.Reservations.Count(b =>
                        {
                            if (b.ReservationStatus == HotelReservationStatus.Cancelled)
                                return false;

                            var existingCheckIn = b.CheckInDate.Date.Add(b.CheckInTime);
                            var existingCheckOut = b.CheckOutDate.Date.Add(b.CheckOutTime);

                            return existingCheckIn < checkOutAt &&
                                   existingCheckOut > checkInAt;
                        });

                        return room.TotalRooms - occupied >= requestedRooms;
                    })
                    .ToList();

                model.TotalItems = availableRooms.Count;
                model.Rooms = availableRooms
                    .Skip((model.Page - 1) * model.PageSize)
                    .Take(model.PageSize)
                    .ToList();
            }
            else
            {
                model.TotalItems = await query.CountAsync();
                model.Rooms = await query
                    .Skip((model.Page - 1) * model.PageSize)
                    .Take(model.PageSize)
                    .ToListAsync();
            }
        }
        else
        {
            model.TotalItems = await query.CountAsync();
            model.Rooms = await query
                .Skip((model.Page - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();
        }

        model.Destinations = await context.HotelRooms
            .AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => r.Destination)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
            ? PartialView("_HotelCards", model)
            : View(model);
    }

    public async Task<IActionResult> Details(
        int id,
        DateTime? checkIn,
        DateTime? checkOut,
        TimeSpan? checkInTime,
        TimeSpan? checkOutTime)
    {
        var room = await context.HotelRooms
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Photos)
            .Include(r => r.Reviews.Where(x => x.IsVisible))
            .ThenInclude(x => x.HotelReservation)
            .ThenInclude(x => x!.ApplicationUser)
            .FirstOrDefaultAsync(r => r.HotelRoomId == id && r.IsActive);

        if (room is null)
            return NotFound();

        // Older demo reservations were created before ApplicationUserId was populated.
        // Resolve those reviews by the reservation contact email so they can still show
        // the member's name and profile picture.
        var guestEmails = room.Reviews
            .Where(r => r.HotelReservation?.ApplicationUser is null)
            .Select(r => r.HotelReservation?.ContactEmail)
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (guestEmails.Count > 0)
        {
            var guestUsers = await context.Users
                .AsNoTracking()
                .Where(u => guestEmails.Contains(u.Email))
                .ToListAsync();

            foreach (var review in room.Reviews)
            {
                var reservation = review.HotelReservation;
                if (reservation?.ApplicationUser is null)
                {
                    reservation!.ApplicationUser = guestUsers.FirstOrDefault(u =>
                        string.Equals(u.Email, reservation.ContactEmail, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        ViewData["CheckIn"] = checkIn;
        ViewData["CheckOut"] = checkOut;
        ViewData["CheckInTime"] = checkInTime;
        ViewData["CheckOutTime"] = checkOutTime;

        return View(room);
    }

    private static decimal CurrencyRate(string? currency)
        => currency?.ToUpperInvariant() switch
        {
            "USD" => 0.22m,
            "SGD" => 0.29m,
            "EUR" => 0.20m,
            "GBP" => 0.17m,
            _ => 1m
        };
}
