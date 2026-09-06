using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminRoomsController(AppDbContext context, IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var rooms = context.HotelRooms.AsNoTracking().Include(r => r.Photos).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) rooms = rooms.Where(r => r.HotelName.Contains(search) || r.RoomName.Contains(search) || r.Destination.Contains(search));
        ViewBag.Search = search;
        return View(await rooms.OrderBy(r => r.HotelName).ThenBy(r => r.RoomName).ToListAsync());
    }
    public async Task<IActionResult> Details(int id) => await context.HotelRooms.AsNoTracking().AsSplitQuery().Include(r => r.Photos).Include(r => r.Reservations).Include(r => r.Reviews).FirstOrDefaultAsync(r => r.HotelRoomId == id) is { } room ? View(room) : NotFound();
    [HttpGet] public IActionResult Create() => View(new HotelRoomFormViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HotelRoomFormViewModel model)
    {
        if (await context.HotelRooms.AnyAsync(r => r.HotelName == model.HotelName && r.RoomName == model.RoomName && r.Destination == model.Destination)) ModelState.AddModelError(nameof(model.RoomName), "This hotel room already exists.");
        if (!ModelState.IsValid) return View(model);
        var room = Map(new HotelRoom(), model);
        context.HotelRooms.Add(room);
        await context.SaveChangesAsync();
        await SavePhotos(room.HotelRoomId, model.Photos);
        TempData["Message"] = "Hotel room created successfully.";
        return RedirectToAction(nameof(Details), new { id = room.HotelRoomId });
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var room = await context.HotelRooms.FindAsync(id); if (room is null) return NotFound();
        return View(ToForm(room));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, HotelRoomFormViewModel model)
    {
        if (id != model.HotelRoomId) return BadRequest();
        var room = await context.HotelRooms.FindAsync(id);
        if (room is null) return NotFound();

        if (await context.HotelRooms.AnyAsync(r =>
            r.HotelRoomId != id &&
            r.HotelName == model.HotelName &&
            r.RoomName == model.RoomName &&
            r.Destination == model.Destination))
        {
            ModelState.AddModelError(nameof(model.RoomName), "This hotel room already exists.");
        }

        if (!ModelState.IsValid) return View(model);

        Map(room, model);
        await context.SaveChangesAsync();
        await SavePhotos(id, model.Photos);
        TempData["Message"] = "Hotel room updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpGet] public async Task<IActionResult> Delete(int id) => await context.HotelRooms.AsNoTracking().Include(r => r.Reservations).FirstOrDefaultAsync(r => r.HotelRoomId == id) is { } room ? View(room) : NotFound();
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var room = await context.HotelRooms.FindAsync(id); if (room is null) return NotFound();
        if (await context.HotelReservations.AnyAsync(b => b.HotelRoomId == id)) { room.IsActive = false; TempData["Message"] = "The room has reservation history, so it was deactivated."; } else context.HotelRooms.Remove(room);
        await context.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var room = await context.HotelRooms.FindAsync(id);
        if (room is null) return NotFound();

        room.IsActive = !room.IsActive;
        await context.SaveChangesAsync();
        TempData["Message"] = room.IsActive
            ? "Hotel room activated and visible in the public catalog."
            : "Hotel room deactivated and hidden from the public catalog.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet] public async Task<IActionResult> Photos(int id) => await context.HotelRooms.AsNoTracking().Include(r => r.Photos.OrderBy(p => p.DisplayOrder)).FirstOrDefaultAsync(r => r.HotelRoomId == id) is { } room ? View(room) : NotFound();
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> UploadPhotos(int hotelRoomId, List<IFormFile> photos) { await SavePhotos(hotelRoomId, photos); return RedirectToAction(nameof(Photos), new { id = hotelRoomId }); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimaryPhoto(int id)
    { var photo = await context.HotelRoomPhotos.FindAsync(id); if (photo is null) return NotFound(); foreach (var item in await context.HotelRoomPhotos.Where(p => p.HotelRoomId == photo.HotelRoomId).ToListAsync()) item.IsPrimary = item.HotelRoomPhotoId == id; await context.SaveChangesAsync(); return RedirectToAction(nameof(Photos), new { id = photo.HotelRoomId }); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id)
    { var photo = await context.HotelRoomPhotos.FindAsync(id); if (photo is null) return NotFound(); var roomId = photo.HotelRoomId; var file = Path.Combine(environment.WebRootPath, photo.PhotoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)); if (System.IO.File.Exists(file)) System.IO.File.Delete(file); context.HotelRoomPhotos.Remove(photo); await context.SaveChangesAsync(); return RedirectToAction(nameof(Photos), new { id = roomId }); }
    private static HotelRoom Map(HotelRoom r, HotelRoomFormViewModel m) { r.HotelName = m.HotelName.Trim(); r.RoomName = m.RoomName.Trim(); r.RoomType = m.RoomType; r.Destination = m.Destination.Trim(); r.Address = m.Address.Trim(); r.Description = m.Description.Trim(); r.PricePerNight = m.PricePerNight; r.Capacity = m.Capacity; r.TotalRooms = m.TotalRooms; r.StarRating = m.StarRating; r.Amenities = m.Amenities?.Trim(); r.IsFeatured = m.IsFeatured; r.IsActive = m.IsActive; return r; }
    private static HotelRoomFormViewModel ToForm(HotelRoom r) => new() { HotelRoomId = r.HotelRoomId, HotelName = r.HotelName, RoomName = r.RoomName, RoomType = r.RoomType, Destination = r.Destination, Address = r.Address, Description = r.Description, PricePerNight = r.PricePerNight, Capacity = r.Capacity, TotalRooms = r.TotalRooms, StarRating = r.StarRating, Amenities = r.Amenities, IsFeatured = r.IsFeatured, IsActive = r.IsActive };
    private async Task SavePhotos(int roomId, IEnumerable<IFormFile> photos)
    {
        var uploads = photos.Where(p => p.Length > 0).ToList(); if (uploads.Count == 0) return;
        var folder = Path.Combine(environment.WebRootPath, "images", "hotels", "uploads"); Directory.CreateDirectory(folder);
        var primaryExists = await context.HotelRoomPhotos.AnyAsync(p => p.HotelRoomId == roomId && p.IsPrimary); var order = await context.HotelRoomPhotos.CountAsync(p => p.HotelRoomId == roomId);
        foreach (var upload in uploads) { var ext = Path.GetExtension(upload.FileName).ToLowerInvariant(); if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext) || upload.Length > 5 * 1024 * 1024) continue; var name = $"{Guid.NewGuid():N}{ext}"; await using var stream = System.IO.File.Create(Path.Combine(folder, name)); await upload.CopyToAsync(stream); context.HotelRoomPhotos.Add(new HotelRoomPhoto { HotelRoomId = roomId, PhotoUrl = $"/images/hotels/uploads/{name}", Caption = Path.GetFileNameWithoutExtension(upload.FileName), IsPrimary = !primaryExists && order == 0, DisplayOrder = order++ }); }
        await context.SaveChangesAsync();
    }
}
