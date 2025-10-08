using Data.Entities;
using Data.Entities.Enums;
using Logica.Models;

namespace Logica.Interfaces
{
    public interface IUserService
    {
        // Local user operations
        Task<IEnumerable<GetUserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<User> CreateUserAsync(string email, string username, string password, Role role, CancellationToken cancellationToken = default);
        Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

        
        Task<IEnumerable<GetUserResponse>> GetUsersFromFakeStoreAsync();
        Task<GetUserResponse?> GetUserFromFakeStoreAsync(int id);
        Task<int> SyncAllUsersFromFakeStoreAsync();
        Task<GetUserResponse?> ImportUserFromFakeStoreAsync(int fakeStoreId);
    }
}