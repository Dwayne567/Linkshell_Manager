using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkshellManager.Models
{
    public class AppUserEvent
    {
        [Key]
        // Primary key
        public int Id { get; set; }

        // Foreign key
        public string? AppUserId { get; set; }

        // Navigation property
        [ForeignKey("AppUserId")]
        public AppUser? AppUser { get; set; }

        // Foreign key
        public int EventId { get; set; }

        // Navigation property
        [ForeignKey("EventId")]
        public Event? Event { get; set; }
        public string? CharacterName { get; set; }
        public string? JobName { get; set; }
        public string? SubJobName { get; set; }
        public string? JobType { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? StartTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        public double? Duration { get; set; }
        public double? EventDkp { get; set; }
        public bool IsQuickJoin { get; set; }
        public bool? IsVerified { get; set; }
        public string? Proctor {  get; set; }
        public bool? IsOnBreak { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? PauseTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ResumeTime { get; set; }
    }
}
