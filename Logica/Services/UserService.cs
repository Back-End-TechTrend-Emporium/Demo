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

        public async Task<IEnumerable<GetUserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var usuario = await _userRepository.GetAllAsync(cancellationToken);

            var model = usuario.Select(u => new GetUserResponse
            {
                Id = u.Id,
                Email = u.Email,
                Username = u.Username,
            }).ToList();
            return model;
        }

        public async Task<User> CreateUserAsync(string email, string username, string password, Role role, CancellationToken cancellationToken = default)
        {
            // Validaciones de negocio
            if (await _userRepository.EmailExistsAsync(email, cancellationToken))
                throw new InvalidOperationException("El e-mail ya existe");

            if (await _userRepository.UsernameExistsAsync(username, cancellationToken))
                throw new InvalidOperationException("El nombre de usuario ya existe");

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