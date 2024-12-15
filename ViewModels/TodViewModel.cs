using LinkshellManager.Models;
using System.Collections.Generic;

namespace LinkshellManager.ViewModels
{
    public class TodManagerViewModel
    {
        public int LinkshellId { get; set; }
        public List<Linkshell> Linkshells { get; set; }
        public Tod Tod { get; set; }
        public List<Tod> TodItems { get; set; }
        public List<TodLootDetail> TodLootDetails { get; set; }
        public bool NoLoot { get; set; }
        public IList<string> Notifications { get; set; } = new List<string>(); // Add this line

        public TodManagerViewModel()
        {
            Linkshells = new List<Linkshell>();
            TodItems = new List<Tod>(); // Initialize TodItems
            TodLootDetails = new List<TodLootDetail>(); // Initialize TodLootDetails
        }
    }
}