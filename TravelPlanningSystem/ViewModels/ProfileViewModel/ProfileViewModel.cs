using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels.ProfileViewModel
{
    public class ProfileViewModel
    {
        [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [Required, StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [Required, EmailAddress, StringLength(150)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [RegularExpression(@"^\+?[0-9][0-9\s().-]{6,18}[0-9]$", ErrorMessage = "Enter a valid phone number using digits only, with an optional country code.")]
        [StringLength(20)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required, StringLength(3)]
        [Display(Name = "Preferred currency")]
        public string PreferredCurrency { get; set; } = "USD";

        [Required, StringLength(10)]
        [Display(Name = "Preferred language")]
        public string PreferredLanguage { get; set; } = "en-US";

        [Display(Name = "Profile picture URL")]
        public string? ProfilePictureUrl { get; set; }

        public bool IsStaff { get; set; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Department must be between 2 and 100 characters.")]
        [Display(Name = "Department")]
        public string? Department { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The password confirmation does not match.")]
        [Display(Name = "Confirm new password")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Role")]
        public string? RoleName { get; set; }

        [Display(Name = "Upload profile picture")]
        public IFormFile? Upload { get; set; }
    }
}
