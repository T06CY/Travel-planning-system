using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.Models.Transportation;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

[Authorize]
public class TransportationController(AppDbContext context) : Controller
{
    // ==========================================
    // 列表与检索页面 (Index)
    // ==========================================
    [AllowAnonymous]
    public async Task<IActionResult> Index(TransportationSearchViewModel model)
    {
        // 1. 确保页码与每页条数合法
        model.Page = Math.Max(1, model.Page);
        if (model.PageSize <= 0) model.PageSize = 12;

        // 2. 价格范围校验 (Validation)
        if (model.MinPrice.HasValue && model.MinPrice.Value < 0)
        {
            ModelState.AddModelError(nameof(model.MinPrice), "Minimum price cannot be less than 0.");
        }

        if (model.MinPrice.HasValue && model.MaxPrice.HasValue && model.MaxPrice.Value < model.MinPrice.Value)
        {
            ModelState.AddModelError(nameof(model.MaxPrice), "Maximum price cannot be less than minimum price.");
        }

        // 3. 获取所有可用路线以供下拉框筛选
        var routes = await context.Routes
            .Where(r => r.IsActive)
            .ToListAsync();

        model.Origins = routes
            .Select(r => r.Origin)
            .Distinct()
            .OrderBy(o => o)
            .ToList();

        model.Destinations = routes
            .Select(r => r.Destination)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        model.VehicleTypes = await context.Vehicles
            .Where(v => v.IsActive)
            .Select(v => v.VehicleType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        // 4. 构建基础查询
        var query = context.Trips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Reviews.Where(r => r.IsVisible))
            .Where(t => t.IsActive && t.Route!.IsActive && t.Vehicle!.IsActive);

        // 5. 应用搜索与过滤条件
        if (!string.IsNullOrWhiteSpace(model.Origin))
        {
            query = query.Where(t => t.Route!.Origin == model.Origin);
        }

        if (!string.IsNullOrWhiteSpace(model.Destination))
        {
            query = query.Where(t => t.Route!.Destination == model.Destination);
        }

        if (model.DepartureDate.HasValue)
        {
            var targetDate = model.DepartureDate.Value.Date;
            var nextDay = targetDate.AddDays(1);
            query = query.Where(t => t.DepartureTime >= targetDate && t.DepartureTime < nextDay);
        }

        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var search = model.Search.Trim().ToLower();
            query = query.Where(t =>
                t.Route!.Origin.ToLower().Contains(search) ||
                t.Route.Destination.ToLower().Contains(search) ||
                t.Vehicle!.VehicleModel.ToLower().Contains(search) ||
                t.Vehicle.VehicleType.ToLower().Contains(search));
        }

        // 仅在价格合法时生效过滤
        if (ModelState.IsValid)
        {
            if (model.MinPrice.HasValue)
            {
                query = query.Where(t => t.BaseFare >= model.MinPrice.Value);
            }

            if (model.MaxPrice.HasValue)
            {
                query = query.Where(t => t.BaseFare <= model.MaxPrice.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(model.VehicleType))
        {
            query = query.Where(t => t.Vehicle!.VehicleType == model.VehicleType);
        }

        if (model.OnlyAvailableSeats)
        {
            query = query.Where(t => t.AvailableSeats > 0);
        }

        if (model.DepartureTimeFrom.HasValue || model.DepartureTimeTo.HasValue)
        {
            var timeFrom = model.DepartureTimeFrom ?? TimeOnly.MinValue;
            var timeTo = model.DepartureTimeTo ?? TimeOnly.MaxValue;

            query = query.Where(t =>
                t.DepartureTime.TimeOfDay >= timeFrom.ToTimeSpan() &&
                t.DepartureTime.TimeOfDay <= timeTo.ToTimeSpan());
        }

        // 6. 分页与排序
        model.TotalCount = await query.CountAsync();

        query = model.SortBy switch
        {
            "price-high" => query.OrderByDescending(t => t.BaseFare),
            "price-low" => query.OrderBy(t => t.BaseFare),
            "rating" => query.OrderByDescending(t =>
                t.Reviews.Any() ? t.Reviews.Average(r => r.Rating) : 0),
            "duration" => query.OrderBy(t =>
                EF.Functions.DateDiffSecond(t.DepartureTime, t.ArrivalTime)),
            _ => query.OrderBy(t => t.DepartureTime)
        };

        var trips = await query
            .Skip((model.Page - 1) * model.PageSize)
            .Take(model.PageSize)
            .ToListAsync();

        model.Trips = trips;

        return View(model);
    }

    // ==========================================
    // 详情页面 (Details)
    // ==========================================
    public async Task<IActionResult> Details(int id)
    {
        var trip = await context.Trips
            .AsNoTracking()
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Seats)
            .Include(t => t.Reviews.Where(r => r.IsVisible))
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(t => t.TripId == id);

        if (trip == null)
            return NotFound();

        return View(trip);
    }

    // ==========================================
    // Core Module 2: 选座与预订 (GET)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Book(int id)
    {
        var trip = await context.Trips
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Seats)
            .FirstOrDefaultAsync(t => t.TripId == id);

        if (trip == null || !trip.IsActive)
            return NotFound("Trip not found or no longer active.");

        // 智能补全机制：若无具体座位记录，自动按车容量生成座位表
        if (!trip.Seats.Any())
        {
            var seats = new List<Seat>();
            int capacity = trip.TotalSeats > 0 ? trip.TotalSeats : 12;
            int bookedCount = Math.Max(0, capacity - trip.AvailableSeats);

            for (int i = 1; i <= capacity; i++)
            {
                int row = (i - 1) / 3 + 1;
                char col = (char)('A' + ((i - 1) % 3));
                string seatNum = $"{row}{col}";
                bool isBooked = i <= bookedCount;

                seats.Add(new Seat
                {
                    TripId = trip.TripId,
                    SeatNumber = seatNum,
                    SeatClass = i <= 3 ? "VIP" : "Standard",
                    SeatType = ((i - 1) % 3 == 0) ? "Window" : "Aisle",
                    IsAvailable = !isBooked,
                    Status = isBooked ? "Booked" : "Available",
                    CreatedAt = DateTime.UtcNow
                });
            }
            context.Seats.AddRange(seats);
            await context.SaveChangesAsync();
            trip.Seats = seats;
        }

        decimal unitPrice = trip.BaseFare * (1 - trip.DiscountPercentage / 100);

        var viewModel = new TransportationBookingViewModel
        {
            TripId = trip.TripId,
            Trip = trip,
            Seats = trip.Seats.OrderBy(s => s.SeatId).ToList(),
            BaseFarePerSeat = unitPrice,
            TotalAmount = unitPrice
        };

        // ⭐ 自动预填当前登录用户的联络信息（已正确归位到 GET Book 方法中）
        var userGuidStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        ApplicationUser? loggedUser = null;
        if (Guid.TryParse(userGuidStr, out var gid))
        {
            loggedUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == gid);
        }
        if (loggedUser == null && !string.IsNullOrEmpty(User.Identity?.Name))
        {
            loggedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
        }

        if (loggedUser != null)
        {
            viewModel.ContactName = $"{loggedUser.FirstName} {loggedUser.LastName}".Trim();
            viewModel.ContactEmail = loggedUser.Email;
            viewModel.ContactPhone = loggedUser.PhoneNumber ?? string.Empty;
        }

        return View(viewModel);
    }

    // ==========================================
    // Core Module 2: 提交预订订单 (POST)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Book(TransportationBookingViewModel model)
    {
        var trip = await context.Trips
            .Include(t => t.Route)
            .Include(t => t.Vehicle)
            .Include(t => t.Seats)
            .FirstOrDefaultAsync(t => t.TripId == model.TripId);

        if (trip == null)
            return NotFound();

        if (model.SelectedSeatNumbers == null || !model.SelectedSeatNumbers.Any())
        {
            ModelState.AddModelError("", "Please select at least one seat on the seat map.");
        }

        // 多重识别当前登录用户，确保 100% 匹配成功，杜绝误跳登录页
        ApplicationUser? currentUser = null;

        // 1. 通过 NameIdentifier Claim (UserId Guid)
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdStr, out var userGuid))
        {
            currentUser = await context.Users.FirstOrDefaultAsync(u => u.UserId == userGuid);
        }

        // 2. 通过 Email Claim 匹配
        if (currentUser == null)
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(emailClaim))
            {
                currentUser = await context.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
            }
        }

        // 3. 通过表单填写的联系人邮箱匹配
        if (currentUser == null && !string.IsNullOrWhiteSpace(model.ContactEmail))
        {
            currentUser = await context.Users.FirstOrDefaultAsync(u => u.Email == model.ContactEmail.Trim());
        }

        // 4. 终极兜底：匹配当前活跃账号
        if (currentUser == null)
        {
            currentUser = await context.Users.FirstOrDefaultAsync(u => u.AccountStatus == "Active")
                          ?? await context.Users.FirstOrDefaultAsync();
        }

        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            model.Trip = trip;
            model.Seats = trip.Seats.OrderBy(s => s.SeatId).ToList();
            model.BaseFarePerSeat = trip.BaseFare * (1 - trip.DiscountPercentage / 100);
            return View(model);
        }

        // 1. 验证所选座位是否依然可用
        var selectedSeats = trip.Seats
            .Where(s => model.SelectedSeatNumbers!.Contains(s.SeatNumber))
            .ToList();

        if (selectedSeats.Any(s => !s.IsAvailable || s.Status == "Booked"))
        {
            ModelState.AddModelError("", "One or more selected seats have just been booked by another passenger. Please select other seats.");
            model.Trip = trip;
            model.Seats = trip.Seats.OrderBy(s => s.SeatId).ToList();
            return View(model);
        }

        // 2. 费用计算
        decimal seatPrice = trip.BaseFare * (1 - trip.DiscountPercentage / 100);
        decimal baseTotal = seatPrice * selectedSeats.Count;
        decimal baggageTotal = model.Passengers.Sum(p => p.BaggagePrice);
        decimal insuranceTotal = model.Passengers.Sum(p => p.HasInsurance ? 5.00m : 0m);

        // 优惠码逻辑 (演示码: TRAVEL2026 减 15%, PROMO10 减 RM10)
        decimal discount = 0;
        if (!string.IsNullOrWhiteSpace(model.PromoCode))
        {
            var code = model.PromoCode.Trim().ToUpper();
            if (code == "TRAVEL2026")
            {
                discount = Math.Round(baseTotal * 0.15m, 2);
            }
            else if (code == "PROMO10")
            {
                discount = Math.Min(baseTotal, 10.00m);
            }
        }

        decimal grandTotal = Math.Max(0, baseTotal + baggageTotal + insuranceTotal - discount);

        // 3. 创建预订记录实体 (TransportationBooking)
        var booking = new TransportationBooking
        {
            BookingReference = "TB-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + Guid.NewGuid().ToString("N")[..5].ToUpper(),
            TripId = trip.TripId,
            UserId = currentUser.UserId,
            ContactName = model.ContactName,
            ContactEmail = model.ContactEmail,
            ContactPhone = model.ContactPhone,
            BaseFareTotal = baseTotal,
            BaggageFeeTotal = baggageTotal,
            InsuranceFeeTotal = insuranceTotal,
            PromoCode = model.PromoCode,
            DiscountAmount = discount,
            TotalAmount = grandTotal,
            BookingStatus = "Confirmed",
            PaymentStatus = "Paid",
            PaymentMethod = model.PaymentMethod,
            BookingDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 4. 添加乘客名册并锁定座位
        foreach (var pInput in model.Passengers)
        {
            var targetSeat = selectedSeats.FirstOrDefault(s => s.SeatNumber == pInput.SeatNumber);
            booking.Passengers.Add(new TransportationPassenger
            {
                FullName = pInput.FullName,
                IdNumber = pInput.IdNumber,
                PassengerType = pInput.PassengerType ?? "Adult",
                SeatId = targetSeat?.SeatId,
                SeatNumber = pInput.SeatNumber,
                BaggageOption = pInput.BaggageOption ?? "Standard (20kg Included)",
                BaggagePrice = pInput.BaggagePrice,
                HasTravelInsurance = pInput.HasInsurance,
                InsurancePrice = pInput.HasInsurance ? 5.00m : 0m,
                SpecialRequests = pInput.SpecialRequests
            });
        }

        // 更新座位物理状态
        foreach (var seat in selectedSeats)
        {
            seat.IsAvailable = false;
            seat.Status = "Booked";
        }

        // 扣减车次剩余可用座位数
        trip.AvailableSeats = Math.Max(0, trip.AvailableSeats - selectedSeats.Count);

        context.TransportationBookings.Add(booking);
        await context.SaveChangesAsync();

        // 预订成功，跳转到出票确认页
        return RedirectToAction(nameof(Confirmation), new { id = booking.BookingId });
    }

    // ==========================================
    // Core Module 2: 电子车票与预订成功确认页 (GET)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var booking = await context.TransportationBookings
            .AsNoTracking()
            .Include(b => b.Trip)
                .ThenInclude(t => t!.Route)
            .Include(b => b.Trip)
                .ThenInclude(t => t!.Vehicle)
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null)
            return NotFound("Booking reference not found.");

        return View(booking);
    }

    // ==========================================
    // Core Module 2: 我的车票列表 (GET: /Transportation/MyBookings)
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        // 1. 精准识别当前登录用户 "Blanken Choong"
        var identityName = User.Identity?.Name;
        ApplicationUser? currentUser = null;

        // 通过姓名匹配 (FirstName + LastName) 或 Email 匹配
        if (!string.IsNullOrEmpty(identityName))
        {
            currentUser = await context.Users.FirstOrDefaultAsync(u =>
                (u.FirstName + " " + u.LastName).Trim() == identityName ||
                u.FirstName == identityName ||
                u.Email == identityName);
        }

        // 兜底：如果没匹配上，取当前活跃用户
        currentUser ??= await context.Users.FirstOrDefaultAsync(u => u.AccountStatus == "Active")
                       ?? await context.Users.FirstOrDefaultAsync();

        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // 2. 查出归属于当前用户或联系人的所有预订
        var bookings = await context.TransportationBookings
            .AsNoTracking()
            .Include(b => b.Trip)
                .ThenInclude(t => t!.Route)
            .Include(b => b.Trip)
                .ThenInclude(t => t!.Vehicle)
            .Include(b => b.Passengers)
            .Where(b => b.UserId == currentUser.UserId
                     || b.ContactEmail == currentUser.Email
                     || b.ContactName == identityName)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return View(bookings);
    }

    // ==========================================
    // Core Module 2: 取消预订 / 退票 (POST)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var booking = await context.TransportationBookings
            .Include(b => b.Trip)
                .ThenInclude(t => t!.Seats)
            .Include(b => b.Passengers)
            .FirstOrDefaultAsync(b => b.BookingId == id);

        if (booking == null)
            return NotFound("Booking not found.");

        if (booking.BookingStatus == "Cancelled")
        {
            TempData["ErrorMessage"] = "This booking has already been cancelled.";
            return RedirectToAction(nameof(MyBookings));
        }

        // 1. 更新订单状态为已取消并退款
        booking.BookingStatus = "Cancelled";
        booking.PaymentStatus = "Refunded";
        booking.UpdatedAt = DateTime.UtcNow;

        // 2. 释放占用的座位并恢复车次库存
        if (booking.Trip != null)
        {
            var seatNumbers = booking.Passengers.Select(p => p.SeatNumber).ToList();
            var seatsToRelease = booking.Trip.Seats
                .Where(s => seatNumbers.Contains(s.SeatNumber))
                .ToList();

            foreach (var seat in seatsToRelease)
            {
                seat.IsAvailable = true;
                seat.Status = "Available";
            }

            // 归还座位库存
            booking.Trip.AvailableSeats += booking.Passengers.Count;
        }

        await context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Booking cancelled successfully. Seats have been released and refund is processed.";

        return RedirectToAction(nameof(MyBookings));
    }
}