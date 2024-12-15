using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkshellManager.Models
{
    public class AuctionHistory
    {
        [Key]
        // Primary key
        public int Id { get; set; }
        // Foreign key
        public int LinkshellId { get; set; }
        // Navigation property
        [ForeignKey("LinkshellId")]
        public Linkshell? Linkshell { get; set; }
        public string? AuctionTitle { get; set; }
        public string? CreatedBy { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartTime { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndTime { get; set; }

        public ICollection<AuctionItem>? AuctionItems { get; set; }
    }
}
