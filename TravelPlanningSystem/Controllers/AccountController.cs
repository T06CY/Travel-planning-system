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
using TravelPlanningSystem.Services;
using TravelPlanningSystem.ViewModels;

namespace TravelPlanningSystem.Controllers
{
    public class AccountController : Controller
    {
    private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly OtpService _otpService;
        private readonly IEmailSender _emailSender;

        public AccountController(AppDbContext context, IWebHostEnvironment environment, OtpService otpService, IEmailSender emailSender)
        {
            _context = context;
            _environment = environment;
            _otpService = otpService;
            _emailSender = emailSender;
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
        public async Task<IActionResult> PhoneLogin(LoginViewModel model, string? returnUrl = null)
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
                     PhoneNumber = u.PhoneNumber,
                     FailedLoginAttempts = u.FailedLoginAttempts,
                     LockoutUntil = u.LockoutUntil
                })
                .ToListAsync();

            var user = users.FirstOrDefault(u => NormalizePhone(u.PhoneNumber) == normalized);

            if (user != null && IsLocked(user.LockoutUntil))
            {
                ModelState.AddModelError(string.Empty, LockoutMessage(user.LockoutUntil!.Value));
                return View(model);
            }

            if (user != null && user.PasswordHash == passwordHash)
            {
                var fullUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId) ?? user;
                ResetLoginFailures(fullUser);
                await _context.SaveChangesAsync();

                return await BeginOtpAsync(fullUser.Email, "login", new PendingLogin
                {
                    UserId = fullUser.UserId,
                    ReturnUrl = returnUrl
                }, model);
            }

            if (user != null)
            {
                var failedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
                if (failedUser != null)
                {
                    RecordFailedLogin(failedUser);
                    await _context.SaveChangesAsync();
                    if (failedUser.LockoutUntil.HasValue)
                    {
                        ModelState.AddModelError(string.Empty, LockoutMessage(failedUser.LockoutUntil.Value));
                        return View(model);
                    }
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid phone or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
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

                if (staff != null && IsLocked(staff.LockoutUntil))
                {
                    ModelState.AddModelError(string.Empty, LockoutMessage(staff.LockoutUntil!.Value));
                    return View(model);
                }

                if (staff != null && staff.Status == "Active" && staff.PasswordHash == passwordHash)
                {
                    ResetLoginFailures(staff);
                    await _context.SaveChangesAsync();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, staff.StaffId.ToString()),
                        new Claim("AccountType", "Staff"),
                        new Claim(ClaimTypes.Name, $"{staff.FirstName} {staff.LastName}"),
                        new Claim(ClaimTypes.Email, staff.Email),
                        new Claim(ClaimTypes.Role, staff.StaffRole?.RoleName ?? "Staff")
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe
                        });

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    if (string.Equals(staff.StaffRole?.RoleName, "Administrator", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(staff.StaffRole?.RoleName, "Support", StringComparison.OrdinalIgnoreCase))
                        return RedirectToAction("Index", "AdminDashboard");

                    return RedirectToAction("Index", "Home");
                }

                if (staff != null && staff.Status == "Active")
                {
                    RecordFailedLogin(staff);
                    await _context.SaveChangesAsync();
                    if (staff.LockoutUntil.HasValue)
                    {
                        ModelState.AddModelError(string.Empty, LockoutMessage(staff.LockoutUntil.Value));
                        return View(model);
                    }
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
                         PhoneNumber = u.PhoneNumber,
                         FailedLoginAttempts = u.FailedLoginAttempts,
                         LockoutUntil = u.LockoutUntil
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

            if (user != null && IsLocked(user.LockoutUntil))
            {
                ModelState.AddModelError(string.Empty, LockoutMessage(user.LockoutUntil!.Value));
                return View(model);
            }

            if (user != null && user.PasswordHash == passwordHash)
            {
                // reload full user to get profile picture fields
                var fullUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId) ?? user;
                ResetLoginFailures(fullUser);
                await _context.SaveChangesAsync();

                return await BeginOtpAsync(fullUser.Email, "login", new PendingLogin
                {
                    UserId = fullUser.UserId,
                    ReturnUrl = returnUrl,
                    RememberMe = model.RememberMe
                }, model);
            }

            if (user != null)
            {
                var failedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
                if (failedUser != null)
                {
                    RecordFailedLogin(failedUser);
                    await _context.SaveChangesAsync();
                    if (failedUser.LockoutUntil.HasValue)
                    {
                        ModelState.AddModelError(string.Empty, LockoutMessage(failedUser.LockoutUntil.Value));
                        return View(model);
                    }
                }
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
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var email = model.Email.Trim();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user is not null)
            {
                var challenge = _otpService.CreateChallenge(user.Email, "password reset");
                _otpService.StorePending(challenge.ChallengeId, new PendingPasswordReset
                {
                    UserId = user.UserId
                });

                TempData["OtpChallengeId"] = challenge.ChallengeId;
                TempData["OtpPurpose"] = "reset-password";
                TempData["OtpEmail"] = user.Email;

                try
                {
                    await _emailSender.SendOtpAsync(user.Email, challenge.Code!, "password reset");
                    return RedirectToAction(nameof(VerifyOtp));
                }
                catch (Exception)
                {
                    _otpService.RemovePending(challenge.ChallengeId);
                    TempData.Remove("OtpChallengeId");
                    TempData.Remove("OtpPurpose");
                    TempData.Remove("OtpEmail");
                    ModelState.AddModelError(string.Empty, "The reset email could not be sent. Check the email configuration and try again.");
                    return View(model);
                }
            }

            TempData["ForgotPasswordMessage"] = "If an account uses that email, a password reset code has been sent.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var challengeId = TempData.Peek("OtpChallengeId") as string;
            var email = TempData.Peek("OtpEmail") as string;
            if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login");

            ViewData["OtpEmail"] = email;
            return View(new VerifyOtpViewModel
            {
                Purpose = TempData.Peek("OtpPurpose") as string ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            var challengeId = TempData.Peek("OtpChallengeId") as string;
            var purpose = TempData.Peek("OtpPurpose") as string;
            var email = TempData.Peek("OtpEmail") as string;

            if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(purpose) || string.IsNullOrWhiteSpace(email))
                return RedirectToAction("Login");

            if (!ModelState.IsValid || !_otpService.Verify(challengeId, model.Code))
            {
                ModelState.AddModelError(nameof(model.Code), "The verification code is invalid or has expired.");
                ViewData["OtpEmail"] = email;
                model.Purpose = purpose;
                return View(model);
            }

            if (string.Equals(purpose, "reset-password", StringComparison.OrdinalIgnoreCase))
            {
                if (!_otpService.TryGetPending<PendingPasswordReset>(challengeId, out var reset) || reset is null)
                    return RedirectToAction(nameof(ForgotPassword));

                TempData["PasswordResetChallengeId"] = challengeId;
                return RedirectToAction(nameof(ResetPassword));
            }

            if (string.Equals(purpose, "register", StringComparison.OrdinalIgnoreCase))
            {
                if (!_otpService.TryGetPending<PendingRegistration>(challengeId, out var pending) || pending is null)
                    return RedirectToAction("Register");

                if (await _context.Users.AnyAsync(u => u.Email == pending.Email))
                    return RedirectToAction("Login");

                var user = new ApplicationUser
                {
                    Email = pending.Email,
                    PasswordHash = pending.PasswordHash,
                    FirstName = pending.FirstName,
                    LastName = pending.LastName,
                    PhoneNumber = pending.PhoneNumber,
                    DateOfBirth = pending.DateOfBirth,
                    PreferredCurrency = pending.PreferredCurrency,
                    PreferredLanguage = pending.PreferredLanguage,
                    ProfilePictureUrl = "/images/profiles/pfpicon.png",
                    ProfilePic = "/images/profiles/pfpicon.png",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _otpService.RemovePending(challengeId);
                await SignInCustomerAsync(user, rememberMe: false);
                return RedirectToAction("Index", "Home");
            }

            if (!_otpService.TryGetPending<PendingLogin>(challengeId, out var login) || login is null)
                return RedirectToAction("Login");

            var userToSignIn = await _context.Users.FirstOrDefaultAsync(u => u.UserId == login.UserId);
            if (userToSignIn is null)
                return RedirectToAction("Login");

            _otpService.RemovePending(challengeId);
            await SignInCustomerAsync(userToSignIn, login.RememberMe);

            if (!string.IsNullOrEmpty(login.ReturnUrl) && Url.IsLocalUrl(login.ReturnUrl))
                return Redirect(login.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var challengeId = TempData.Peek("PasswordResetChallengeId") as string;
            if (string.IsNullOrWhiteSpace(challengeId) ||
                !_otpService.TryGetPending<PendingPasswordReset>(challengeId, out var reset) || reset is null)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var challengeId = TempData.Peek("PasswordResetChallengeId") as string;
            if (string.IsNullOrWhiteSpace(challengeId) ||
                !_otpService.TryGetPending<PendingPasswordReset>(challengeId, out var reset) || reset is null)
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == reset.UserId);
            if (user is null)
                return RedirectToAction(nameof(ForgotPassword));

            user.PasswordHash = PasswordHashing.Hash(model.NewPassword);
            await _context.SaveChangesAsync();

            _otpService.RemovePending(challengeId);
            TempData.Remove("PasswordResetChallengeId");
            TempData.Remove("OtpChallengeId");
            TempData.Remove("OtpPurpose");
            TempData.Remove("OtpEmail");
            TempData["PasswordResetMessage"] = "Your password has been reset. You can now log in.";

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var email = User?.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            if (User.HasClaim("AccountType", "Staff"))
            {
                var staff = await _context.StaffUsers
                    .Include(s => s.StaffRole)
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (staff == null)
                    return NotFound();

                return View(new TravelPlanningSystem.ViewModels.ProfileViewModel.ProfileViewModel
                {
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    Email = staff.Email,
                    PhoneNumber = staff.PhoneNumber,
                    IsStaff = true,
                    Department = staff.Department,
                    RoleName = staff.StaffRole?.RoleName,
                    ProfilePictureUrl = staff.ProfilePictureUrl
                });
            }

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

            if (User.HasClaim("AccountType", "Staff"))
            {
                var staff = await _context.StaffUsers
                    .Include(s => s.StaffRole)
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (staff == null)
                    return NotFound();

                model.IsStaff = true;
                model.Email = staff.Email;
                ModelState.Remove(nameof(model.Email));

                if (string.IsNullOrWhiteSpace(model.FirstName))
                    ModelState.AddModelError(nameof(model.FirstName), "First name is required.");
                if (string.IsNullOrWhiteSpace(model.LastName))
                    ModelState.AddModelError(nameof(model.LastName), "Last name is required.");
                if (string.IsNullOrWhiteSpace(model.Department))
                    ModelState.AddModelError(nameof(model.Department), "Department is required.");

                if (!ModelState.IsValid)
                {
                    model.RoleName = staff.StaffRole?.RoleName;
                    return View(model);
                }

                staff.FirstName = model.FirstName ?? staff.FirstName;
                staff.LastName = model.LastName ?? staff.LastName;
                staff.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
                staff.Department = model.Department ?? staff.Department;

                if (model.Upload != null && model.Upload.Length > 0)
                {
                    var uploads = Path.Combine(_environment.WebRootPath, "images", "profiles");
                    Directory.CreateDirectory(uploads);

                    var fileName = $"{Guid.NewGuid()}.jpg";
                    var filePath = Path.Combine(uploads, fileName);
                    await using var stream = System.IO.File.Create(filePath);
                    await model.Upload.CopyToAsync(stream);

                    staff.ProfilePictureUrl = $"/images/profiles/{fileName}";
                    staff.ProfilePic = staff.ProfilePictureUrl;
                }

                await _context.SaveChangesAsync();

                var staffClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, staff.StaffId.ToString()),
                    new Claim("AccountType", "Staff"),
                    new Claim(ClaimTypes.Name, $"{staff.FirstName} {staff.LastName}"),
                    new Claim(ClaimTypes.Email, staff.Email),
                    new Claim(ClaimTypes.Role, staff.StaffRole?.RoleName ?? "Staff"),
                    new Claim("ProfilePictureUrl", staff.ProfilePictureUrl ?? string.Empty),
                    new Claim("ProfilePic", staff.ProfilePic ?? string.Empty)
                };

                var staffIdentity = new ClaimsIdentity(staffClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(staffIdentity));

                return RedirectToAction("Profile");
            }

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
            if (!string.IsNullOrWhiteSpace(model.Password))
                user.PasswordHash = PasswordHashing.Hash(model.Password);

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

            var pending = new PendingRegistration
            {
                Email = model.Email,
                PasswordHash = PasswordHashing.Hash(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                PreferredCurrency = model.PreferredCurrency ?? "USD",
                PreferredLanguage = model.PreferredLanguage ?? "en-US",
            };

            return await BeginOtpAsync(model.Email, "register", pending, model);
        }

        private async Task<IActionResult> BeginOtpAsync<T>(string email, string purpose, T pending, object errorModel)
        {
            var challenge = _otpService.CreateChallenge(email, purpose);
            _otpService.StorePending(challenge.ChallengeId, pending);
            TempData["OtpChallengeId"] = challenge.ChallengeId;
            TempData["OtpPurpose"] = purpose;
            TempData["OtpEmail"] = email;

            try
            {
                await _emailSender.SendOtpAsync(email, challenge.Code!, purpose);
            }
            catch (Exception)
            {
                _otpService.RemovePending(challenge.ChallengeId);
                TempData.Remove("OtpChallengeId");
                TempData.Remove("OtpPurpose");
                TempData.Remove("OtpEmail");
                ModelState.AddModelError(string.Empty, "The verification email could not be sent. Check the email configuration and try again.");
                return purpose == "register" ? View("Register", errorModel) : View("Login", errorModel);
            }

            return RedirectToAction("VerifyOtp");
        }

        private async Task SignInCustomerAsync(ApplicationUser user, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim("AccountType", "Customer"),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, "User"),
                new Claim("ProfilePictureUrl", user.ProfilePictureUrl ?? string.Empty),
                new Claim("ProfilePic", user.ProfilePic ?? string.Empty)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe
                });
        }

        private sealed class PendingLogin
        {
            public Guid UserId { get; set; }
            public string? ReturnUrl { get; set; }
            public bool RememberMe { get; set; }
        }

        private sealed class PendingPasswordReset
        {
            public Guid UserId { get; set; }
        }

        private sealed class PendingRegistration
        {
            public string Email { get; set; } = string.Empty;
            public string PasswordHash { get; set; } = string.Empty;
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public string PreferredCurrency { get; set; } = "USD";
            public string PreferredLanguage { get; set; } = "en-US";
        }

        private static string NormalizePhone(string? p)
        {
            return string.IsNullOrEmpty(p) ? string.Empty : new string(p.Where(char.IsDigit).ToArray());
        }

        private static bool IsLocked(DateTime? lockoutUntil)
            => lockoutUntil.HasValue && lockoutUntil.Value > DateTime.UtcNow;

        private static string LockoutMessage(DateTime lockoutUntil)
        {
            var seconds = Math.Max(1, (int)Math.Ceiling((lockoutUntil - DateTime.UtcNow).TotalSeconds));
            return $"Too many failed login attempts. Try again in {seconds} second(s).";
        }

        private static void RecordFailedLogin(ApplicationUser user)
        {
            if (user.LockoutUntil <= DateTime.UtcNow)
                user.LockoutUntil = null;

            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 3)
            {
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(1);
                user.FailedLoginAttempts = 0;
            }
        }

        private static void RecordFailedLogin(StaffUser user)
        {
            if (user.LockoutUntil <= DateTime.UtcNow)
                user.LockoutUntil = null;

            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 3)
            {
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(1);
                user.FailedLoginAttempts = 0;
            }
        }

        private static void ResetLoginFailures(ApplicationUser user)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
        }

        private static void ResetLoginFailures(StaffUser user)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
        }
    }
}
