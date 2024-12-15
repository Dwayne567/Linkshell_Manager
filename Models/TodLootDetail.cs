using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkshellManager.Models
{
    public class TodLootDetail
    {
        [Key]
        // Primary key
        public int Id { get; set; }

        // Foreign key
        public int? TodId { get; set; }

        // Navigation property
        [ForeignKey("TodId")]
        public Tod? Tod { get; set; }
        public string? ItemName { get; set; }
        public string? ItemWinner { get; set; }
        public int? WinningDkpSpent { get; set; }
    }
}
