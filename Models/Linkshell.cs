using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkshellManager.Models
{
    public class Linkshell
    {
        [Key]
        // Primary key
        public int Id { get; set; }

        // Foreign key
        public string? AppUserId { get; set; }

        // Navigation property
        [ForeignKey("AppUserId")]
        public AppUser? AppUser { get; set; }

        public string? LinkshellName { get; set; }
        public int? TotalMembers { get; set; }
        public int? TotalItems { get; set; }
        public int? Revenue { get; set; }
        public int? LinkshellCurrentDkp { get; set; }
        public int? LinkshellTotalDkp { get; set; }
        public string? Status { get; set; }
        public string? Details { get; set; }

        public ICollection<AppUserLinkshell>? AppUserLinkshells { get; set; }
        public ICollection<AppUserEvent>? AppUserEvents { get; set; }
        public ICollection<Item>? Items { get; set; }
        public ICollection<Income>? Incomes { get; set; }
        public ICollection<Tod>? Tods { get; set; }
        public ICollection<Event>? Events { get; set; }
        public ICollection<EventHistory>? EventHistories { get; set; }
        public ICollection<Auction>? Auctions { get; set; }
        public ICollection<Announcement>? Announcements { get; set; }
        public ICollection<Rule>? Rules { get; set; }
    }
}