using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkshellManager.Models
{
    public class AuctionItem
    {
        [Key]
        // Primary key
        public int Id { get; set; }
        // Foreign key
        public int? AuctionId { get; set; }
        // Navigation property
        [ForeignKey("AuctionId")]
        public Auction? Auction { get; set; }
        public string? ItemName { get; set; }
        public string? ItemType { get; set; }
        public int? StartingBidDkp { get; set; }
        public List<Bid>? Bids { get; set; }
        public int? CurrentHighestBid { get; set; }
        public string? CurrentHighestBidder { get; set; }
        public int? EndingBidDkp { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartTime { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndTime { get; set; }

        public string? Status { get; set; }
        public string? Notes { get; set; }
    }
}
