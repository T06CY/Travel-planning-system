using System;
using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels.RegisterViewModel
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "First name")]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last name")]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [MaxLength(150)]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        [MaxLength(20)]
        [RegularExpression(@"^\+?[0-9]{6,20}$", ErrorMessage = "Phone number must start with an optional '+' and contain 6-20 digits, e.g. +60123456789")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Preferred currency")]
        [MaxLength(3)]
        public string PreferredCurrency { get; set; } = "USD";

        [Display(Name = "Preferred language")]
        [MaxLength(10)]
        public string PreferredLanguage { get; set; } = "en-US";
    }
}
