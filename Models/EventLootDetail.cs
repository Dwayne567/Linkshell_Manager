using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkshellManager.Models
{
    public class EventLootDetail
    {
        [Key]
        // Primary key
        public int Id { get; set; }

        // Foreign key
        public int EventId { get; set; }

        // Navigation property
        [ForeignKey("EventId")]
        public Event? Event { get; set; }
        public string? ItemName { get; set; }
        public string? ItemWinner { get; set; }
        public int? WinningDkpSpent { get; set; }
    }
}
