using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminUserManagementController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(new AdminUserManagementViewModel
        {
            Members = await context.Users.AsNoTracking().OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync(),
            StaffUsers = await context.StaffUsers.AsNoTracking().Include(s => s.StaffRole).OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync(),
            StaffRoles = await context.StaffRoles.AsNoTracking().OrderBy(r => r.RoleName).ToListAsync()
        });
    }

    [HttpGet]
    public IActionResult CreateMember() => View(new MemberManagementInput());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMember(MemberManagementInput model)
    {
        await ValidateEmailAsync(model.Email, null, null);
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "A password is required for a new member.");

        if (!ModelState.IsValid) return View(model);

        context.Users.Add(new ApplicationUser
        {
            Email = model.Email.Trim(),
            PasswordHash = PasswordHashing.Hash(model.Password!),
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            PhoneNumber = model.PhoneNumber,
            DateOfBirth = model.DateOfBirth,
            PreferredCurrency = model.PreferredCurrency,
            PreferredLanguage = model.PreferredLanguage,
            LoyaltyTier = model.LoyaltyTier,
            RewardPoints = model.RewardPoints,
            AccountStatus = model.AccountStatus,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        TempData["Message"] = "Member created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditMember(Guid id)
    {
        var user = await context.Users.FindAsync(id);
        if (user is null) return NotFound();

        return View(new MemberManagementInput
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            PreferredCurrency = user.PreferredCurrency,
            PreferredLanguage = user.PreferredLanguage,
            LoyaltyTier = user.LoyaltyTier,
            RewardPoints = user.RewardPoints,
            AccountStatus = user.AccountStatus
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMember(MemberManagementInput model)
    {
        var user = await context.Users.FindAsync(model.UserId);
        if (user is null) return NotFound();

        await ValidateEmailAsync(model.Email, model.UserId, null);
        if (!ModelState.IsValid) return View(model);

        user.Email = model.Email.Trim();
        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = model.PhoneNumber;
        user.DateOfBirth = model.DateOfBirth;
        user.PreferredCurrency = model.PreferredCurrency;
        user.PreferredLanguage = model.PreferredLanguage;
        user.LoyaltyTier = model.LoyaltyTier;
        user.RewardPoints = model.RewardPoints;
        user.AccountStatus = model.AccountStatus;
        if (!string.IsNullOrWhiteSpace(model.Password))
            user.PasswordHash = PasswordHashing.Hash(model.Password);

        await context.SaveChangesAsync();
        TempData["Message"] = "Member updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteMember(Guid id)
    {
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        return user is null ? NotFound() : View(user);
    }

    [HttpPost, ActionName("DeleteMember"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMemberConfirmed(Guid id)
    {
        var user = await context.Users.FindAsync(id);
        if (user is null) return NotFound();

        context.Users.Remove(user);
        await context.SaveChangesAsync();
        TempData["Message"] = "Member deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> CreateStaff()
    {
        await LoadRolesAsync();
        return View(new StaffManagementInput());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff(StaffManagementInput model)
    {
        await ValidateEmailAsync(model.Email, null, null);
        if (model.RoleId == Guid.Empty || !await context.StaffRoles.AnyAsync(r => r.RoleId == model.RoleId))
            ModelState.AddModelError(nameof(model.RoleId), "Select a valid staff role.");
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "A password is required for new staff.");

        if (!ModelState.IsValid)
        {
            await LoadRolesAsync();
            return View(model);
        }

        context.StaffUsers.Add(new StaffUser
        {
            Email = model.Email.Trim(),
            PasswordHash = PasswordHashing.Hash(model.Password!),
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Department = model.Department.Trim(),
            RoleId = model.RoleId,
            AccessLevel = model.AccessLevel,
            Status = model.Status,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        TempData["Message"] = "Staff user created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditStaff(Guid id)
    {
        var staff = await context.StaffUsers.FindAsync(id);
        if (staff is null) return NotFound();

        await LoadRolesAsync();
        return View(new StaffManagementInput
        {
            StaffId = staff.StaffId,
            Email = staff.Email,
            FirstName = staff.FirstName,
            LastName = staff.LastName,
            Department = staff.Department,
            RoleId = staff.RoleId,
            AccessLevel = staff.AccessLevel,
            Status = staff.Status
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStaff(StaffManagementInput model)
    {
        var staff = await context.StaffUsers.FindAsync(model.StaffId);
        if (staff is null) return NotFound();

        await ValidateEmailAsync(model.Email, null, model.StaffId);
        if (model.RoleId == Guid.Empty || !await context.StaffRoles.AnyAsync(r => r.RoleId == model.RoleId))
            ModelState.AddModelError(nameof(model.RoleId), "Select a valid staff role.");

        if (!ModelState.IsValid)
        {
            await LoadRolesAsync();
            return View(model);
        }

        staff.Email = model.Email.Trim();
        staff.FirstName = model.FirstName.Trim();
        staff.LastName = model.LastName.Trim();
        staff.Department = model.Department.Trim();
        staff.RoleId = model.RoleId;
        staff.AccessLevel = model.AccessLevel;
        staff.Status = model.Status;
        if (!string.IsNullOrWhiteSpace(model.Password))
            staff.PasswordHash = PasswordHashing.Hash(model.Password);

        await context.SaveChangesAsync();
        TempData["Message"] = "Staff user updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteStaff(Guid id)
    {
        var staff = await context.StaffUsers.AsNoTracking().Include(s => s.StaffRole).FirstOrDefaultAsync(s => s.StaffId == id);
        return staff is null ? NotFound() : View(staff);
    }

    [HttpPost, ActionName("DeleteStaff"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteStaffConfirmed(Guid id)
    {
        var staff = await context.StaffUsers.FindAsync(id);
        if (staff is null) return NotFound();

        var currentEmail = User.FindFirstValue(ClaimTypes.Email);
        if (string.Equals(staff.Email, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "You cannot delete the account currently being used for administration.";
            return RedirectToAction(nameof(Index));
        }

        context.StaffUsers.Remove(staff);
        await context.SaveChangesAsync();
        TempData["Message"] = "Staff user deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateEmailAsync(string email, Guid? memberId, Guid? staffId)
    {
        var normalizedEmail = email.Trim().ToLower();
        if (await context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail && (!memberId.HasValue || u.UserId != memberId.Value)) ||
            await context.StaffUsers.AnyAsync(s => s.Email.ToLower() == normalizedEmail && (!staffId.HasValue || s.StaffId != staffId.Value)))
        {
            ModelState.AddModelError(nameof(MemberManagementInput.Email), "That email address is already in use.");
            ModelState.AddModelError(nameof(StaffManagementInput.Email), "That email address is already in use.");
        }
    }

    private async Task LoadRolesAsync()
    {
        ViewBag.StaffRoles = await context.StaffRoles.AsNoTracking().OrderBy(r => r.RoleName).ToListAsync();
    }
}
