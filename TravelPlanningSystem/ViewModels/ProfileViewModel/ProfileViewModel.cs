using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels.ProfileViewModel
{
    public class ProfileViewModel
    {
        [Display(Name = "First name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last name")]
        public string? LastName { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Profile picture URL")]
        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Upload profile picture")]
        public IFormFile? Upload { get; set; }
    }
}
