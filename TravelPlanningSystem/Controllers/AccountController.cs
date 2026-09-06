using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using TravelPlanningSystem.ViewModels.LoginViewModel;
using TravelPlanningSystem.ViewModels.RegisterViewModel;
using TravelPlanningSystem.Data;
using TravelPlanningSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace TravelPlanningSystem.Controllers
{
    public class AccountController : Controller
    {
    private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AccountController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpGet]
        public IActionResult PhoneLogin()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PhoneLogin(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var identifier = (model.Username ?? string.Empty).Trim();
            var passwordHash = PasswordHashing.Hash(model.Password ?? string.Empty);

            // Validate phone format
            var phonePattern = new System.Text.RegularExpressions.Regex("^\\+?[0-9]{6,20}$");
            if (!phonePattern.IsMatch(identifier))
            {
                ModelState.AddModelError(nameof(model.Username), "Invalid phone number format. Include country code like +60 and digits only.");
                return View(model);
            }

            var normalized = NormalizePhone(identifier);

            var users = await _context.Users
                .Select(u => new ApplicationUser
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    PasswordHash = u.PasswordHash,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber
                })
                .ToListAsync();

            var user = users.FirstOrDefault(u => NormalizePhone(u.PhoneNumber) == normalized);

            if (user != null && user.PasswordHash == passwordHash)
            {
                var fullUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId) ?? user;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, $"{fullUser.FirstName} {fullUser.LastName}"),
                    new Claim(ClaimTypes.Email, fullUser.Email),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("ProfilePictureUrl", fullUser.ProfilePictureUrl ?? string.Empty),
                    new Claim("ProfilePic", fullUser.ProfilePic ?? string.Empty)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid phone or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var identifier = (model.Username ?? string.Empty).Trim();
            var passwordHash = PasswordHashing.Hash(model.Password ?? string.Empty);

            // Try staff login by email first (staff use email only)
            if (identifier.Contains("@"))
            {
                var staff = await _context.StaffUsers
                    .Include(s => s.StaffRole)
                    .FirstOrDefaultAsync(s => s.Email.ToLower() == identifier.ToLower());

                if (staff != null && staff.Status == "Active" && staff.PasswordHash == passwordHash)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, $"{staff.FirstName} {staff.LastName}"),
                        new Claim(ClaimTypes.Email, staff.Email),
                        new Claim(ClaimTypes.Role, staff.StaffRole?.RoleName ?? "Staff")
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    if (string.Equals(staff.StaffRole?.RoleName, "Administrator", StringComparison.OrdinalIgnoreCase))
                        return RedirectToAction("Index", "AdminDashboard");

                    return RedirectToAction("Index", "Home");
                }
            }

            // Try application user login by email or phone
            ApplicationUser? user = null;

            if (identifier.Contains("@"))
            {
                user = await _context.Users
                    .Where(u => u.Email.ToLower() == identifier.ToLower())
                    .Select(u => new ApplicationUser
                    {
                        UserId = u.UserId,
                        Email = u.Email,
                        PasswordHash = u.PasswordHash,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        PhoneNumber = u.PhoneNumber
                    })
                    .FirstOrDefaultAsync();
            }
            else
            {
                // Normalize phone: digits only
                var normalized = NormalizePhone(identifier);

                // Validate phone format (allow optional leading + and digits)
                var phonePattern = new System.Text.RegularExpressions.Regex("^\\+?[0-9]{6,20}$");
                if (!phonePattern.IsMatch(identifier))
                {
                    ModelState.AddModelError(nameof(model.Username), "Invalid phone number format. Include country code like +60 and digits only.");
                    return View(model);
                }

                // Load candidates from DB (you may add additional SQL-friendly filtering here)
                var users = await _context.Users
                    .Select(u => new ApplicationUser
                    {
                        UserId = u.UserId,
                        Email = u.Email,
                        PasswordHash = u.PasswordHash,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        PhoneNumber = u.PhoneNumber
                    })
                    .ToListAsync();

                user = users.FirstOrDefault(u => NormalizePhone(u.PhoneNumber) == normalized);
            }

            if (user != null && user.PasswordHash == passwordHash)
            {
                // reload full user to get profile picture fields
                var fullUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId) ?? user;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, $"{fullUser.FirstName} {fullUser.LastName}"),
                    new Claim(ClaimTypes.Email, fullUser.Email),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("ProfilePictureUrl", fullUser.ProfilePictureUrl ?? string.Empty),
                    new Claim("ProfilePic", fullUser.ProfilePic ?? string.Empty)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid email/phone or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // Registration
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var email = User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound();

            var model = new TravelPlanningSystem.ViewModels.ProfileViewModel.ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                ProfilePictureUrl = user.ProfilePictureUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(TravelPlanningSystem.ViewModels.ProfileViewModel.ProfileViewModel model)
        {
            var email = User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            // Handle file upload
            if (model.Upload != null && model.Upload.Length > 0)
            {
                var uploads = Path.Combine(_environment.WebRootPath, "images", "profiles");
                if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                var ext = Path.GetExtension(model.Upload.FileName);
                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    await model.Upload.CopyToAsync(stream);
                }

                user.ProfilePictureUrl = $"/images/profiles/{fileName}";
                user.ProfilePic = user.ProfilePictureUrl;
            }

            // Update basic fields
            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            user.PhoneNumber = model.PhoneNumber ?? user.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth ?? user.DateOfBirth;

            await _context.SaveChangesAsync();

            // refresh auth cookie so the layout immediately shows the updated picture
            var updatedClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, "User"),
                new Claim("ProfilePictureUrl", user.ProfilePictureUrl ?? string.Empty),
                new Claim("ProfilePic", user.ProfilePic ?? string.Empty)
            };

            var newIdentity = new ClaimsIdentity(updatedClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(newIdentity));

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check duplicate email
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email is already registered.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                Email = model.Email,
                PasswordHash = PasswordHashing.Hash(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                PreferredCurrency = model.PreferredCurrency ?? "USD",
                PreferredLanguage = model.PreferredLanguage ?? "en-US",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Auto-sign in after register
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("ProfilePictureUrl", user.ProfilePictureUrl ?? string.Empty),
                new Claim("ProfilePic", user.ProfilePic ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        private static string NormalizePhone(string? p)
        {
            return string.IsNullOrEmpty(p) ? string.Empty : new string(p.Where(char.IsDigit).ToArray());
        }
    }
}
