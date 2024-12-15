using LinkshellManager.Models;

namespace LinkshellManager.ViewModels
{
    public class AuctionHistoryViewModel
    {
        public AuctionHistory AuctionHistory { get; set; }
        public List<AuctionItem> AuctionItems { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
