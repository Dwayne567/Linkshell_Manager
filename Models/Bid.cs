using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LinkshellManager.Models
{
    public class Bid
    {
        [Key]
        public int Id { get; set; }
        public int AuctionItemId { get; set; }
        [ForeignKey("AuctionItemId")]
        public AuctionItem AuctionItem { get; set; }
        public string CharacterName { get; set; }
        public int BidAmount { get; set; }
    }
}
