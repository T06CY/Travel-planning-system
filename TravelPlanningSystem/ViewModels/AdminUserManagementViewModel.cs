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

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [Required, StringLength(3)]
    public string PreferredCurrency { get; set; } = "USD";

    [Required, StringLength(10)]
    public string PreferredLanguage { get; set; } = "en-US";

    [Required, StringLength(20)]
    public string LoyaltyTier { get; set; } = "Member";

    [Range(0, int.MaxValue)]
    public int RewardPoints { get; set; }

    [Required, StringLength(20)]
    public string AccountStatus { get; set; } = "Active";

    [DataType(DataType.Password)]
    public string? Password { get; set; }
}

public class StaffManagementInput
{
    public Guid StaffId { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required]
    public Guid RoleId { get; set; }

    [Range(1, 10)]
    public int AccessLevel { get; set; } = 1;

    [Required, StringLength(20)]
    public string Status { get; set; } = "Active";

    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
