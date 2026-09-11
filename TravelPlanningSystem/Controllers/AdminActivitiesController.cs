using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

using Activity = TravelPlanningSystem.Models.Activity;

namespace TravelPlanningSystem.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminActivitiesController(
    AppDbContext context,
    IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId)
    {
        var query = context.Activities
            .AsNoTracking()
            .Include(a => a.ActivityCategory)
            .Include(a => a.Sessions)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.ActivityName.Contains(search) ||
                a.Destination.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(a =>
                a.ActivityCategoryId == categoryId);
        }

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;

        ViewBag.Categories = new SelectList(
            await context.ActivityCategories
                .OrderBy(c => c.Name)
                .ToListAsync(),
            "ActivityCategoryId",
            "Name",
            categoryId);

        return View(
            await query
                .OrderBy(a => a.ActivityName)
                .ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var activity = await context.Activities
            .AsNoTracking()
            .Include(a => a.ActivityCategory)
            .Include(a => a.Photos)
            .Include(a => a.Reviews)
            .Include(a => a.Sessions)
                .ThenInclude(s => s.Bookings)
            .FirstOrDefaultAsync(a =>
                a.ActivityId == id);

        if (activity == null)
            return NotFound();

        return View(activity);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadCategories();

        return View(
            new ActivityFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ActivityFormViewModel model)
    {
        var duplicate =
            await context.Activities
                .AnyAsync(a =>
                    a.ActivityName ==
                        model.ActivityName &&
                    a.Destination ==
                        model.Destination);

        if (duplicate)
        {
            ModelState.AddModelError(
                nameof(model.ActivityName),
                "An activity with the same name and destination already exists.");
        }

        if (Convert.ToDouble(
                model.DurationHours) <= 0)
        {
            ModelState.AddModelError(
                nameof(model.DurationHours),
                "Duration must be greater than 0.");
        }

        if (model.MaximumParticipants <
            model.MinimumParticipants)
        {
            ModelState.AddModelError(
                nameof(model.MaximumParticipants),
                "Maximum participants cannot be lower than minimum participants.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCategories();

            return View(model);
        }

        var activity =
            Map(
                new Activity(),
                model);

        context.Activities.Add(
            activity);

        await context.SaveChangesAsync();


        var defaultStartTime =
            new TimeSpan(
                9,
                0,
                0);

        var duration =
            TimeSpan.FromHours(
                Convert.ToDouble(
                    activity.DurationHours));

        var defaultEndTime =
            defaultStartTime.Add(
                duration);

        var generatedSessions =
            new List<ActivitySession>();

        for (var i = 0;
             i < 6;
             i++)
        {
            var sessionDate =
                DateTime.Today
                    .AddDays(
                        1 + (i * 2));

            generatedSessions.Add(
                new ActivitySession
                {
                    ActivityId =
                        activity.ActivityId,

                    SessionDate =
                        sessionDate.Date,

                    StartTime =
                        defaultStartTime,

                    EndTime =
                        defaultEndTime,

                    Capacity =
                        activity.MaximumParticipants,

                    AvailableSlots =
                        activity.MaximumParticipants,

                    IsActive =
                        true
                });
        }

        await context.ActivitySessions
            .AddRangeAsync(
                generatedSessions);

        await context.SaveChangesAsync();


        var photos =
            model.Photos?
                .Where(p =>
                    p != null &&
                    p.Length > 0)
                .ToList()
            ?? new List<IFormFile>();

        if (photos.Any())
        {
            var result =
                await ReplaceActivityPhotos(
                    activity.ActivityId,
                    photos);

            if (!result.Success)
            {
                TempData["Error"] =
                    result.Message;
            }
        }


        TempData["Message"] =
            $"Activity created successfully. {generatedSessions.Count} upcoming sessions were generated automatically.";

        return RedirectToAction(
            nameof(Details),
            new
            {
                id =
                    activity.ActivityId
            });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var activity =
            await context.Activities
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a =>
                    a.ActivityId == id);

        if (activity == null)
            return NotFound();

        await LoadCategories();

        var primaryPhoto =
            activity.Photos
                .OrderByDescending(p =>
                    p.IsPrimary)
                .ThenBy(p =>
                    p.DisplayOrder)
                .FirstOrDefault();

        ViewBag.ExistingPhotoUrl =
            primaryPhoto?.PhotoUrl;

        ViewBag.ExistingPhotoCount =
            activity.Photos.Count;

        var model =
            new ActivityFormViewModel
            {
                ActivityId =
                    activity.ActivityId,

                ActivityName =
                    activity.ActivityName,

                Destination =
                    activity.Destination,

                Location =
                    activity.Location,

                Description =
                    activity.Description,

                PricePerPerson =
                    activity.PricePerPerson,

                DurationHours =
                    activity.DurationHours,

                MinimumParticipants =
                    activity.MinimumParticipants,

                MaximumParticipants =
                    activity.MaximumParticipants,

                MinimumAge =
                    activity.MinimumAge,

                IncludedItems =
                    activity.IncludedItems,

                WhatToBring =
                    activity.WhatToBring,

                ActivityCategoryId =
                    activity.ActivityCategoryId,

                IsFeatured =
                    activity.IsFeatured,

                IsActive =
                    activity.IsActive
            };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        ActivityFormViewModel model)
    {
        if (id != model.ActivityId)
            return BadRequest();

        var activity =
            await context.Activities
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a =>
                    a.ActivityId == id);

        if (activity == null)
            return NotFound();

        var duplicate =
            await context.Activities
                .AnyAsync(a =>
                    a.ActivityId != id &&
                    a.ActivityName ==
                        model.ActivityName &&
                    a.Destination ==
                        model.Destination);

        if (duplicate)
        {
            ModelState.AddModelError(
                nameof(model.ActivityName),
                "An activity with the same name and destination already exists.");
        }

        if (Convert.ToDouble(
                model.DurationHours) <= 0)
        {
            ModelState.AddModelError(
                nameof(model.DurationHours),
                "Duration must be greater than 0.");
        }

        if (model.MaximumParticipants <
            model.MinimumParticipants)
        {
            ModelState.AddModelError(
                nameof(model.MaximumParticipants),
                "Maximum participants cannot be lower than minimum participants.");
        }

        var futureSessions =
            await context.ActivitySessions
                .Include(s => s.Bookings)
                .Where(s =>
                    s.ActivityId == id &&
                    s.SessionDate.Date >=
                        DateTime.Today)
                .ToListAsync();

        foreach (var session in
                 futureSessions)
        {
            var bookedParticipants =
                session.Bookings
                    .Where(b =>
                        b.BookingStatus !=
                        ActivityBookingStatus.Cancelled)
                    .Sum(b =>
                        b.ParticipantCount);

            if (model.MaximumParticipants <
                bookedParticipants)
            {
                ModelState.AddModelError(
                    nameof(model.MaximumParticipants),
                    $"Maximum participants cannot be lower than {bookedParticipants} because a future session already has that many booked participants.");

                break;
            }
        }

        var oldDuration =
            Convert.ToDouble(
                activity.DurationHours);

        var newDuration =
            Convert.ToDouble(
                model.DurationHours);

        var durationChanged =
            Math.Abs(
                oldDuration -
                newDuration) >
            0.0001;

        if (durationChanged &&
            ModelState.IsValid)
        {
            var newDurationTime =
                TimeSpan.FromHours(
                    newDuration);

            var activeSessions =
                futureSessions
                    .Where(s =>
                        s.IsActive)
                    .OrderBy(s =>
                        s.SessionDate)
                    .ThenBy(s =>
                        s.StartTime)
                    .ToList();

            foreach (var session in
                     activeSessions)
            {
                var newEndTime =
                    session.StartTime.Add(
                        newDurationTime);

                var conflict =
                    activeSessions.Any(other =>
                        other.ActivitySessionId !=
                            session.ActivitySessionId &&
                        other.SessionDate.Date ==
                            session.SessionDate.Date &&
                        session.StartTime <
                            other.StartTime.Add(
                                newDurationTime) &&
                        newEndTime >
                            other.StartTime);

                if (conflict)
                {
                    ModelState.AddModelError(
                        nameof(model.DurationHours),
                        "Changing this duration would cause future sessions to overlap.");

                    break;
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadCategories();

            var primaryPhoto =
                activity.Photos
                    .OrderByDescending(p =>
                        p.IsPrimary)
                    .ThenBy(p =>
                        p.DisplayOrder)
                    .FirstOrDefault();

            ViewBag.ExistingPhotoUrl =
                primaryPhoto?.PhotoUrl;

            ViewBag.ExistingPhotoCount =
                activity.Photos.Count;

            return View(model);
        }

        Map(
            activity,
            model);

        if (durationChanged)
        {
            var durationTime =
                TimeSpan.FromHours(
                    newDuration);

            foreach (var session in
                     futureSessions)
            {
                session.EndTime =
                    session.StartTime.Add(
                        durationTime);
            }
        }

        await context.SaveChangesAsync();

        var newPhotos =
            model.Photos?
                .Where(p =>
                    p != null &&
                    p.Length > 0)
                .ToList()
            ?? new List<IFormFile>();

        if (newPhotos.Any())
        {
            var result =
                await ReplaceActivityPhotos(
                    id,
                    newPhotos);

            if (!result.Success)
            {
                TempData["Error"] =
                    result.Message;

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }
        }

        if (durationChanged &&
            newPhotos.Any())
        {
            TempData["Message"] =
                "Activity updated successfully. Future session times and activity photos were updated.";
        }
        else if (durationChanged)
        {
            TempData["Message"] =
                "Activity updated successfully. Future session end times were recalculated automatically.";
        }
        else if (newPhotos.Any())
        {
            TempData["Message"] =
                "Activity updated successfully. The activity photo gallery was replaced.";
        }
        else
        {
            TempData["Message"] =
                "Activity updated successfully.";
        }

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(
        int id)
    {
        var activity =
            await context.Activities
                .AsNoTracking()
                .Include(a =>
                    a.ActivityCategory)
                .FirstOrDefaultAsync(a =>
                    a.ActivityId == id);

        if (activity == null)
            return NotFound();

        return View(activity);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        DeleteConfirmed(int id)
    {
        var activity =
            await context.Activities
                .FindAsync(id);

        if (activity == null)
            return NotFound();

        var hasBookings =
            await context.ActivityBookings
                .AnyAsync(b =>
                    b.ActivitySession != null &&
                    b.ActivitySession.ActivityId ==
                        id);

        if (hasBookings)
        {
            activity.IsActive =
                false;

            await context.SaveChangesAsync();

            TempData["Message"] =
                "This activity has booking history, so it was deactivated instead of permanently deleted.";
        }
        else
        {
            context.Activities.Remove(
                activity);

            await context.SaveChangesAsync();

            TempData["Message"] =
                "Activity deleted successfully.";
        }

        return RedirectToAction(
            nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Sessions(
        int id,
        string? filter = "upcoming")
    {
        var activity =
            await context.Activities
                .AsNoTracking()
                .Include(a => a.Sessions)
                    .ThenInclude(s =>
                        s.Bookings)
                .FirstOrDefaultAsync(a =>
                    a.ActivityId == id);

        if (activity == null)
            return NotFound();

        var sessions =
            activity.Sessions
                .OrderBy(s =>
                    s.SessionDate)
                .ThenBy(s =>
                    s.StartTime)
                .AsEnumerable();

        filter =
            string.IsNullOrWhiteSpace(filter)
                ? "upcoming"
                : filter.ToLower();

        sessions =
            filter switch
            {
                "today" =>
                    sessions.Where(s =>
                        s.SessionDate.Date ==
                        DateTime.Today),

                "past" =>
                    sessions.Where(s =>
                        s.SessionDate.Date <
                        DateTime.Today),

                "all" =>
                    sessions,

                _ =>
                    sessions.Where(s =>
                        s.SessionDate.Date >=
                        DateTime.Today)
            };

        activity.Sessions =
            sessions.ToList();

        ViewBag.Filter =
            filter;

        return View(activity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSession(
        int activityId,
        DateTime sessionDate,
        TimeSpan startTime,
        int capacity)
    {
        var activity =
            await context.Activities
                .FirstOrDefaultAsync(a =>
                    a.ActivityId ==
                    activityId);

        if (activity == null)
            return NotFound();

        if (sessionDate.Date <
            DateTime.Today)
        {
            TempData["Error"] =
                "Session date cannot be in the past.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activityId
                });
        }

        if (capacity <
            activity.MinimumParticipants)
        {
            TempData["Error"] =
                $"Capacity must be at least {activity.MinimumParticipants}.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activityId
                });
        }

        if (capacity >
            activity.MaximumParticipants)
        {
            TempData["Error"] =
                $"Capacity cannot exceed {activity.MaximumParticipants}.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activityId
                });
        }

        var duration =
            TimeSpan.FromHours(
                Convert.ToDouble(
                    activity.DurationHours));

        var endTime =
            startTime.Add(
                duration);

        var existingSessions =
            await context.ActivitySessions
                .Where(s =>
                    s.ActivityId ==
                        activityId &&
                    s.SessionDate.Date ==
                        sessionDate.Date &&
                    s.IsActive)
                .ToListAsync();

        var hasOverlap =
            existingSessions.Any(s =>
                startTime <
                    s.EndTime &&
                endTime >
                    s.StartTime);

        if (hasOverlap)
        {
            TempData["Error"] =
                "This session overlaps with another active session.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activityId
                });
        }

        var session =
            new ActivitySession
            {
                ActivityId =
                    activityId,

                SessionDate =
                    sessionDate.Date,

                StartTime =
                    startTime,

                EndTime =
                    endTime,

                Capacity =
                    capacity,

                AvailableSlots =
                    capacity,

                IsActive =
                    true
            };

        context.ActivitySessions.Add(
            session);

        await context.SaveChangesAsync();

        TempData["Message"] =
            $"Session created successfully. End time was automatically calculated as {DateTime.Today.Add(endTime):h:mm tt}.";

        return RedirectToAction(
            nameof(Sessions),
            new
            {
                id =
                    activityId
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSession(
        int sessionId,
        DateTime sessionDate,
        TimeSpan startTime,
        int capacity)
    {
        var session =
            await context.ActivitySessions
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s =>
                    s.ActivitySessionId ==
                    sessionId);

        if (session == null)
            return NotFound();

        var activity =
            await context.Activities
                .FirstOrDefaultAsync(a =>
                    a.ActivityId ==
                    session.ActivityId);

        if (activity == null)
            return NotFound();

        if (sessionDate.Date <
            DateTime.Today)
        {
            TempData["Error"] =
                "Session date cannot be in the past.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activity.ActivityId
                });
        }

        var bookedParticipants =
            session.Bookings
                .Where(b =>
                    b.BookingStatus !=
                    ActivityBookingStatus.Cancelled)
                .Sum(b =>
                    b.ParticipantCount);

        if (capacity <
            bookedParticipants)
        {
            TempData["Error"] =
                $"Capacity cannot be lower than {bookedParticipants} because those participants are already booked.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activity.ActivityId
                });
        }

        if (capacity <
            activity.MinimumParticipants ||
            capacity >
            activity.MaximumParticipants)
        {
            TempData["Error"] =
                $"Capacity must be between {activity.MinimumParticipants} and {activity.MaximumParticipants}.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activity.ActivityId
                });
        }

        var duration =
            TimeSpan.FromHours(
                Convert.ToDouble(
                    activity.DurationHours));

        var endTime =
            startTime.Add(
                duration);

        var otherSessions =
            await context.ActivitySessions
                .Where(s =>
                    s.ActivityId ==
                        activity.ActivityId &&
                    s.ActivitySessionId !=
                        sessionId &&
                    s.SessionDate.Date ==
                        sessionDate.Date &&
                    s.IsActive)
                .ToListAsync();

        var hasOverlap =
            otherSessions.Any(s =>
                startTime <
                    s.EndTime &&
                endTime >
                    s.StartTime);

        if (hasOverlap)
        {
            TempData["Error"] =
                "The updated session overlaps with another active session.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activity.ActivityId
                });
        }

        session.SessionDate =
            sessionDate.Date;

        session.StartTime =
            startTime;

        session.EndTime =
            endTime;

        session.Capacity =
            capacity;

        session.AvailableSlots =
            capacity -
            bookedParticipants;

        await context.SaveChangesAsync();

        TempData["Message"] =
            "Session updated successfully.";

        return RedirectToAction(
            nameof(Sessions),
            new
            {
                id =
                    activity.ActivityId
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        DuplicateSession(
            int sessionId,
            DateTime newDate)
    {
        var session =
            await context.ActivitySessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s =>
                    s.ActivitySessionId ==
                    sessionId);

        if (session == null)
            return NotFound();

        var activity =
            await context.Activities
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.ActivityId ==
                    session.ActivityId);

        if (activity == null)
            return NotFound();

        if (newDate.Date <
            DateTime.Today)
        {
            TempData["Error"] =
                "The new session date cannot be in the past.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activity.ActivityId
                });
        }

        var duration =
            TimeSpan.FromHours(
                Convert.ToDouble(
                    activity.DurationHours));

        var endTime =
            session.StartTime.Add(
                duration);

        var existingSessions =
            await context.ActivitySessions
                .Where(s =>
                    s.ActivityId ==
                        activity.ActivityId &&
                    s.SessionDate.Date ==
                        newDate.Date &&
                    s.IsActive)
                .ToListAsync();

        var overlap =
            existingSessions.Any(s =>
                session.StartTime <
                    s.EndTime &&
                endTime >
                    s.StartTime);

        if (overlap)
        {
            TempData["Error"] =
                "An overlapping session already exists on the selected date.";

            return RedirectToAction(
                nameof(Sessions),
                new
                {
                    id =
                        activity.ActivityId
                });
        }

        context.ActivitySessions.Add(
            new ActivitySession
            {
                ActivityId =
                    activity.ActivityId,

                SessionDate =
                    newDate.Date,

                StartTime =
                    session.StartTime,

                EndTime =
                    endTime,

                Capacity =
                    session.Capacity,

                AvailableSlots =
                    session.Capacity,

                IsActive =
                    true
            });

        await context.SaveChangesAsync();

        TempData["Message"] =
            "Session duplicated successfully.";

        return RedirectToAction(
            nameof(Sessions),
            new
            {
                id =
                    activity.ActivityId
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        DeleteSession(int id)
    {
        var session =
            await context.ActivitySessions
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s =>
                    s.ActivitySessionId ==
                    id);

        if (session == null)
            return NotFound();

        var activityId =
            session.ActivityId;

        if (session.Bookings.Any())
        {
            session.IsActive =
                false;

            TempData["Message"] =
                "This session has booking history, so it was deactivated instead of permanently deleted.";
        }
        else
        {
            context.ActivitySessions.Remove(
                session);

            TempData["Message"] =
                "Session deleted successfully.";
        }

        await context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Sessions),
            new
            {
                id =
                    activityId
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        ToggleSession(int id)
    {
        var session =
            await context.ActivitySessions
                .FindAsync(id);

        if (session == null)
            return NotFound();

        session.IsActive =
            !session.IsActive;

        await context.SaveChangesAsync();

        TempData["Message"] =
            session.IsActive
                ? "Session activated successfully."
                : "Session deactivated successfully.";

        return RedirectToAction(
            nameof(Sessions),
            new
            {
                id =
                    session.ActivityId
            });
    }

    [HttpGet]
    public async Task<IActionResult> Photos(
        int id)
    {
        var activity =
            await context.Activities
                .AsNoTracking()
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a =>
                    a.ActivityId ==
                    id);

        if (activity == null)
            return NotFound();

        return View(activity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhotos(
        int activityId,
        List<IFormFile> photos)
    {
        var selectedPhotos =
            photos?
                .Where(p =>
                    p != null &&
                    p.Length > 0)
                .ToList()
            ?? new List<IFormFile>();

        if (!selectedPhotos.Any())
        {
            TempData["Error"] =
                "Please select at least one photo.";

            return RedirectToAction(
                nameof(Photos),
                new
                {
                    id =
                        activityId
                });
        }

        var result =
            await ReplaceActivityPhotos(
                activityId,
                selectedPhotos);

        if (result.Success)
        {
            TempData["Message"] =
                selectedPhotos.Count == 1
                    ? "Activity photo replaced successfully."
                    : $"Activity gallery replaced successfully with {selectedPhotos.Count} photos.";
        }
        else
        {
            TempData["Error"] =
                result.Message;
        }

        return RedirectToAction(
            nameof(Photos),
            new
            {
                id =
                    activityId
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        SetPrimaryPhoto(int id)
    {
        var photo =
            await context.ActivityPhotos
                .FindAsync(id);

        if (photo == null)
            return NotFound();

        var photos =
            await context.ActivityPhotos
                .Where(p =>
                    p.ActivityId ==
                    photo.ActivityId)
                .ToListAsync();

        foreach (var item in photos)
        {
            item.IsPrimary =
                item.ActivityPhotoId ==
                photo.ActivityPhotoId;
        }

        await context.SaveChangesAsync();

        TempData["Message"] =
            "Primary photo updated successfully.";

        return RedirectToAction(
            nameof(Photos),
            new
            {
                id =
                    photo.ActivityId
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        DeletePhoto(int id)
    {
        var photo =
            await context.ActivityPhotos
                .FindAsync(id);

        if (photo == null)
            return NotFound();

        var activityId =
            photo.ActivityId;

        var wasPrimary =
            photo.IsPrimary;

        string? physicalPath =
            null;

        if (!string.IsNullOrWhiteSpace(
                photo.PhotoUrl) &&
            photo.PhotoUrl.Contains(
                "/images/activities/uploads/",
                StringComparison.OrdinalIgnoreCase))
        {
            physicalPath =
                Path.Combine(
                    environment.WebRootPath,
                    photo.PhotoUrl
                        .TrimStart('/')
                        .Replace(
                            '/',
                            Path.DirectorySeparatorChar));
        }

        context.ActivityPhotos.Remove(
            photo);

        await context.SaveChangesAsync();

        if (wasPrimary)
        {
            var replacement =
                await context.ActivityPhotos
                    .Where(p =>
                        p.ActivityId ==
                            activityId)
                    .OrderBy(p =>
                        p.DisplayOrder)
                    .FirstOrDefaultAsync();

            if (replacement != null)
            {
                replacement.IsPrimary =
                    true;

                await context.SaveChangesAsync();
            }
        }

        if (!string.IsNullOrWhiteSpace(
                physicalPath) &&
            System.IO.File.Exists(
                physicalPath))
        {
            try
            {
                System.IO.File.Delete(
                    physicalPath);
            }
            catch
            {
            }
        }

        TempData["Message"] =
            "Photo deleted successfully.";

        return RedirectToAction(
            nameof(Photos),
            new
            {
                id =
                    activityId
            });
    }

    private async Task LoadCategories()
    {
        ViewBag.Categories =
            new SelectList(
                await context.ActivityCategories
                    .Where(c =>
                        c.IsActive)
                    .OrderBy(c =>
                        c.Name)
                    .ToListAsync(),
                "ActivityCategoryId",
                "Name");
    }

    private static Activity Map(
        Activity activity,
        ActivityFormViewModel model)
    {
        activity.ActivityName =
            model.ActivityName.Trim();

        activity.Destination =
            model.Destination.Trim();

        activity.Location =
            model.Location.Trim();

        activity.Description =
            model.Description.Trim();

        activity.PricePerPerson =
            model.PricePerPerson;

        activity.DurationHours =
            model.DurationHours;

        activity.MinimumParticipants =
            model.MinimumParticipants;

        activity.MaximumParticipants =
            model.MaximumParticipants;

        activity.MinimumAge =
            model.MinimumAge;

        activity.IncludedItems =
            model.IncludedItems?.Trim();

        activity.WhatToBring =
            model.WhatToBring?.Trim();

        activity.ActivityCategoryId =
            model.ActivityCategoryId;

        activity.IsFeatured =
            model.IsFeatured;

        activity.IsActive =
            model.IsActive;

        return activity;
    }

    private async Task<(bool Success, string Message)>
        ReplaceActivityPhotos(
            int activityId,
            List<IFormFile> photos)
    {
        var activity =
            await context.Activities
                .Include(a => a.Photos)
                .FirstOrDefaultAsync(a =>
                    a.ActivityId ==
                    activityId);

        if (activity == null)
        {
            return (
                false,
                "Activity could not be found.");
        }

        var selectedPhotos =
            photos
                .Where(p =>
                    p != null &&
                    p.Length > 0)
                .ToList();

        if (!selectedPhotos.Any())
        {
            return (
                false,
                "Please select at least one photo.");
        }

        var allowedExtensions =
            new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

        foreach (var photo in
                 selectedPhotos)
        {
            var extension =
                Path.GetExtension(
                        photo.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(
                    extension))
            {
                return (
                    false,
                    $"'{photo.FileName}' is not supported. Please use JPG, JPEG, PNG or WEBP.");
            }

            if (photo.Length >
                5 * 1024 * 1024)
            {
                return (
                    false,
                    $"'{photo.FileName}' is larger than 5 MB.");
            }
        }

        var folder =
            Path.Combine(
                environment.WebRootPath,
                "images",
                "activities",
                "uploads");

        Directory.CreateDirectory(
            folder);

        var newPhotoRecords =
            new List<ActivityPhoto>();

        var newPhysicalFiles =
            new List<string>();

        try
        {
            for (var i = 0;
                 i < selectedPhotos.Count;
                 i++)
            {
                var photo =
                    selectedPhotos[i];

                var extension =
                    Path.GetExtension(
                            photo.FileName)
                        .ToLowerInvariant();

                var fileName =
                    $"{Guid.NewGuid():N}{extension}";

                var physicalPath =
                    Path.Combine(
                        folder,
                        fileName);

                await using (
                    var stream =
                        new FileStream(
                            physicalPath,
                            FileMode.Create,
                            FileAccess.Write))
                {
                    await photo.CopyToAsync(
                        stream);
                }

                newPhysicalFiles.Add(
                    physicalPath);

                newPhotoRecords.Add(
                    new ActivityPhoto
                    {
                        ActivityId =
                            activityId,

                        PhotoUrl =
                            $"/images/activities/uploads/{fileName}",

                        Caption =
                            Path.GetFileNameWithoutExtension(
                                photo.FileName),

                        IsPrimary =
                            i == 0,

                        DisplayOrder =
                            i
                    });
            }

            var oldPhotos =
                activity.Photos
                    .ToList();

            if (oldPhotos.Any())
            {
                context.ActivityPhotos
                    .RemoveRange(
                        oldPhotos);
            }

            await context.ActivityPhotos
                .AddRangeAsync(
                    newPhotoRecords);

            await context.SaveChangesAsync();

            foreach (var oldPhoto in
                     oldPhotos)
            {
                if (string.IsNullOrWhiteSpace(
                        oldPhoto.PhotoUrl))
                {
                    continue;
                }

                if (!oldPhoto.PhotoUrl.Contains(
                        "/images/activities/uploads/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var oldPhysicalPath =
                    Path.Combine(
                        environment.WebRootPath,
                        oldPhoto.PhotoUrl
                            .TrimStart('/')
                            .Replace(
                                '/',
                                Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(
                        oldPhysicalPath))
                {
                    try
                    {
                        System.IO.File.Delete(
                            oldPhysicalPath);
                    }
                    catch
                    {
                    }
                }
            }

            return (
                true,
                "Photos replaced successfully.");
        }
        catch
        {
            foreach (var file in
                     newPhysicalFiles)
            {
                if (System.IO.File.Exists(
                        file))
                {
                    try
                    {
                        System.IO.File.Delete(
                            file);
                    }
                    catch
                    {
                    }
                }
            }

            return (
                false,
                "The new photos could not be saved.");
        }
    }
}