using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class ActivityController(AppDbContext context) : Controller
{

    public async Task<IActionResult> Index(ActivitySearchViewModel model)
    {
        // Make sure page number is never less than 1
        model.Page = Math.Max(1, model.Page);

        var query = context.Activities
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.ActivityCategory)
            .Include(a => a.Photos)
            .Include(a => a.Sessions)
            .Include(a => a.Reviews.Where(r => r.IsVisible))
            .Where(a => a.IsActive);


        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var search = model.Search.Trim();

            query = query.Where(a =>
                a.ActivityName.Contains(search) ||
                a.Description.Contains(search) ||
                a.Destination.Contains(search));
        }


        if (!string.IsNullOrWhiteSpace(model.Destination))
        {
            query = query.Where(a =>
                a.Destination == model.Destination);
        }



        if (model.CategoryId.HasValue)
        {
            query = query.Where(a =>
                a.ActivityCategoryId == model.CategoryId.Value);
        }


        if (model.MinPrice.HasValue)
        {
            query = query.Where(a =>
                a.PricePerPerson >= model.MinPrice.Value);
        }


        if (model.MaxPrice.HasValue)
        {
            query = query.Where(a =>
                a.PricePerPerson <= model.MaxPrice.Value);
        }

        if (model.Date.HasValue)
        {
            var selectedDate = model.Date.Value.Date;
            var nextDate = selectedDate.AddDays(1);

            query = query.Where(a =>
                a.Sessions.Any(s =>
                    s.IsActive &&
                    s.SessionDate >= selectedDate &&
                    s.SessionDate < nextDate &&
                    s.AvailableSlots > 0));
        }


        query = model.Sort switch
        {
            "price-low" =>
                query
                    .OrderBy(a => a.PricePerPerson)
                    .ThenBy(a => a.ActivityId),

            "price-high" =>
                query
                    .OrderByDescending(a => a.PricePerPerson)
                    .ThenBy(a => a.ActivityId),

            "name" =>
                query
                    .OrderBy(a => a.ActivityName)
                    .ThenBy(a => a.ActivityId),

            "rating" =>
                query
                    .OrderByDescending(a =>
                        a.Reviews.Any()
                            ? a.Reviews.Average(r => r.Rating)
                            : 0)
                    .ThenBy(a => a.ActivityId),

            _ =>
                query
                    .OrderByDescending(a => a.IsFeatured)
                    .ThenBy(a => a.ActivityName)
                    .ThenBy(a => a.ActivityId)
        };

        model.TotalItems = await query.CountAsync();


        model.Activities = await query
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();

        model.Categories = await context.ActivityCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(
                c.Name,
                c.ActivityCategoryId.ToString()))
            .ToListAsync();


        model.Destinations = await context.Activities
            .AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => a.Destination)
            .Distinct()
            .OrderBy(x => x)
            .Select(x => new SelectListItem(
                x,
                x))
            .ToListAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return PartialView(
                "~/Views/Activity/_ActivityCards.cshtml",
                model);
        }


        return View(
            "~/Views/Activity/Index.cshtml",
            model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var today = DateTime.Today;

        var activity = await context.Activities
            .AsNoTracking()
            .AsSplitQuery()

            .Include(a => a.ActivityCategory)

            .Include(a => a.Photos)

            .Include(a => a.Sessions
                .Where(s =>
                    s.IsActive &&
                    s.SessionDate >= today &&
                    s.AvailableSlots > 0)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime))

            .Include(a => a.Reviews
                .Where(r => r.IsVisible))
            .ThenInclude(r => r.ActivityBooking)

            .FirstOrDefaultAsync(a =>
                a.ActivityId == id &&
                a.IsActive);


        if (activity == null)
        {
            return NotFound();
        }


        return View(activity);
    }
}