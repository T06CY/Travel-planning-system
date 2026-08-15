using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TravelPlanningSystem.ViewModels;

public class ActivityFormViewModel : IValidatableObject
{
    public int ActivityId { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Activity Name")]
    public string ActivityName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Destination { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Location { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 999999)]
    [Display(Name = "Price per Person (RM)")]
    public decimal PricePerPerson { get; set; }

    [Range(0.5, 72)]
    [Display(Name = "Duration (Hours)")]
    public double DurationHours { get; set; }

    [Range(1, 100)]
    public int MinimumParticipants { get; set; } = 1;

    [Range(1, 500)]
    public int MaximumParticipants { get; set; } = 20;

    [Range(0, 100)]
    public int MinimumAge { get; set; }

    public string? IncludedItems { get; set; }
    public string? WhatToBring { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    public int ActivityCategoryId { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public List<IFormFile> Photos { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaximumParticipants < MinimumParticipants)
            yield return new ValidationResult("Maximum participants must be greater than or equal to minimum participants.", [nameof(MaximumParticipants)]);

        foreach (var photo in Photos)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(Path.GetExtension(photo.FileName).ToLowerInvariant()))
                yield return new ValidationResult("Only JPG, PNG and WEBP images are allowed.", [nameof(Photos)]);
            if (photo.Length > 5 * 1024 * 1024)
                yield return new ValidationResult("Each image must be 5 MB or smaller.", [nameof(Photos)]);
        }
    }
}
