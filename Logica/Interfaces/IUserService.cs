using Data.Entities;
using Data.Entities.Enums;
using Logica.Models;

namespace Logica.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default);
        Task<UserResponse?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<(UserResponse? User, string? Error)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
        Task<(UserResponse? User, string? Error)> UpdateUserAsync(string username, UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task<(bool Success, string? Error)> DeleteUsersAsync(DeleteUsersRequest request, CancellationToken cancellationToken = default);
    }
}