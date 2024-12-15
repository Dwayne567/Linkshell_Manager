using Microsoft.EntityFrameworkCore;
using LinkshellManager.Data;
using LinkshellManager.Interfaces;
using LinkshellManager.Models;

namespace LinkshellManager.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool Add(AppUser user)
        {
            _context.Users.Add(user);
            return Save();
        }

        public bool Save()
        {
            var saved = _context.SaveChanges();
            return saved > 0 ? true : false;
        }

        public async Task<IEnumerable<AppUser>> GetAllUsers()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<AppUser?> GetUserById(string id)
        {
            return await _context.Users.FindAsync(id);
        }

        public bool Update(AppUser user)
        {
            _context.Update(user);
            return Save();
        }

        public bool Delete(AppUser user)
        {
            var existingUser = _context.Users.Find(user.Id);
            if (existingUser == null) return false;
            _context.Users.Remove(existingUser);
            return Save();
        }
    }
}
