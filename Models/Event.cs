using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkshellManager.Models
{
    public class Event
    {
        [Key]
        // Primary key
        public int Id { get; set; }
        // Foreign key
        public int LinkshellId { get; set; }
        // Navigation property
        [ForeignKey("LinkshellId")]
        public Linkshell? Linkshell { get; set; }
        public string? EventName { get; set; }
        public string? EventType { get; set; }
        public string? EventLocation { get; set; }
        public string? CreatorUserId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? StartTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? CommencementStartTime { get; set; }

        public double? Duration { get; set; }
        public int? DkpPerHour { get; set; }
        public double? EventDkp { get; set; }
        public string? Details { get; set; }
        public ICollection<Job>? Jobs { get; set; }
        public ICollection<AppUserEvent>? AppUserEvents { get; set; }
        public ICollection<EventLootDetail>? EventLootDetails { get; set; }
        
        [DataType(DataType.DateTime)]
        public DateTime? TimeStamp { get; set; }
    }
}
