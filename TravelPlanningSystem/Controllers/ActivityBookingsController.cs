using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class ActivityBookingsController(AppDbContext context) : Controller
{
    private int CurrentUserId => 1;


    [HttpGet]
    public async Task<IActionResult> Create(int sessionId)
    {
        var session = await context.ActivitySessions
            .AsNoTracking()
            .Include(s => s.Activity)
            .FirstOrDefaultAsync(s =>
                s.ActivitySessionId == sessionId &&
                s.IsActive &&
                s.SessionDate >= DateTime.Today);

        if (session?.Activity is null ||
            session.AvailableSlots < 1)
        {
            return NotFound();
        }

        return View(new ActivityBookingViewModel
        {
            ActivityId = session.ActivityId,
            ActivitySessionId = session.ActivitySessionId,
            ActivityName = session.Activity.ActivityName,
            Destination = session.Activity.Destination,
            PricePerPerson = session.Activity.PricePerPerson,
            AvailableSlots = session.AvailableSlots
        });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ActivityBookingViewModel model)
    {
        var session = await context.ActivitySessions
            .Include(s => s.Activity)
            .FirstOrDefaultAsync(s =>
                s.ActivitySessionId ==
                model.ActivitySessionId);


        if (session?.Activity is null)
        {
            ModelState.AddModelError(
                "",
                "The selected session no longer exists.");
        }
        else
        {
            model.ActivityId =
                session.ActivityId;

            model.ActivityName =
                session.Activity.ActivityName;

            model.Destination =
                session.Activity.Destination;

            model.PricePerPerson =
                session.Activity.PricePerPerson;

            model.AvailableSlots =
                session.AvailableSlots;


            if (!session.IsActive ||
                session.SessionDate < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.ActivitySessionId),
                    "This session is no longer available.");
            }


            if (model.ParticipantCount <
                session.Activity.MinimumParticipants)
            {
                ModelState.AddModelError(
                    nameof(model.ParticipantCount),
                    $"At least {session.Activity.MinimumParticipants} participant(s) are required.");
            }


            if (model.ParticipantCount >
                session.AvailableSlots)
            {
                ModelState.AddModelError(
                    nameof(model.ParticipantCount),
                    $"Only {session.AvailableSlots} slot(s) remain.");
            }
        }


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        await using var transaction =
            await context.Database.BeginTransactionAsync();


        try
        {
            session!.AvailableSlots -=
                model.ParticipantCount;


            var booking =
                new ActivityBooking
                {
                    BookingReference =
                        $"ACT{DateTime.Now:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",

                    UserId =
                        CurrentUserId,

                    ActivitySessionId =
                        session.ActivitySessionId,

                    ParticipantCount =
                        model.ParticipantCount,

                    PricePerPerson =
                        session.Activity!.PricePerPerson,

                    TotalAmount =
                        session.Activity.PricePerPerson *
                        model.ParticipantCount,

                    ContactName =
                        model.ContactName.Trim(),

                    ContactEmail =
                        model.ContactEmail.Trim(),

                    ContactPhone =
                        model.ContactPhone.Trim(),

                    BookingStatus =
                        ActivityBookingStatus.Confirmed
                };


            context.ActivityBookings.Add(
                booking);


            await context.SaveChangesAsync();

            await transaction.CommitAsync();


            return RedirectToAction(
                nameof(Confirmation),
                new
                {
                    id =
                        booking.ActivityBookingId
                });
        }
        catch
        {
            await transaction.RollbackAsync();


            ModelState.AddModelError(
                "",
                "The booking could not be completed. Please try again.");


            return View(model);
        }
    }


    public async Task<IActionResult> Confirmation(
        int id)
    {
        var booking =
            await OwnedBooking(id);


        return booking is null
            ? NotFound()
            : View(booking);
    }

    public async Task<IActionResult> MyBookings(
        string? status)
    {
        var query =
            context.ActivityBookings
                .AsNoTracking()

                // Load session
                .Include(b =>
                    b.ActivitySession)

                // Load activity
                .ThenInclude(s =>
                    s!.Activity)

                // IMPORTANT:
                // Load the activity photos
                .ThenInclude(a =>
                    a!.Photos)

                .Where(b =>
                    b.UserId ==
                    CurrentUserId);


        // Filter by booking status
        if (Enum.TryParse<ActivityBookingStatus>(
            status,
            true,
            out var parsed))
        {
            query =
                query.Where(b =>
                    b.BookingStatus ==
                    parsed);
        }


        ViewBag.Status =
            status;


        var bookings =
            await query
                .OrderByDescending(b =>
                    b.BookingDate)
                .ToListAsync();


        return View(bookings);
    }

    public async Task<IActionResult> Details(
        int id)
    {
        var booking =
            await OwnedBooking(id);


        if (booking is null)
        {
            return NotFound();
        }


        return View(
            new ActivityBookingDetailsViewModel
            {
                Booking =
                    booking,

                CanCancel =
                    booking.BookingStatus ==
                    ActivityBookingStatus.Confirmed
                    &&
                    booking.ActivitySession!.SessionDate >
                    DateTime.Today,

                CanReview =
                    booking.BookingStatus ==
                    ActivityBookingStatus.Completed
                    &&
                    booking.Review is null,

                ExistingReview =
                    booking.Review
            });
    }


    [HttpGet]
    public async Task<IActionResult> Cancel(
        int id)
    {
        var booking =
            await OwnedBooking(id);


        if (booking is null ||
            booking.BookingStatus !=
            ActivityBookingStatus.Confirmed ||
            booking.ActivitySession!.SessionDate <=
            DateTime.Today)
        {
            return BadRequest();
        }


        return View(booking);
    }


    [HttpPost]
    [ActionName("Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelConfirmed(
        int id,
        string cancellationReason)
    {
        var booking =
            await context.ActivityBookings
                .Include(b =>
                    b.ActivitySession)

                .FirstOrDefaultAsync(b =>
                    b.ActivityBookingId == id &&
                    b.UserId ==
                    CurrentUserId);


        if (booking?.ActivitySession is null ||
            booking.BookingStatus !=
            ActivityBookingStatus.Confirmed ||
            booking.ActivitySession.SessionDate <=
            DateTime.Today)
        {
            return BadRequest();
        }


        if (string.IsNullOrWhiteSpace(
                cancellationReason)
            ||
            cancellationReason.Length > 400)
        {
            ModelState.AddModelError(
                "cancellationReason",
                "Please enter a cancellation reason (maximum 400 characters).");


            return View(
                "Cancel",
                booking);
        }


        booking.BookingStatus =
            ActivityBookingStatus.Cancelled;

        booking.CancellationReason =
            cancellationReason.Trim();

        booking.CancelledAt =
            DateTime.Now;


        booking.ActivitySession.AvailableSlots =
            Math.Min(
                booking.ActivitySession.Capacity,
                booking.ActivitySession.AvailableSlots +
                booking.ParticipantCount);


        await context.SaveChangesAsync();


        return RedirectToAction(
            nameof(Details),
            new
            {
                id
            });
    }


    public async Task<IActionResult> Ticket(
        int id)
    {
        var booking =
            await OwnedBooking(id);


        return booking is null
            ? NotFound()
            : View(booking);
    }


    [HttpGet]
    public async Task<IActionResult> Review(
        int bookingId)
    {
        var booking =
            await OwnedBooking(
                bookingId);


        if (booking is null ||
            booking.BookingStatus !=
            ActivityBookingStatus.Completed)
        {
            return BadRequest();
        }


        return View(
            new ActivityReviewViewModel
            {
                ActivityBookingId =
                    bookingId,

                ActivityReviewId =
                    booking.Review?.ActivityReviewId
                    ?? 0,

                ActivityName =
                    booking.ActivitySession!
                        .Activity!
                        .ActivityName,

                Rating =
                    booking.Review?.Rating
                    ?? 5,

                Comment =
                    booking.Review?.Comment
                    ?? ""
            });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(
        ActivityReviewViewModel model)
    {
        var booking =
            await context.ActivityBookings

                .Include(b =>
                    b.ActivitySession)

                .ThenInclude(s =>
                    s!.Activity)

                .Include(b =>
                    b.Review)

                .FirstOrDefaultAsync(b =>
                    b.ActivityBookingId ==
                    model.ActivityBookingId
                    &&
                    b.UserId ==
                    CurrentUserId);


        if (booking?.ActivitySession?.Activity is null ||
            booking.BookingStatus !=
            ActivityBookingStatus.Completed)
        {
            return BadRequest();
        }


        model.ActivityName =
            booking.ActivitySession
                .Activity
                .ActivityName;


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        if (booking.Review is null)
        {
            context.ActivityReviews.Add(
                new ActivityReview
                {
                    ActivityId =
                        booking.ActivitySession.ActivityId,

                    ActivityBookingId =
                        booking.ActivityBookingId,

                    UserId =
                        CurrentUserId,

                    Rating =
                        model.Rating,

                    Comment =
                        model.Comment.Trim()
                });
        }
        else
        {
            booking.Review.Rating =
                model.Rating;

            booking.Review.Comment =
                model.Comment.Trim();

            booking.Review.UpdatedAt =
                DateTime.Now;
        }


        await context.SaveChangesAsync();


        return RedirectToAction(
            nameof(Details),
            new
            {
                id =
                    booking.ActivityBookingId
            });
    }

    private Task<ActivityBooking?> OwnedBooking(
        int id)
    {
        return context.ActivityBookings
            .AsNoTracking()

            .Include(b =>
                b.ActivitySession)

            .ThenInclude(s =>
                s!.Activity)

            .ThenInclude(a =>
                a!.Photos)

            .Include(b =>
                b.Review)

            .FirstOrDefaultAsync(b =>
                b.ActivityBookingId == id &&
                b.UserId ==
                CurrentUserId);
    }
}