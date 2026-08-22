using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelPlanningSystem.Models
{
    [Table("Users")]
    public class ApplicationUser
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        [Required, EmailAddress, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? OAuthProvider { get; set; }

        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Phone, MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Required, MaxLength(3)]
        public string PreferredCurrency { get; set; } = "USD";

        [Required, MaxLength(10)]
        public string PreferredLanguage { get; set; } = "en-US";

        [Required, MaxLength(20)]
        public string LoyaltyTier { get; set; } = "Member";

        public int RewardPoints { get; set; } = 0;

        [Required, MaxLength(20)]
        public string AccountStatus { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(250)]
        public string? ProfilePictureUrl { get; set; }

        // New column: short-friendly profile picture path/filename
        [MaxLength(250)]
        public string? ProfilePic { get; set; }
    }
}
