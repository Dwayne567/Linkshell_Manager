using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkshellManager.Models
{
    public class AppUserLinkshell
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
        public int LinkshellId { get; set; }

        // Navigation property
        [ForeignKey("LinkshellId")]
        public Linkshell? Linkshell { get; set; }

        public string? CharacterName { get; set; }
        public string? Rank { get; set; }
        public string? Status { get; set; }
        public double? LinkshellDkp { get; set; }
        public ICollection<DkpAudit>? DkpAudits { get; set; }
        public ICollection<DkpLedger>? DkpLedgers { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? DateJoined { get; set; }
    }
}
