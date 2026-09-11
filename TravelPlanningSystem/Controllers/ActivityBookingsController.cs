using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class ActivityBookingsController(
    AppDbContext context) : Controller
{
    private int CurrentUserId => 1;


    [HttpGet]
    public async Task<IActionResult> Create(
        int sessionId)
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

        return View(
            new ActivityBookingViewModel
            {
                ActivityId =
                    session.ActivityId,

                ActivitySessionId =
                    session.ActivitySessionId,

                ActivityName =
                    session.Activity.ActivityName,

                Destination =
                    session.Activity.Destination,

                PricePerPerson =
                    session.Activity.PricePerPerson,

                AvailableSlots =
                    session.AvailableSlots
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
                session.SessionDate <
                DateTime.Today)
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
            await context.Database
                .BeginTransactionAsync();


        try
        {
            session!.AvailableSlots -=
                model.ParticipantCount;


            decimal baseTotal =
                session.Activity!.PricePerPerson *
                model.ParticipantCount;


            decimal addonTotal = 0;


            if (model.HasInsurance)
            {
                addonTotal +=
                    5.00m *
                    model.ParticipantCount;
            }


            if (model.HasEquipmentRental)
            {
                addonTotal +=
                    15.00m *
                    model.ParticipantCount;
            }


            decimal discount = 0;


            if (!string.IsNullOrWhiteSpace(
                    model.PromoCode))
            {
                var code =
                    model.PromoCode
                        .Trim()
                        .ToUpperInvariant();


                if (code == "TRAVEL2026")
                {
                    discount =
                        Math.Round(
                            baseTotal * 0.15m,
                            2);
                }
                else if (code == "PROMO10")
                {
                    discount =
                        Math.Min(
                            baseTotal,
                            10.00m);
                }
            }


            decimal grandTotal =
                Math.Max(
                    0,
                    baseTotal +
                    addonTotal -
                    discount);


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
                        session.Activity.PricePerPerson,

                    TotalAmount =
                        grandTotal,

                    AddonFee =
                        addonTotal,

                    DiscountAmount =
                        discount,

                    PromoCode =
                        model.PromoCode,

                    PaymentMethod =
                        model.PaymentMethod,

                    PaymentStatus =
                        "Paid",

                    ContactName =
                        model.ContactName.Trim(),

                    ContactEmail =
                        model.ContactEmail.Trim(),

                    ContactPhone =
                        model.ContactPhone.Trim(),

                    BookingStatus =
                        ActivityBookingStatus.Confirmed,

                    BookingDate =
                        DateTime.Now
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
                .Include(b =>
                    b.ActivitySession)
                    .ThenInclude(s =>
                        s!.Activity)
                        .ThenInclude(a =>
                            a!.Photos)
                .Include(b =>
                    b.Review)
                .Where(b =>
                    b.UserId ==
                    CurrentUserId);


        if (
            Enum.TryParse<ActivityBookingStatus>(
                status,
                true,
                out var parsed)
        )
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
                        ActivityBookingStatus.Confirmed &&
                    booking.ActivitySession!
                        .SessionDate >
                        DateTime.Today,

                CanReview =
                    booking.BookingStatus ==
                        ActivityBookingStatus.Completed &&
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


        if (
            booking is null ||
            booking.BookingStatus !=
                ActivityBookingStatus.Confirmed ||
            booking.ActivitySession!
                .SessionDate <=
                DateTime.Today
        )
        {
            TempData["ErrorMessage"] =
                "This booking cannot be cancelled.";

            return RedirectToAction(
                nameof(MyBookings));
        }


        return View(booking);
    }


    [HttpPost]
    [ActionName("Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CancelConfirmed(
            int id,
            string cancellationReason)
    {
        var booking =
            await context.ActivityBookings
                .Include(b =>
                    b.ActivitySession)
                .FirstOrDefaultAsync(b =>
                    b.ActivityBookingId ==
                        id &&
                    b.UserId ==
                        CurrentUserId);


        if (
            booking?.ActivitySession is null ||
            booking.BookingStatus !=
                ActivityBookingStatus.Confirmed ||
            booking.ActivitySession
                .SessionDate <=
                DateTime.Today
        )
        {
            TempData["ErrorMessage"] =
                "This booking cannot be cancelled.";

            return RedirectToAction(
                nameof(MyBookings));
        }


        if (
            string.IsNullOrWhiteSpace(
                cancellationReason) ||
            cancellationReason.Length > 400
        )
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


        TempData["SuccessMessage"] =
            "Your booking has been cancelled successfully.";


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


        if (
            booking?.ActivitySession?.Activity
                is null
        )
        {
            return NotFound();
        }


        var session =
            booking.ActivitySession;


        var activity =
            session.Activity;


        var startTime =
            DateTime.Today
                .Add(session.StartTime)
                .ToString("h:mm tt");


        var endTime =
            DateTime.Today
                .Add(session.EndTime)
                .ToString("h:mm tt");


        var qrContent =
            $"TRAVELMATE ACTIVITY E-TICKET\n" +
            $"Booking Reference: {booking.BookingReference}\n" +
            $"Activity: {activity.ActivityName}\n" +
            $"Destination: {activity.Destination}\n" +
            $"Location: {activity.Location}\n" +
            $"Date: {session.SessionDate:dd MMM yyyy}\n" +
            $"Time: {startTime} - {endTime}\n" +
            $"Traveller: {booking.ContactName}\n" +
            $"Participants: {booking.ParticipantCount}\n" +
            $"Price Per Person: RM {booking.PricePerPerson:N2}\n" +
            $"Total Amount: RM {booking.TotalAmount:N2}\n" +
            $"Booking Status: {booking.BookingStatus}\n" +
            $"Payment Status: {booking.PaymentStatus}\n" +
            $"Payment Method: {booking.PaymentMethod}";


        using var qrData =
            QRCodeGenerator.GenerateQrCode(
                qrContent,
                QRCodeGenerator.ECCLevel.Q);


        using var qrCode =
            new PngByteQRCode(
                qrData);


        var qrBytes =
            qrCode.GetGraphic(
                20);


        ViewBag.QrCode =
            "data:image/png;base64," +
            Convert.ToBase64String(
                qrBytes);


        ViewBag.QrContent =
            qrContent;


        return View(
            booking);
    }


    [HttpGet]
    public async Task<IActionResult> Review(
        int bookingId)
    {
        var booking =
            await context.ActivityBookings
                .AsNoTracking()
                .Include(b =>
                    b.ActivitySession)
                    .ThenInclude(s =>
                        s!.Activity)
                .Include(b =>
                    b.Review)
                .FirstOrDefaultAsync(b =>
                    b.ActivityBookingId ==
                    bookingId);


        if (booking == null)
        {
            TempData["ErrorMessage"] =
                "The booking could not be found.";

            return RedirectToAction(
                nameof(MyBookings));
        }


        if (
            booking.BookingStatus !=
            ActivityBookingStatus.Completed
        )
        {
            TempData["ErrorMessage"] =
                "You can only review an activity after the booking has been completed.";

            return RedirectToAction(
                nameof(MyBookings));
        }


        if (
            booking.ActivitySession?.Activity
                is null
        )
        {
            TempData["ErrorMessage"] =
                "The activity information for this booking is unavailable.";

            return RedirectToAction(
                nameof(MyBookings));
        }


        if (booking.Review != null)
        {
            TempData["ErrorMessage"] =
                "You have already submitted a review for this booking.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id =
                        booking.ActivityBookingId
                });
        }


        return View(
            new ActivityReviewViewModel
            {
                ActivityBookingId =
                    booking.ActivityBookingId,

                ActivityName =
                    booking.ActivitySession
                        .Activity
                        .ActivityName,

                Rating =
                    0,

                Comment =
                    ""
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
                    model.ActivityBookingId);


        if (booking == null)
        {
            TempData["ErrorMessage"] =
                "The booking could not be found.";

            return RedirectToAction(
                nameof(MyBookings));
        }


        if (
            booking.BookingStatus !=
            ActivityBookingStatus.Completed
        )
        {
            TempData["ErrorMessage"] =
                "You can only review a completed activity.";

            return RedirectToAction(
                nameof(MyBookings));
        }


        if (
            booking.ActivitySession?.Activity
                is null
        )
        {
            TempData["ErrorMessage"] =
                "The activity information is unavailable.";

            return RedirectToAction(
                nameof(MyBookings));
        }


        if (booking.Review != null)
        {
            TempData["ErrorMessage"] =
                "You have already submitted a review for this booking.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id =
                        booking.ActivityBookingId
                });
        }


        model.ActivityName =
            booking.ActivitySession
                .Activity
                .ActivityName;


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var review =
            new ActivityReview
            {
                ActivityId =
                    booking.ActivitySession
                        .ActivityId,

                ActivityBookingId =
                    booking.ActivityBookingId,

                UserId =
                    booking.UserId,

                Rating =
                    model.Rating,

                Comment =
                    model.Comment.Trim(),

                CreatedAt =
                    DateTime.Now,

                IsVisible =
                    true
            };


        context.ActivityReviews.Add(
            review);


        await context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            "Your review has been submitted successfully.";


        return RedirectToAction(
            nameof(Details),
            new
            {
                id =
                    booking.ActivityBookingId
            });
    }


    private Task<ActivityBooking?>
        OwnedBooking(
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
                b.ActivityBookingId ==
                    id &&
                b.UserId ==
                    CurrentUserId);
    }
}