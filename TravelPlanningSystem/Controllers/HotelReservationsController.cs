using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

public class HotelReservationsController(AppDbContext context) : Controller
{
    private const decimal HotelSstRate = 0.10m;

    private Guid? CurrentAccountId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;

    private bool CurrentAccountIsStaff => User.HasClaim("AccountType", "Staff");

    [HttpGet]
    public async Task<IActionResult> Create(
        int roomId,
        DateTime? checkIn,
        DateTime? checkOut,
        TimeSpan? checkInTime,
        TimeSpan? checkOutTime)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Url.Action(nameof(Create), "HotelReservations", new { roomId, checkIn, checkOut });
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var accountId = CurrentAccountId;
        if (!accountId.HasValue)
            return Challenge();

        var room = await context.HotelRooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.HotelRoomId == roomId && r.IsActive);

        if (room is null)
            return NotFound();

        string contactName;
        string contactEmail;
        string contactPhone;

        if (CurrentAccountIsStaff)
        {
            var staff = await context.StaffUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StaffId == accountId.Value && s.Status == "Active");

            if (staff is null)
                return Challenge();

            contactName = $"{staff.FirstName} {staff.LastName}".Trim();
            contactEmail = staff.Email;
            contactPhone = staff.PhoneNumber ?? string.Empty;
        }
        else
        {
            var account = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == accountId.Value);

            if (account is null)
                return Challenge();

            contactName = $"{account.FirstName} {account.LastName}".Trim();
            contactEmail = account.Email;
            contactPhone = account.PhoneNumber ?? string.Empty;
        }

        return View(new HotelReservationViewModel
        {
            HotelRoomId = roomId,
            HotelName = room.HotelName,
            RoomName = room.RoomName,
            PricePerNight = room.PricePerNight,
            Capacity = room.Capacity,
            CheckInDate = checkIn?.Date >= DateTime.Today
                ? checkIn.Value.Date
                : DateTime.Today.AddDays(1),
            CheckOutDate = checkOut?.Date >= DateTime.Today
                ? checkOut.Value.Date
                : DateTime.Today.AddDays(2),
            CheckInTime = checkInTime ?? new TimeSpan(15, 0, 0),
            CheckOutTime = checkOutTime ?? new TimeSpan(12, 0, 0),
             BookingFor = HotelReservationViewModel.BookForSelf,
            ContactName = contactName,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone
        });
    }

    [HttpGet]
    public async Task<IActionResult> CalculatePrice(int roomId, DateTime checkInDate, DateTime checkOutDate, TimeSpan checkInTime, TimeSpan checkOutTime)
    {
        if (checkOutDate.Date.Add(checkOutTime) <= checkInDate.Date.Add(checkInTime))
            return BadRequest(new { message = "Check-out must be after check-in." });

        var room = await context.HotelRooms
            .AsNoTracking()
            .Where(r => r.HotelRoomId == roomId && r.IsActive)
            .Select(r => new { r.PricePerNight })
            .FirstOrDefaultAsync();

        if (room is null)
            return NotFound();

        var nights = Math.Max(1, (int)Math.Ceiling((checkOutDate.Date.Add(checkOutTime) - checkInDate.Date.Add(checkInTime)).TotalDays));
        return Json(new { nights, pricePerNight = room.PricePerNight, totalAmount = room.PricePerNight * nights });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HotelReservationViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            var returnUrl = Url.Action(nameof(Create), "HotelReservations", new
            {
                roomId = model.HotelRoomId,
                checkIn = model.CheckInDate,
                checkOut = model.CheckOutDate
            });

            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var accountId = CurrentAccountId;
        if (!accountId.HasValue)
            return Challenge();

        var room = await context.HotelRooms
            .FirstOrDefaultAsync(r => r.HotelRoomId == model.HotelRoomId && r.IsActive);

        if (room is null)
            return NotFound();

        model.HotelName = room.HotelName;
        model.RoomName = room.RoomName;
        model.PricePerNight = room.PricePerNight;
        model.Capacity = room.Capacity;

        var accountEmail = string.Empty;
        var accountName = string.Empty;
        var accountPhone = string.Empty;

        if (CurrentAccountIsStaff)
        {
            var staff = await context.StaffUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StaffId == accountId.Value && s.Status == "Active");

            if (staff is null)
                return Challenge();

            accountName = $"{staff.FirstName} {staff.LastName}".Trim();
            accountEmail = staff.Email;
            accountPhone = staff.PhoneNumber ?? string.Empty;
        }
        else
        {
            var account = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == accountId.Value);

            if (account is null)
                return Challenge();

            accountName = $"{account.FirstName} {account.LastName}".Trim();
            accountEmail = account.Email;
            accountPhone = account.PhoneNumber ?? string.Empty;
        }

        if (model.BookingFor == HotelReservationViewModel.BookForSelf)
        {
            model.ContactName = accountName;
            model.ContactEmail = accountEmail;
            model.ContactPhone = accountPhone;
        }
        else if (model.BookingFor == HotelReservationViewModel.BookForOthers)
        {
            model.ContactEmail = accountEmail;
        }
        else
        {
            ModelState.AddModelError(nameof(model.BookingFor), "Choose whether you are booking for yourself or someone else.");
        }

        model.ContactName = model.ContactName?.Trim() ?? string.Empty;
        model.ContactEmail = model.ContactEmail?.Trim() ?? string.Empty;
        model.ContactPhone = model.ContactPhone?.Trim() ?? string.Empty;

        if (model.BookingFor == HotelReservationViewModel.BookForOthers && string.IsNullOrWhiteSpace(model.ContactName))
            ModelState.AddModelError(nameof(model.ContactName), "Enter the name of the person staying.");

        if (string.IsNullOrWhiteSpace(model.ContactPhone))
            ModelState.AddModelError(nameof(model.ContactPhone), "Enter a phone number.");

        var checkInAt = model.CheckInDate.Date.Add(model.CheckInTime);
        var checkOutAt = model.CheckOutDate.Date.Add(model.CheckOutTime);

        if (model.CheckInDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(model.CheckInDate),
                "Check-in date cannot be in the past.");
        }

        if (checkOutAt <= checkInAt)
        {
            ModelState.AddModelError(
                nameof(model.CheckOutDate),
                "Check-out date and time must be after check-in date and time.");
        }

        if (model.GuestCount <= 0)
        {
            ModelState.AddModelError(
                nameof(model.GuestCount),
                "Guest count must be at least 1.");
        }

        if (model.GuestCount > room.Capacity)
        {
            ModelState.AddModelError(
                nameof(model.GuestCount),
                $"This room allows up to {room.Capacity} guest(s).");
        }

        // First let SQL Server filter by date only. Then perform the exact
        // DateTime + TimeSpan overlap check in memory because EF Core cannot
        // translate DateTime.Add(TimeSpan) to SQL Server.
        var possibleReservations = await context.HotelReservations
            .AsNoTracking()
            .Where(b =>
                b.HotelRoomId == room.HotelRoomId &&
                b.ReservationStatus != HotelReservationStatus.Cancelled &&
                b.CheckInDate <= model.CheckOutDate.Date &&
                b.CheckOutDate >= model.CheckInDate.Date)
            .ToListAsync();

        var occupied = possibleReservations.Count(b =>
        {
            var existingCheckIn = b.CheckInDate.Date.Add(b.CheckInTime);
            var existingCheckOut = b.CheckOutDate.Date.Add(b.CheckOutTime);

            return existingCheckIn < checkOutAt && existingCheckOut > checkInAt;
        });

        if (occupied >= room.TotalRooms)
        {
            ModelState.AddModelError(
                string.Empty,
                "This room is no longer available for the selected date and time.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var nights = Math.Max(1, (int)Math.Ceiling((checkOutAt - checkInAt).TotalDays));

        var reservation = new HotelReservation
        {
            ReservationReference =
                $"HTL{DateTime.Now:yyMMdd}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            ApplicationUserId = CurrentAccountIsStaff ? null : accountId.Value,
            StaffUserId = CurrentAccountIsStaff ? accountId.Value : null,
            HotelRoomId = room.HotelRoomId,
            CheckInDate = model.CheckInDate.Date,
            CheckOutDate = model.CheckOutDate.Date,
            CheckInTime = model.CheckInTime,
            CheckOutTime = model.CheckOutTime,
            GuestCount = model.GuestCount,
            PricePerNight = room.PricePerNight,
            TotalAmount = room.PricePerNight * nights,
            ReservationStatus = HotelReservationStatus.Pending,
            PaymentStatus = "Pending",
            PaymentMethod = string.Empty,
            ContactName = model.ContactName.Trim(),
            ContactEmail = model.ContactEmail.Trim(),
            ContactPhone = model.ContactPhone.Trim()
        };

        context.HotelReservations.Add(reservation);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Payment), new { id = reservation.HotelReservationId });
    }

    public async Task<IActionResult> MyReservations(string? status)
    {
        var query = context.HotelReservations
            .AsNoTracking()
            .Include(b => b.HotelRoom)
            .ThenInclude(r => r!.Photos)
            .Where(b => (CurrentAccountIsStaff && b.StaffUserId == CurrentAccountId) ||
                        (!CurrentAccountIsStaff && b.ApplicationUserId == CurrentAccountId));

        if (Enum.TryParse<HotelReservationStatus>(status, true, out var parsed))
            query = query.Where(b => b.ReservationStatus == parsed);

        ViewBag.Status = status;

        return View(await query
            .OrderByDescending(b => b.ReservationDate)
            .ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
        => await Owned(id) is { } reservation ? View(reservation) : NotFound();

    [HttpGet]
    public async Task<IActionResult> Payment(int id)
    {
        var reservation = await Owned(id);

        if (reservation is null)
            return NotFound();

        if (reservation.PaymentStatus == "Paid" ||
            reservation.ReservationStatus != HotelReservationStatus.Pending)
        {
            TempData["Message"] = reservation.PaymentStatus == "Paid"
                ? "This reservation has already been paid."
                : "This reservation is not awaiting payment.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var nights = GetNights(reservation);
        var subtotal = reservation.PricePerNight * nights;
        var tax = Math.Round(subtotal * HotelSstRate, 2, MidpointRounding.AwayFromZero);

        return View(new HotelPaymentViewModel
        {
            HotelReservationId = reservation.HotelReservationId,
            ReservationReference = reservation.ReservationReference,
            HotelName = reservation.HotelRoom?.HotelName ?? "Hotel Reservation",
            RoomName = reservation.HotelRoom?.RoomName ?? string.Empty,
            Nights = nights,
            Subtotal = subtotal,
            TaxAmount = tax,
            TotalAmount = subtotal + tax
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Payment(HotelPaymentViewModel model, string? action)
    {
        var allowedMethods = new[]
        {
            "Online Banking (FPX)",
            "Credit / Debit Card",
            "Touch 'n Go eWallet"
        };

        if (!allowedMethods.Contains(model.PaymentMethod))
        {
            ModelState.AddModelError(nameof(model.PaymentMethod), "Please select a valid payment method.");
        }

        var reservation = await context.HotelReservations
            .Include(r => r.HotelRoom)
            .FirstOrDefaultAsync(r =>
                r.HotelReservationId == model.HotelReservationId &&
                ((CurrentAccountIsStaff && r.StaffUserId == CurrentAccountId) ||
                 (!CurrentAccountIsStaff && r.ApplicationUserId == CurrentAccountId)));

        if (reservation is null)
            return NotFound();

        if (reservation.PaymentStatus == "Paid" ||
            reservation.ReservationStatus != HotelReservationStatus.Pending)
        {
            return RedirectToAction(nameof(Details), new { id = reservation.HotelReservationId });
        }

        var nights = GetNights(reservation);
        var subtotal = reservation.PricePerNight * nights;
        var discount = GetVoucherDiscount(model.VoucherCode, subtotal, out var voucherError);
        if (voucherError is not null)
            ModelState.AddModelError(nameof(model.VoucherCode), voucherError);

        var taxableAmount = Math.Max(0, subtotal - discount);
        var tax = Math.Round(taxableAmount * HotelSstRate, 2, MidpointRounding.AwayFromZero);
        var total = taxableAmount + tax;

        var possibleReservations = await context.HotelReservations
            .AsNoTracking()
            .Where(b =>
                b.HotelReservationId != reservation.HotelReservationId &&
                b.HotelRoomId == reservation.HotelRoomId &&
                b.ReservationStatus != HotelReservationStatus.Cancelled &&
                b.CheckInDate <= reservation.CheckOutDate.Date &&
                b.CheckOutDate >= reservation.CheckInDate.Date)
            .ToListAsync();

        var checkInAt = reservation.CheckInDate.Date.Add(reservation.CheckInTime);
        var checkOutAt = reservation.CheckOutDate.Date.Add(reservation.CheckOutTime);
        var occupied = possibleReservations.Count(b =>
            b.CheckInDate.Date.Add(b.CheckInTime) < checkOutAt &&
            b.CheckOutDate.Date.Add(b.CheckOutTime) > checkInAt);

        if (occupied >= (reservation.HotelRoom?.TotalRooms ?? 0))
            ModelState.AddModelError(string.Empty, "This room is no longer available for the selected dates.");

        if (string.Equals(action, "apply", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.Remove(nameof(model.PaymentMethod));
        }

        if (!ModelState.IsValid)
        {
            model.ReservationReference = reservation.ReservationReference;
            model.HotelName = reservation.HotelRoom?.HotelName ?? "Hotel Reservation";
            model.RoomName = reservation.HotelRoom?.RoomName ?? string.Empty;
            model.Nights = nights;
            model.Subtotal = subtotal;
            model.DiscountAmount = discount;
            model.TaxAmount = tax;
            model.TotalAmount = total;
            return View(model);
        }

        if (string.Equals(action, "apply", StringComparison.OrdinalIgnoreCase))
        {
            model.ReservationReference = reservation.ReservationReference;
            model.HotelName = reservation.HotelRoom?.HotelName ?? "Hotel Reservation";
            model.RoomName = reservation.HotelRoom?.RoomName ?? string.Empty;
            model.Nights = nights;
            model.Subtotal = subtotal;
            model.DiscountAmount = discount;
            model.TaxAmount = tax;
            model.TotalAmount = total;
            return View(model);
        }

        reservation.TotalAmount = total;
        reservation.PaymentStatus = "Paid";
        reservation.PaymentMethod = model.PaymentMethod;
        reservation.ReservationStatus = HotelReservationStatus.Confirmed;

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = reservation.HotelReservationId });
    }

    private static int GetNights(HotelReservation reservation)
        => Math.Max(1, (int)Math.Ceiling((reservation.CheckOutDate.Date.Add(reservation.CheckOutTime) -
            reservation.CheckInDate.Date.Add(reservation.CheckInTime)).TotalDays));

    private static decimal GetVoucherDiscount(string? voucherCode, decimal subtotal, out string? error)
    {
        error = null;
        var code = voucherCode?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(code))
            return 0;

        if (code == "TRAVEL2026")
            return Math.Round(subtotal * 0.15m, 2, MidpointRounding.AwayFromZero);

        if (code == "PROMO10" || code == "FLY50")
            return Math.Min(subtotal, code == "PROMO10" ? 10m : 50m);

        error = "The voucher code is not valid.";
        return 0;
    }

    [HttpGet]
    public async Task<IActionResult> Cancel(int id)
        => await Owned(id) is { } booking &&
           (booking.ReservationStatus == HotelReservationStatus.Confirmed ||
            booking.ReservationStatus == HotelReservationStatus.Pending) &&
            booking.CheckInDate.Date.Add(booking.CheckInTime) > DateTime.Now
            ? View(booking)
            : BadRequest();

    [HttpPost]
    [ActionName("Cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelConfirmed(int id, string cancellationReason)
    {
        var booking = await context.HotelReservations
            .FirstOrDefaultAsync(b => b.HotelReservationId == id &&
                ((CurrentAccountIsStaff && b.StaffUserId == CurrentAccountId) ||
                 (!CurrentAccountIsStaff && b.ApplicationUserId == CurrentAccountId)));

        if (booking is null ||
            (booking.ReservationStatus != HotelReservationStatus.Confirmed &&
             booking.ReservationStatus != HotelReservationStatus.Pending) ||
            booking.CheckInDate.Date.Add(booking.CheckInTime) <= DateTime.Now)
        {
            return BadRequest();
        }

        if (string.IsNullOrWhiteSpace(cancellationReason) || cancellationReason.Length > 400)
        {
            ModelState.AddModelError(
                "cancellationReason",
                "Please provide a cancellation reason (maximum 400 characters).");

            return View("Cancel", booking);
        }

        booking.ReservationStatus = HotelReservationStatus.Cancelled;
        booking.CancellationReason = cancellationReason.Trim();
        booking.CancelledAt = DateTime.Now;

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Review(int reservationId)
    {
        var booking = await Owned(reservationId);

        if (booking is null || booking.ReservationStatus != HotelReservationStatus.Completed)
            return BadRequest();

        return View(new HotelReviewViewModel
        {
            HotelReservationId = reservationId,
            RoomName = booking.HotelRoom!.RoomName,
            Rating = booking.Review?.Rating ?? 5,
            Comment = booking.Review?.Comment ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(HotelReviewViewModel model)
    {
        var booking = await context.HotelReservations
            .Include(b => b.Review)
            .Include(b => b.HotelRoom)
            .FirstOrDefaultAsync(b =>
                b.HotelReservationId == model.HotelReservationId &&
                (CurrentAccountIsStaff && b.StaffUserId == CurrentAccountId) ||
                (!CurrentAccountIsStaff && b.ApplicationUserId == CurrentAccountId));

        if (booking?.HotelRoom is null ||
            booking.ReservationStatus != HotelReservationStatus.Completed)
        {
            return BadRequest();
        }

        model.RoomName = booking.HotelRoom.RoomName;

        if (!ModelState.IsValid)
            return View(model);

        if (booking.Review is null)
        {
            context.HotelReviews.Add(new HotelReview
            {
                HotelRoomId = booking.HotelRoomId,
                HotelReservationId = booking.HotelReservationId,
                UserId = 1, // Retained for compatibility with the legacy schema.
                Rating = model.Rating,
                Comment = model.Comment.Trim()
            });

        }
        else
        {
            booking.Review.Rating = model.Rating;
            booking.Review.Comment = model.Comment.Trim();
        }

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = booking.HotelReservationId });
    }

    private Task<HotelReservation?> Owned(int id)
        => context.HotelReservations
            .AsNoTracking()
            .Include(b => b.HotelRoom)
            .ThenInclude(r => r!.Photos)
            .Include(b => b.ApplicationUser)
            .Include(b => b.StaffUser)
            .Include(b => b.Review)
            .FirstOrDefaultAsync(b =>
                b.HotelReservationId == id &&
                ((CurrentAccountIsStaff && b.StaffUserId == CurrentAccountId) ||
                 (!CurrentAccountIsStaff && b.ApplicationUserId == CurrentAccountId)));
}
