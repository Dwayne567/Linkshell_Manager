using LinkshellManager.Models;

namespace LinkshellManager.Interfaces
{
    public interface IUserRepository
    {
        bool Add(AppUser user);
        bool Save();
        Task<IEnumerable<AppUser>> GetAllUsers();
        Task<AppUser?> GetUserById(string id);
        bool Update(AppUser user);
        bool Delete(AppUser user);
    }
}
