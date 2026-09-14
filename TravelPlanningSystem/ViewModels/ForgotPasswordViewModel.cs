using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels;

public sealed class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    [StringLength(150)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
