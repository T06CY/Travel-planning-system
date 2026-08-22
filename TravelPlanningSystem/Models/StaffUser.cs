using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models
{
    [Table("StaffUsers")]
    public class StaffUser
    {
        [Key]
        public Guid StaffId { get; set; } = Guid.NewGuid();

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? MfaSecret { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Department { get; set; } = string.Empty;

        public Guid? ManagedPropertyId { get; set; }

        public int AccessLevel { get; set; } = 1;

        [MaxLength(45)]
        public string? LastLoginIp { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key for Role
        [Required]
        public Guid RoleId { get; set; }

        [ForeignKey("RoleId")]
        public virtual StaffRole StaffRole { get; set; } = null!;
    }
}
