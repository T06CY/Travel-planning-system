using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels;

public sealed class VerifyOtpViewModel
{
    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Enter the six-digit verification code.")]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "The verification code must contain six digits.")]
    [Display(Name = "Verification code")]
    public string Code { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;
}
