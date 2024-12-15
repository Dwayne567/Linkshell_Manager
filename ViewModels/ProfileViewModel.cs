using LinkshellManager.Models;

namespace LinkshellManager.ViewModels
{
    public class ProfileViewModel
    {
        public string? CharacterName { get; set; }
        public IFormFile? ProfileImage { get; set; }
        public byte[]? ProfileImageData { get; set; }
    }
}
