using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkshellManager.Models
{
    public class Job
    {
        [Key]
        // Primary key
        public int Id { get; set; }
        // Foreign key
        public int EventId { get; set; }

        // Navigation property
        [ForeignKey("EventId")]
        public Event? Event { get; set; }
        public string? JobName { get; set; }
        public string? SubJobName { get; set; }
        public string? JobType { get; set; }
        public int? Quantity { get; set; }
        public int? SignedUp { get; set; }

        public List<string>? Enlisted { get; set; }
        public string? Details { get; set; }
    }
}
