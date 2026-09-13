using System.ComponentModel.DataAnnotations;
using TravelPlanningSystem.Models;

namespace TravelPlanningSystem.ViewModels;

public class AdminUserManagementViewModel
{
    public List<ApplicationUser> Members { get; set; } = new();
    public List<StaffUser> StaffUsers { get; set; } = new();
    public List<StaffRole> StaffRoles { get; set; } = new();
}

public class MemberManagementInput
{
    public Guid UserId { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [RegularExpression(@"^\+?[0-9][0-9\s().-]{6,18}[0-9]$", ErrorMessage = "Enter a valid phone number using digits only, with an optional country code."), StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [Required, StringLength(3)]
    public string PreferredCurrency { get; set; } = "USD";

    [Required, StringLength(10)]
    public string PreferredLanguage { get; set; } = "en-US";

    [Required, RegularExpression("^(Member|VIP)$", ErrorMessage = "Loyalty tier must be Member or VIP.")]
    public string LoyaltyTier { get; set; } = "Member";

    [Range(0, int.MaxValue)]
    public int RewardPoints { get; set; }

    [Required, StringLength(20)]
    public string AccountStatus { get; set; } = "Active";

    [DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string? Password { get; set; }
}

public class StaffManagementInput
{
    public Guid StaffId { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [RegularExpression(@"^\+?[0-9][0-9\s().-]{6,18}[0-9]$", ErrorMessage = "Enter a valid phone number using digits only, with an optional country code."), StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Department must be between 2 and 100 characters.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Select a staff role.")]
    public Guid RoleId { get; set; }

    [Range(1, 10)]
    public int AccessLevel { get; set; } = 1;

    [Required, RegularExpression("^(Active|Inactive|Suspended)$", ErrorMessage = "Select a valid account status."), StringLength(20)]
    public string Status { get; set; } = "Active";

    [DataType(DataType.Password), StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string? Password { get; set; }
}
