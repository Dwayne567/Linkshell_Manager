using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkshellManager.Models
{
    public class Invite
    {
        [Key]
        public int Id { get; set; }

        public string AppUserId { get; set; }
        [ForeignKey("AppUserId")]
        public AppUser AppUser { get; set; }

        public int LinkshellId { get; set; }
        [ForeignKey("LinkshellId")]
        public Linkshell Linkshell { get; set; }

        public string Status { get; set; } // Pending, Accepted, Declined
    }
}