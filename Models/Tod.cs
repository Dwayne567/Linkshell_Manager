using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkshellManager.Models
{
    public class Tod
    {
        [Key]
        // Primary key
        public int Id { get; set; }
        // Foreign key
        public int LinkshellId { get; set; }
        // Navigation property
        [ForeignKey("LinkshellId")]
        public Linkshell? Linkshell { get; set; }
        public string? MonsterName { get; set; }
        public int? DayNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Time { get; set; }
        public bool Claim { get; set; }

        public string? Cooldown { get; set; }

        [DataType(DataType.Date)]
        public DateTime? RepopTime { get; set; }
        public string? Interval { get; set; }
        
        [DataType(DataType.DateTime)]
        public DateTime? TimeStamp { get; set; }

        public int? TotalClaims { get; set; }

        public int? TotalTods { get; set; }
        public ICollection<TodLootDetail>? TodLootDetails { get; set; }

    }
}
