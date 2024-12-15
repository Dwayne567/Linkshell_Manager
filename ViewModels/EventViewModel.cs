using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LinkshellManager.Models;

namespace LinkshellManager.ViewModels
{
    public class EventViewModel
    {
        public int LinkshellId { get; set; }
        public List<Linkshell>? Linkshells { get; set; }
        public List<string> LinkshellMembers { get; set; }
        public Event Event { get; set; }
        public double? EventDkp { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? StartTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }

        public List<Job> Jobs { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? CommencementStartTime { get; set; }
        public int? DkpPerHour { get; set; }

        public string? CreatorCharacterName { get; set; }
        public List<AppUserEvent>? AppUserEvents { get; set; }
        public List<AppUserEvent>? BreakUsers { get; set; }
        public List<EventLootDetail>? EventLootDetails { get; set; }

        public EventViewModel()
        {
            Event = new Event();
            Linkshells = new List<Linkshell>();
            Jobs = new List<Job>();
            AppUserEvents = new List<AppUserEvent>();
            BreakUsers = new List<AppUserEvent>();
            EventLootDetails = new List<EventLootDetail>();
            LinkshellMembers = new List<string>();
        }

    }
}