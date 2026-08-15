using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("Activities")]
public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Activity Name")]
    public string ActivityName { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Destination { get; set; } = string.Empty;

    [Required, StringLength(180)]
    public string Location { get; set; } = string.Empty;

    [Required, StringLength(2000), DataType(DataType.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    [Range(0.01, 999999)]
    [Display(Name = "Price per Person")]
    public decimal PricePerPerson { get; set; }

    [Range(0.5, 72)]
    [Display(Name = "Duration (hours)")]
    public double DurationHours { get; set; }

    [Range(1, 100)]
    [Display(Name = "Minimum Participants")]
    public int MinimumParticipants { get; set; } = 1;

    [Range(1, 500)]
    [Display(Name = "Maximum Participants")]
    public int MaximumParticipants { get; set; } = 20;

    [Range(0, 100)]
    [Display(Name = "Minimum Age")]
    public int MinimumAge { get; set; }

    [StringLength(300)]
    [Display(Name = "What's Included")]
    public string? IncludedItems { get; set; }

    [StringLength(300)]
    [Display(Name = "What to Bring")]
    public string? WhatToBring { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Required]
    public int ActivityCategoryId { get; set; }
    public ActivityCategory? ActivityCategory { get; set; }

    public ICollection<ActivitySession> Sessions { get; set; } = new List<ActivitySession>();
    public ICollection<ActivityPhoto> Photos { get; set; } = new List<ActivityPhoto>();
    public ICollection<ActivityReview> Reviews { get; set; } = new List<ActivityReview>();
}
