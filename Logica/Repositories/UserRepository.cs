using Data;
using Data.Entities;
using Data.Entities.Enums;
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
            // Usa la forma compatible con todas las versiones de C#
            return await _context.Users.FindAsync(new object?[] { id }, cancellationToken);
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

        // IMPLEMENTACIÓN EXACTA de la firma de la interfaz
        public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);
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

        public async Task<Session> CreateSessionAsync(Session session, CancellationToken cancellationToken = default)
        {
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync(cancellationToken);
            return session;
        }

        public async Task<Session?> GetLastActiveSessionAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Sessions
                .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Session?> GetActiveSessionByJtiAsync(string tokenJti, CancellationToken cancellationToken = default)
        {
            // Si el JTI se almacena hasheado, hashea 'tokenJti' antes de comparar.
            return await _context.Sessions
                .FirstOrDefaultAsync(s => s.TokenJtiHash == tokenJti && s.Status == SessionStatus.Active, cancellationToken);
        }

        public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateSessionAsync(Session session, CancellationToken cancellationToken = default)
        {
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
