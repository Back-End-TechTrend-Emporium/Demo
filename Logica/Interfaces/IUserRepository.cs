using Data.Entities;
using Data.Entities.Enums;

namespace Logica.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

        Task<Session> CreateSessionAsync(Session session, CancellationToken cancellationToken = default);
        Task<Session?> GetLastActiveSessionAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<Session?> GetActiveSessionByJtiAsync(string tokenJti, CancellationToken cancellationToken = default);
        Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
        Task UpdateSessionAsync(Session session, CancellationToken cancellationToken = default);
    }
}