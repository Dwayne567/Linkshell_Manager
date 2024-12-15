using System.ComponentModel.DataAnnotations;
using LinkshellManager.Models;

namespace LinkshellManager.ViewModels
{
    public class LinkshellViewModel
    {
        [Required]
        public string LinkshellName { get; set; }
        public string AppUserId { get; set; }
    }
}