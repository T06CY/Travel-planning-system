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
        if (model.Guests.HasValue) query = query.Where(r => r.Capacity >= model.Guests.Value);
        if (model.MaxPrice.HasValue) query = query.Where(r => r.PricePerNight <= model.MaxPrice.Value);
        if (model.CheckIn.HasValue && model.CheckOut.HasValue && model.CheckOut > model.CheckIn)
            query = query.Where(r => r.TotalRooms > r.Reservations.Count(b => b.ReservationStatus != Models.HotelReservationStatus.Cancelled && b.CheckInDate < model.CheckOut && b.CheckOutDate > model.CheckIn));
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
}
