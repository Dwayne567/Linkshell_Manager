using LinkshellManager.Models;

namespace LinkshellManager.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalMembers { get; set; }
        public int TotalItems { get; set; }
        public int Revenue { get; set; }
        public List<Linkshell>? Linkshells { get; set; }
        public int? SelectedLinkshellId { get; set; }
        public List<AppUserLinkshell>? Members { get; set; }
        public List<AuctionHistory>? RecentSales { get; set; }
        public List<Tod>? Tods { get; set; }
        public List<Event>? Events { get; set; }
        public List<EventHistory>? EventHistories { get; set; }
        public Dictionary<string, double>? HnmClaimsPercentages { get; set; }
        public List<TodLootDetail>? TodLootDetails { get; set; }
        public List<EventLootDetail>? EventLootDetails { get; set; }
        public int[] MembersData { get; set; } // Add this line
        public int[] ItemsData { get; set; } // Add this line
        public int[] RevenueData { get; set; } // Add this line
        public IList<string> Notifications { get; set; } = new List<string>();
        public List<Announcement>? RecentAnnouncements { get; set; }
    }
}