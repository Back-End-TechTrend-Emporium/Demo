using Data.Entities;
using Data.Entities.Enums;
using Logica.Interfaces;
using Logica.Models;

namespace Logica.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetAllAsync(cancellationToken);
            // Mapeamos a UserResponse para no exponer datos sensibles como el PasswordHash
            return users.Select(u => new UserResponse(u.Id, u.Name, u.Email, u.Username, u.Role.ToString()));
        }

        public async Task<UserResponse?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return null;
            }
            // Mapeamos a UserResponse
            return new UserResponse(user.Id, user.Name, user.Email, user.Username, user.Role.ToString());
        }

        public async Task<(UserResponse? User, string? Error)> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken) || await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
            {
                return (null, "El email o nombre de usuario ya existe.");
            }

            if (!Enum.IsDefined(typeof(Role), request.Role))
            {
                return (null, "El rol especificado no es válido.");
            }

            var user = new User
            {
                Name = request.Name,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role
            };

            var addedUser = await _userRepository.AddAsync(user, cancellationToken);
            var response = new UserResponse(addedUser.Id, addedUser.Name, addedUser.Email, addedUser.Username, addedUser.Role.ToString());
            return (response, null);
        }

        public async Task<(UserResponse? User, string? Error)> UpdateUserAsync(string username, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
            if (user == null)
            {
                return (null, "Usuario no encontrado.");
            }

            // Actualizamos solo los campos que vienen en la petición
            if (!string.IsNullOrEmpty(request.Name)) user.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
            if (!string.IsNullOrEmpty(request.Password)) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            if (!string.IsNullOrEmpty(request.Role) && Enum.TryParse<Role>(request.Role, true, out var role))
            {
                user.Role = role;
            }

            await _userRepository.UpdateUserAsync(user, cancellationToken);
            var response = new UserResponse(user.Id, user.Name, user.Email, user.Username, user.Role.ToString());
            return (response, null);
        }

        public async Task<(bool Success, string? Error)> DeleteUsersAsync(DeleteUsersRequest request, CancellationToken cancellationToken = default)
        {
            var usersToDelete = await _userRepository.GetUsersByUsernamesAsync(request.Usernames, cancellationToken);
            if (usersToDelete.Count == 0)
            {
                return (false, "Ninguno de los usuarios especificados fue encontrado.");
            }

            await _userRepository.DeleteUsersAsync(usersToDelete, cancellationToken);
            return (true, null);
        }
    }
}