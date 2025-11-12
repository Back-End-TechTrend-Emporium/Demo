using External.FakeStore.Models;
using System.Text.Json;
using System.Text;

namespace External.FakeStore
{
    public class FakeStoreApiService : IFakeStoreApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public FakeStoreApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // Products
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

        // Carts
        public async Task<IEnumerable<FakeStoreCartResponse>> GetCartsAsync()
        {
            var response = await _httpClient.GetStringAsync("/carts");
            var fakeStoreCarts = JsonSerializer.Deserialize<List<FakeStoreCartResponse>>(response, _jsonOptions);
            
            return fakeStoreCarts ?? Enumerable.Empty<FakeStoreCartResponse>();
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

        public async Task<FakeStoreCartResponse?> CreateCartAsync(FakeStoreCartCreateRequest cartRequest)
        {
            try
            {
                var json = JsonSerializer.Serialize(cartRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/carts", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                return JsonSerializer.Deserialize<FakeStoreCartResponse>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<FakeStoreCartResponse?> UpdateCartAsync(int cartId, FakeStoreCartUpdateRequest cartRequest)
        {
            try
            {
                var json = JsonSerializer.Serialize(cartRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"/carts/{cartId}", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                return JsonSerializer.Deserialize<FakeStoreCartResponse>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<FakeStoreCartResponse?> DeleteCartAsync(int cartId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/carts/{cartId}");
                var responseContent = await response.Content.ReadAsStringAsync();
                
                return JsonSerializer.Deserialize<FakeStoreCartResponse>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<IEnumerable<FakeStoreCartResponse>> GetUserCartsAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"/carts/user/{userId}");
                var userCarts = JsonSerializer.Deserialize<List<FakeStoreCartResponse>>(response, _jsonOptions);
                
                return userCarts ?? Enumerable.Empty<FakeStoreCartResponse>();
            }
            catch (HttpRequestException)
            {
                return Enumerable.Empty<FakeStoreCartResponse>();
            }
        }

        // Users
        public async Task<IEnumerable<FakeStoreUserResponse>> GetUsersAsync()
        {
            var response = await _httpClient.GetStringAsync("/users");
            var fakeStoreUsers = JsonSerializer.Deserialize<List<FakeStoreUserResponse>>(response, _jsonOptions);
            
            return fakeStoreUsers ?? Enumerable.Empty<FakeStoreUserResponse>();
        }

        public async Task<FakeStoreUserResponse?> GetUserByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"/users/{id}");
                return JsonSerializer.Deserialize<FakeStoreUserResponse>(response, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<FakeStoreUserResponse?> CreateUserAsync(FakeStoreUserCreateRequest userRequest)
        {
            try
            {
                var json = JsonSerializer.Serialize(userRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/users", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                return JsonSerializer.Deserialize<FakeStoreUserResponse>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<FakeStoreUserResponse?> UpdateUserAsync(int userId, FakeStoreUserCreateRequest userRequest)
        {
            try
            {
                var json = JsonSerializer.Serialize(userRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync($"/users/{userId}", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                return JsonSerializer.Deserialize<FakeStoreUserResponse>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<FakeStoreUserResponse?> DeleteUserAsync(int userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/users/{userId}");
                var responseContent = await response.Content.ReadAsStringAsync();
                
                return JsonSerializer.Deserialize<FakeStoreUserResponse>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
    }
}