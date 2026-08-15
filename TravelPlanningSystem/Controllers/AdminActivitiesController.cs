using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

using Activity = TravelPlanningSystem.Models.Activity;

namespace TravelPlanningSystem.Controllers;

public class AdminActivitiesController(AppDbContext context, IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var query = context.Activities.AsNoTracking().Include(a => a.ActivityCategory).Include(a => a.Sessions).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(a => a.ActivityName.Contains(search) || a.Destination.Contains(search));
        if (categoryId.HasValue) query = query.Where(a => a.ActivityCategoryId == categoryId);
        ViewBag.Search = search; ViewBag.CategoryId = categoryId;
        ViewBag.Categories = new SelectList(await context.ActivityCategories.OrderBy(c => c.Name).ToListAsync(), "ActivityCategoryId", "Name", categoryId);
        return View(await query.OrderBy(a => a.ActivityName).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var activity = await context.Activities.AsNoTracking().Include(a => a.ActivityCategory).Include(a => a.Photos).Include(a => a.Sessions).Include(a => a.Reviews)
            .FirstOrDefaultAsync(a => a.ActivityId == id);
        return activity is null ? NotFound() : View(activity);
    }

    [HttpGet]
    public async Task<IActionResult> Create() { await LoadCategories(); return View(new ActivityFormViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityFormViewModel model)
    {
        if (await context.Activities.AnyAsync(a => a.ActivityName == model.ActivityName && a.Destination == model.Destination))
            ModelState.AddModelError(nameof(model.ActivityName), "An activity with the same name and destination already exists.");
        if (!ModelState.IsValid) { await LoadCategories(); return View(model); }
        var activity = Map(new Activity(), model);
        context.Activities.Add(activity);
        await context.SaveChangesAsync();
        await SavePhotos(activity.ActivityId, model.Photos);
        return RedirectToAction(nameof(Details), new { id = activity.ActivityId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var a = await context.Activities.FindAsync(id); if (a is null) return NotFound(); await LoadCategories();
        return View(new ActivityFormViewModel { ActivityId = a.ActivityId, ActivityName = a.ActivityName, Destination = a.Destination, Location = a.Location, Description = a.Description, PricePerPerson = a.PricePerPerson, DurationHours = a.DurationHours, MinimumParticipants = a.MinimumParticipants, MaximumParticipants = a.MaximumParticipants, MinimumAge = a.MinimumAge, IncludedItems = a.IncludedItems, WhatToBring = a.WhatToBring, ActivityCategoryId = a.ActivityCategoryId, IsFeatured = a.IsFeatured, IsActive = a.IsActive });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityFormViewModel model)
    {
        if (id != model.ActivityId) return BadRequest();
        var activity = await context.Activities.FindAsync(id); if (activity is null) return NotFound();
        if (await context.Activities.AnyAsync(a => a.ActivityId != id && a.ActivityName == model.ActivityName && a.Destination == model.Destination)) ModelState.AddModelError(nameof(model.ActivityName), "An activity with the same name and destination already exists.");
        if (!ModelState.IsValid) { await LoadCategories(); return View(model); }
        Map(activity, model); await context.SaveChangesAsync(); await SavePhotos(id, model.Photos);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id) => await context.Activities.AsNoTracking().Include(a => a.ActivityCategory).FirstOrDefaultAsync(a => a.ActivityId == id) is { } a ? View(a) : NotFound();

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var activity = await context.Activities.FindAsync(id); if (activity is null) return NotFound();
        var hasBookings = await context.ActivityBookings.AnyAsync(b => b.ActivitySession!.ActivityId == id);
        if (hasBookings) { activity.IsActive = false; await context.SaveChangesAsync(); TempData["Message"] = "Activity has bookings, so it was deactivated instead of deleted."; }
        else { context.Activities.Remove(activity); await context.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Sessions(int id)
    {
        var activity = await context.Activities.AsNoTracking().Include(a => a.Sessions.OrderBy(s => s.SessionDate).ThenBy(s => s.StartTime)).FirstOrDefaultAsync(a => a.ActivityId == id);
        return activity is null ? NotFound() : View(activity);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSession(int activityId, DateTime sessionDate, TimeSpan startTime, TimeSpan endTime, int capacity)
    {
        if (sessionDate.Date < DateTime.Today || startTime >= endTime || capacity < 1) { TempData["Error"] = "Enter a future date, valid times and capacity."; return RedirectToAction(nameof(Sessions), new { id = activityId }); }
        if (await context.ActivitySessions.AnyAsync(s => s.ActivityId == activityId && s.SessionDate.Date == sessionDate.Date && s.StartTime == startTime)) { TempData["Error"] = "A duplicate session already exists."; return RedirectToAction(nameof(Sessions), new { id = activityId }); }
        context.ActivitySessions.Add(new ActivitySession { ActivityId = activityId, SessionDate = sessionDate.Date, StartTime = startTime, EndTime = endTime, Capacity = capacity, AvailableSlots = capacity });
        await context.SaveChangesAsync(); return RedirectToAction(nameof(Sessions), new { id = activityId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSession(int id)
    { var s = await context.ActivitySessions.FindAsync(id); if (s is null) return NotFound(); s.IsActive = !s.IsActive; await context.SaveChangesAsync(); return RedirectToAction(nameof(Sessions), new { id = s.ActivityId }); }

    [HttpGet]
    public async Task<IActionResult> Photos(int id)
    { var activity = await context.Activities.AsNoTracking().Include(a => a.Photos.OrderBy(p => p.DisplayOrder)).FirstOrDefaultAsync(a => a.ActivityId == id); return activity is null ? NotFound() : View(activity); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhotos(int activityId, List<IFormFile> photos)
    { await SavePhotos(activityId, photos); return RedirectToAction(nameof(Photos), new { id = activityId }); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimaryPhoto(int id)
    { var photo = await context.ActivityPhotos.FindAsync(id); if (photo is null) return NotFound(); foreach (var p in await context.ActivityPhotos.Where(p => p.ActivityId == photo.ActivityId).ToListAsync()) p.IsPrimary = p.ActivityPhotoId == id; await context.SaveChangesAsync(); return RedirectToAction(nameof(Photos), new { id = photo.ActivityId }); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int id)
    { var photo = await context.ActivityPhotos.FindAsync(id); if (photo is null) return NotFound(); var activityId = photo.ActivityId; var physical = Path.Combine(environment.WebRootPath, photo.PhotoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)); if (System.IO.File.Exists(physical) && photo.PhotoUrl.Contains("/uploads/")) System.IO.File.Delete(physical); context.ActivityPhotos.Remove(photo); await context.SaveChangesAsync(); return RedirectToAction(nameof(Photos), new { id = activityId }); }

    private async Task LoadCategories() => ViewBag.Categories = new SelectList(await context.ActivityCategories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(), "ActivityCategoryId", "Name");
    private static Activity Map(Activity a, ActivityFormViewModel m) { a.ActivityName = m.ActivityName.Trim(); a.Destination = m.Destination.Trim(); a.Location = m.Location.Trim(); a.Description = m.Description.Trim(); a.PricePerPerson = m.PricePerPerson; a.DurationHours = m.DurationHours; a.MinimumParticipants = m.MinimumParticipants; a.MaximumParticipants = m.MaximumParticipants; a.MinimumAge = m.MinimumAge; a.IncludedItems = m.IncludedItems?.Trim(); a.WhatToBring = m.WhatToBring?.Trim(); a.ActivityCategoryId = m.ActivityCategoryId; a.IsFeatured = m.IsFeatured; a.IsActive = m.IsActive; return a; }
    private async Task SavePhotos(int activityId, IEnumerable<IFormFile> photos)
    {
        var list = photos.Where(p => p.Length > 0).ToList(); if (list.Count == 0) return;
        var folder = Path.Combine(environment.WebRootPath, "images", "activities", "uploads"); Directory.CreateDirectory(folder);
        var hasPrimary = await context.ActivityPhotos.AnyAsync(p => p.ActivityId == activityId && p.IsPrimary); var order = await context.ActivityPhotos.Where(p => p.ActivityId == activityId).CountAsync();
        foreach (var photo in list) { var ext = Path.GetExtension(photo.FileName).ToLowerInvariant(); if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext) || photo.Length > 5 * 1024 * 1024) continue; var file = $"{Guid.NewGuid():N}{ext}"; await using var stream = System.IO.File.Create(Path.Combine(folder, file)); await photo.CopyToAsync(stream); context.ActivityPhotos.Add(new ActivityPhoto { ActivityId = activityId, PhotoUrl = $"/images/activities/uploads/{file}", Caption = Path.GetFileNameWithoutExtension(photo.FileName), IsPrimary = !hasPrimary && order == 0, DisplayOrder = order++ }); }
        await context.SaveChangesAsync();
    }
}
