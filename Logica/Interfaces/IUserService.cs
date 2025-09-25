using Data.Entities;
using Data.Entities.Enums;

namespace Logica.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<User> CreateUserAsync(string email, string username, string password, Role role, CancellationToken cancellationToken = default);
        Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}