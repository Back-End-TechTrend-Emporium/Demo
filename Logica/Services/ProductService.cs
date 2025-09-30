using External.FakeStore;
using External.FakeStore.Models;
using External.FakeStore.Mappers;
using Logica.Models;
using Data.Entities;

namespace Logica.Services
{
    /// <summary>
    /// Servicio que maneja la lógica de productos
    /// Maneja tanto productos externos como productos locales
    /// </summary>
    public class ProductService
    {
        private readonly IFakeStoreApiClient _fakeStoreClient;

        public ProductService(IFakeStoreApiClient fakeStoreClient)
        {
            _fakeStoreClient = fakeStoreClient;
        }

        public async Task<IEnumerable<ProductDto>> GetProductsAsync()
        {
            var fakeStoreProducts = await _fakeStoreClient.GetProductsAsync();
            return fakeStoreProducts.Select(MapToProductDto);
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var fakeStoreProduct = await _fakeStoreClient.GetProductByIdAsync(id);
            return fakeStoreProduct != null ? MapToProductDto(fakeStoreProduct) : null;
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _fakeStoreClient.GetCategoriesAsync();
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(string category)
        {
            var fakeStoreProducts = await _fakeStoreClient.GetProductsByCategoryAsync(category);
            return fakeStoreProducts.Select(MapToProductDto);
        }

        // Mapper de FakeStore hacia Domain DTOs
        private static ProductDto MapToProductDto(FakeStoreProductResponse fakeStoreProduct)
        {
            return new ProductDto
            {
                Id = fakeStoreProduct.id,
                Title = fakeStoreProduct.title,
                Price = fakeStoreProduct.price,
                Description = fakeStoreProduct.description,
                Category = fakeStoreProduct.category,
                Image = fakeStoreProduct.image,
                Rating = fakeStoreProduct.rating != null ? new RatingDto
                {
                    Rate = fakeStoreProduct.rating.rate,
                    Count = fakeStoreProduct.rating.count
                } : null
            };
        }
    }
}