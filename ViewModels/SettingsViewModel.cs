using LinkshellManager.Models;
using System.Collections.Generic;

namespace LinkshellManager.ViewModels
{
    public class SettingsViewModel
    {
        public List<Linkshell> Linkshells { get; set; }
        public int? SelectedLinkshellId { get; set; }
    }
}