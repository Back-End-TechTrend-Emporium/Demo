using Data.Entities;
using Data.Entities.Enums;
using Logica.Interfaces;

namespace Logica.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            return await _userRepository.GetAllAsync(cancellationToken);
        }

        public async Task<User> CreateUserAsync(string email, string username, string password, Role role, CancellationToken cancellationToken = default)
        {
            // Validaciones de negocio
            if (await _userRepository.EmailExistsAsync(email, cancellationToken))
                throw new InvalidOperationException("Email already exists");

            if (await _userRepository.UsernameExistsAsync(username, cancellationToken))
                throw new InvalidOperationException("Username already exists");

            var user = new User
            {
                Email = email.ToLower(),
                Username = username.ToLower(),
                PasswordHash = password, // Por ahora sin hash para pruebas
                Role = role,
                IsActive = true
            };

            return await _userRepository.AddAsync(user, cancellationToken);
        }

        public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _userRepository.GetByIdAsync(id, cancellationToken);
        }
    }
}