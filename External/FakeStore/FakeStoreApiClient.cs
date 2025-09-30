using External.FakeStore.Models;
using System.Text.Json;

namespace External.FakeStore
{
    public class FakeStoreApiClient : IFakeStoreApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public FakeStoreApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<IEnumerable<FakeStoreProductResponse>> GetProductsAsync()
        {
            var response = await _httpClient.GetStringAsync("/products");
            var fakeStoreProducts = JsonSerializer.Deserialize<List<FakeStoreProductResponse>>(response, _jsonOptions);
            
            return fakeStoreProducts ?? Enumerable.Empty<FakeStoreProductResponse>();
        }

        public async Task<FakeStoreProductResponse?> GetProductByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"/products/{id}");
                return JsonSerializer.Deserialize<FakeStoreProductResponse>(response, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            var response = await _httpClient.GetStringAsync("/products/categories");
            return JsonSerializer.Deserialize<List<string>>(response, _jsonOptions) ?? Enumerable.Empty<string>();
        }

        public async Task<IEnumerable<FakeStoreProductResponse>> GetProductsByCategoryAsync(string category)
        {
            var response = await _httpClient.GetStringAsync($"/products/category/{category}");
            var fakeStoreProducts = JsonSerializer.Deserialize<List<FakeStoreProductResponse>>(response, _jsonOptions);
            
            return fakeStoreProducts ?? Enumerable.Empty<FakeStoreProductResponse>();
        }

        public async Task<FakeStoreCartResponse?> GetCartByIdAsync(int cartId)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"/carts/{cartId}");
                return JsonSerializer.Deserialize<FakeStoreCartResponse>(response, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IEnumerable<FakeStoreCartResponse>> GetCartsAsync()
        {
            var response = await _httpClient.GetStringAsync("/carts");
            var fakeStoreCarts = JsonSerializer.Deserialize<List<FakeStoreCartResponse>>(response, _jsonOptions);
            
            return fakeStoreCarts ?? Enumerable.Empty<FakeStoreCartResponse>();
        }
    }
}