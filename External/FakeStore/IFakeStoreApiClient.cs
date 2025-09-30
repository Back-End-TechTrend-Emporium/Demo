using External.FakeStore.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace External.FakeStore
{
    public interface IFakeStoreApiClient
    {
        Task<IEnumerable<FakeStoreProductResponse>> GetProductsAsync();
        Task<FakeStoreProductResponse?> GetProductByIdAsync(int id);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<IEnumerable<FakeStoreProductResponse>> GetProductsByCategoryAsync(string category);
        Task<FakeStoreCartResponse?> GetCartByIdAsync(int cartId);
        Task<IEnumerable<FakeStoreCartResponse>> GetCartsAsync();
    }
}