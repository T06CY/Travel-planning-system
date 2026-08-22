// Simple view model used by the AccountController for login binding
using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels.LoginViewModel
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email or phone number")]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
