using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models
{
    [Table("StaffRoles")]
    public class StaffRole
    {
        [Key]
        public Guid RoleId { get; set; } = Guid.NewGuid();

        [Required, MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        public virtual ICollection<StaffUser> StaffUsers { get; set; } = new List<StaffUser>();
    }
}
