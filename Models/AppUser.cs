using Microsoft.AspNetCore.Identity;

namespace LinkshellManager.Models
{
    public class AppUser : IdentityUser
    {
        public string? CharacterName { get; set; }
        public string? TimeZone { get; set; }
        public int? PrimaryLinkshellId { get; set; }
        public string? PrimaryLinkshellName { get; set; }
        public ICollection<AppUserLinkshell>? AppUserLinkshells { get; set; }
        public ICollection<AppUserEvent>? AppUserEvents { get; set; }
        public ICollection<AppUserMessage>? AppUserMessages { get; set; }
        public ICollection<Notification>? Notifications { get; set; }

        public byte[]? ProfileImage { get; set; }
    }
}