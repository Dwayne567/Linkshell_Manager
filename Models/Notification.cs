using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkshellManager.Models
{
    public class Notification
    {
        [Key]
        // Primary key
        public int Id { get; set; }

        // Foreign key
        public string? AppUserId { get; set; }

        // Navigation property
        [ForeignKey("AppUserId")]
        public AppUser? AppUser { get; set; }

        public String? NotificationType { get; set; }
        public string? CharacterNameSender { get; set; }
        public String? NotificationDetails { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
