using External.FakeStore.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace External.FakeStore
{
    public interface IFakeStoreApiService
    {
        // Products
        Task<IEnumerable<FakeStoreProductResponse>> GetProductsAsync();
        Task<FakeStoreProductResponse?> GetProductByIdAsync(int id);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<IEnumerable<FakeStoreProductResponse>> GetProductsByCategoryAsync(string category);
        
        // Carts
        Task<IEnumerable<FakeStoreCartResponse>> GetCartsAsync();
        Task<FakeStoreCartResponse?> GetCartByIdAsync(int cartId);
        Task<FakeStoreCartResponse?> CreateCartAsync(FakeStoreCartCreateRequest cartRequest);
        Task<FakeStoreCartResponse?> UpdateCartAsync(int cartId, FakeStoreCartUpdateRequest cartRequest);
        Task<FakeStoreCartResponse?> DeleteCartAsync(int cartId);

        // Users
        Task<IEnumerable<FakeStoreUserResponse>> GetUsersAsync();
        Task<FakeStoreUserResponse?> GetUserByIdAsync(int id);
        Task<FakeStoreUserResponse?> CreateUserAsync(FakeStoreUserCreateRequest userRequest);
        Task<FakeStoreUserResponse?> UpdateUserAsync(int userId, FakeStoreUserCreateRequest userRequest);
        Task<FakeStoreUserResponse?> DeleteUserAsync(int userId);
    }
}