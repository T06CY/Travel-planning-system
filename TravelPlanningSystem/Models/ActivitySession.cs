using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models;

[Table("ActivitySessions")]
public class ActivitySession
{
    [Key]
    public int ActivitySessionId { get; set; }

    [Required]
    public int ActivityId { get; set; }
    public Activity? Activity { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Session Date")]
    public DateTime SessionDate { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; }

    [Range(1, 500)]
    public int Capacity { get; set; }

    [Range(0, 500)]
    [Display(Name = "Available Slots")]
    public int AvailableSlots { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ActivityBooking> Bookings { get; set; } = new List<ActivityBooking>();
}
