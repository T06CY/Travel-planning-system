using System.ComponentModel.DataAnnotations;

namespace TravelPlanningSystem.ViewModels;

public class ActivityBookingViewModel
{
    public int ActivityId { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Please choose an available session.")]
    [Display(Name = "Activity Session")]
    public int ActivitySessionId { get; set; }


    [Range(1, 500,
        ErrorMessage = "Number of participants must be between 1 and 500.")]
    [Display(Name = "Number of Participants")]
    public int ParticipantCount { get; set; } = 1;


    [Required(ErrorMessage = "Contact name is required.")]
    [StringLength(100,
        ErrorMessage = "Contact name cannot exceed 100 characters.")]
    [Display(Name = "Contact Name")]
    public string ContactName { get; set; } = string.Empty;


    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(120,
        ErrorMessage = "Email address cannot exceed 120 characters.")]
    [Display(Name = "Email Address")]
    public string ContactEmail { get; set; } = string.Empty;


    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(
        @"^\d{10,11}$",
        ErrorMessage = "Phone number must contain 10 to 11 digits only.")]
    [Display(Name = "Phone Number")]
    public string ContactPhone { get; set; } = string.Empty;


    public string ActivityName { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public decimal PricePerPerson { get; set; }

    public int AvailableSlots { get; set; }
}