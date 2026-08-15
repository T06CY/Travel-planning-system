using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("ActivityPhotos")]
public class ActivityPhoto
{
    [Key]
    public int ActivityPhotoId { get; set; }

    [Required]
    public int ActivityId { get; set; }
    public Activity? Activity { get; set; }

    [Required, StringLength(350)]
    public string PhotoUrl { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Caption { get; set; }

    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}
