using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;
public class HotelController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(HotelSearchViewModel model)
    {
        model.Page = Math.Max(1, model.Page);
        var query = context.HotelRooms.AsNoTracking().AsSplitQuery().Include(r => r.Photos).Include(r => r.Reviews.Where(x => x.IsVisible)).Where(r => r.IsActive);
        if (!string.IsNullOrWhiteSpace(model.Search)) { var term = model.Search.Trim(); query = query.Where(r => r.HotelName.Contains(term) || r.RoomName.Contains(term) || r.Destination.Contains(term)); }
        if (!string.IsNullOrWhiteSpace(model.Destination)) query = query.Where(r => r.Destination == model.Destination);
        var guests = model.Guests ?? ((model.Adults ?? 0) + (model.Children ?? 0));
        if (guests > 0) query = query.Where(r => r.Capacity >= guests);
        if (model.MaxPrice.HasValue) query = query.Where(r => r.PricePerNight <= model.MaxPrice.Value / CurrencyRate(model.Currency));
        if (model.CheckIn.HasValue && model.CheckOut.HasValue)
        {
            var checkIn = model.CheckIn.Value.Date.Add(model.CheckInTime ?? new TimeSpan(15, 0, 0));
            var checkOut = model.CheckOut.Value.Date.Add(model.CheckOutTime ?? new TimeSpan(12, 0, 0));
            if (checkOut > checkIn)
                query = query.Where(r => r.TotalRooms > r.Reservations.Count(b => b.ReservationStatus != Models.HotelReservationStatus.Cancelled && b.CheckInDate.Add(b.CheckInTime) < checkOut && b.CheckOutDate.Add(b.CheckOutTime) > checkIn));
        }
        query = model.Sort switch { "price-low" => query.OrderBy(r => r.PricePerNight), "price-high" => query.OrderByDescending(r => r.PricePerNight), "rating" => query.OrderByDescending(r => r.Reviews.Any() ? r.Reviews.Average(x => x.Rating) : 0), "name" => query.OrderBy(r => r.HotelName).ThenBy(r => r.RoomName), _ => query.OrderByDescending(r => r.IsFeatured).ThenBy(r => r.HotelName) };
        model.TotalItems = await query.CountAsync();
        model.Rooms = await query.Skip((model.Page - 1) * model.PageSize).Take(model.PageSize).ToListAsync();
        model.Destinations = await context.HotelRooms.AsNoTracking().Where(r => r.IsActive).Select(r => r.Destination).Distinct().OrderBy(x => x).ToListAsync();
        return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ? PartialView("_HotelCards", model) : View(model);
    }
    public async Task<IActionResult> Details(int id)
    {
        var room = await context.HotelRooms.AsNoTracking().AsSplitQuery().Include(r => r.Photos).Include(r => r.Reviews.Where(x => x.IsVisible)).ThenInclude(x => x.HotelReservation).FirstOrDefaultAsync(r => r.HotelRoomId == id && r.IsActive);
        return room is null ? NotFound() : View(room);
    }

    private static decimal CurrencyRate(string? currency) => currency?.ToUpperInvariant() switch
    {
        "USD" => 0.22m,
        "SGD" => 0.29m,
        "EUR" => 0.20m,
        "GBP" => 0.17m,
        _ => 1m
    };
}
