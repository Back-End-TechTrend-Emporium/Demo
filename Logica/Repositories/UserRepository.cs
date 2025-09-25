using Data;
using Data.Entities;
using Logica.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Logica.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.FindAsync([id], cancellationToken);
        }

        public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Users.ToListAsync(cancellationToken);
        }

        public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            return user;
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);
        }
    }
}