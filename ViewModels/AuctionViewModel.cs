using LinkshellManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LinkshellManager.ViewModels
{
    public class AuctionViewModel
    {
        public int LinkshellId { get; set; }
        public List<Linkshell>? Linkshells { get; set; }
        public Auction Auction { get; set; }
        public List<AuctionItem> AuctionItems { get; set; }
        public AuctionViewModel()
        {
            Linkshells = new List<Linkshell>();
            Auction = new Auction();
            AuctionItems = new List<AuctionItem>();
        }
    }
}