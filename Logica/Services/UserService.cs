using Data.Entities;
using Data.Entities.Enums;
using External.FakeStore;
using Logica.Interfaces;
using Logica.Mappers;
using Logica.Models;
using System.Text.Json;

namespace Logica.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IFakeStoreApiService _fakeStoreApiService;
        private readonly IExternalMappingRepository _externalMappingRepository;

        public UserService(
            IUserRepository userRepository,
            IFakeStoreApiService fakeStoreApiService,
            IExternalMappingRepository externalMappingRepository)
        {
            _userRepository = userRepository;
            _fakeStoreApiService = fakeStoreApiService;
            _externalMappingRepository = externalMappingRepository;
        }

        // Local user operations
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

        // FakeStore user operations
        public async Task<IEnumerable<GetUserResponse>> GetUsersFromFakeStoreAsync()
        {
            var fakeStoreUsers = await _fakeStoreApiService.GetUsersAsync();
            return fakeStoreUsers.Select(FakeStoreUserMapper.ToUserDto);
        }

        public async Task<GetUserResponse?> GetUserFromFakeStoreAsync(int id)
        {
            var fakeStoreUser = await _fakeStoreApiService.GetUserByIdAsync(id);
            return fakeStoreUser != null ? FakeStoreUserMapper.ToUserDto(fakeStoreUser) : null;
        }

        // Sync operations
        public async Task<int> SyncAllUsersFromFakeStoreAsync()
        {
            var fakeStoreUsers = await _fakeStoreApiService.GetUsersAsync();
            int importedCount = 0;

            foreach (var fakeStoreUser in fakeStoreUsers)
            {
                try
                {
                    // Check if this user is already imported
                    var existingMapping = await _externalMappingRepository.GetByExternalIdAsync(
                        ExternalSource.FakeStore, 
                        "USER", 
                        fakeStoreUser.Id.ToString());

                    if (existingMapping != null)
                        continue; // Skip already imported users

                    // Check if email or username already exists
                    if (await _userRepository.EmailExistsAsync(fakeStoreUser.Email))
                        continue;

                    if (await _userRepository.UsernameExistsAsync(fakeStoreUser.Username))
                        continue;

                    // Create the user
                    var user = FakeStoreUserMapper.ToUser(fakeStoreUser);
                    var createdUser = await _userRepository.AddAsync(user);

                    // Create the external mapping
                    var mapping = new ExternalMapping
                    {
                        Source = ExternalSource.FakeStore,
                        SourceType = "USER",
                        SourceId = fakeStoreUser.Id.ToString(),
                        InternalId = createdUser.Id,
                        SnapshotJson = JsonSerializer.Serialize(fakeStoreUser),
                        ImportedAt = DateTime.UtcNow
                    };

                    await _externalMappingRepository.CreateAsync(mapping);
                    importedCount++;
                }
                catch (Exception)
                {
                    // Log error and continue with next user
                    continue;
                }
            }

            return importedCount;
        }

        public async Task<GetUserResponse?> ImportUserFromFakeStoreAsync(int fakeStoreId)
        {
            var fakeStoreUser = await _fakeStoreApiService.GetUserByIdAsync(fakeStoreId);
            if (fakeStoreUser == null)
                return null;

            // Check if already imported
            var existingMapping = await _externalMappingRepository.GetByExternalIdAsync(
                ExternalSource.FakeStore, 
                "USER", 
                fakeStoreId.ToString());

            if (existingMapping != null)
            {
                var existingUser = await _userRepository.GetByIdAsync(existingMapping.InternalId);
                return existingUser != null ? FakeStoreUserMapper.ToUserDto(existingUser) : null;
            }

            // Check if email or username already exists
            if (await _userRepository.EmailExistsAsync(fakeStoreUser.Email))
                throw new InvalidOperationException($"El email {fakeStoreUser.Email} ya existe en el sistema");

            if (await _userRepository.UsernameExistsAsync(fakeStoreUser.Username))
                throw new InvalidOperationException($"El username {fakeStoreUser.Username} ya existe en el sistema");

            // Create the user
            var user = FakeStoreUserMapper.ToUser(fakeStoreUser);
            var createdUser = await _userRepository.AddAsync(user);

            // Create the external mapping
            var mapping = new ExternalMapping
            {
                Source = ExternalSource.FakeStore,
                SourceType = "USER",
                SourceId = fakeStoreId.ToString(),
                InternalId = createdUser.Id,
                SnapshotJson = JsonSerializer.Serialize(fakeStoreUser),
                ImportedAt = DateTime.UtcNow
            };

            await _externalMappingRepository.CreateAsync(mapping);

            return FakeStoreUserMapper.ToUserDto(createdUser);
        }
    }
}