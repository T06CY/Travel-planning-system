using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("ActivityCategories")]
public class ActivityCategory
{
    [Key]
    public int ActivityCategoryId { get; set; }

    [Required, StringLength(60)]
    [Display(Name = "Category Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
