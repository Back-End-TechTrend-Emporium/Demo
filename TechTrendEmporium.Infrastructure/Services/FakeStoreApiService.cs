using System.Text.Json;
using TechTrendEmporium.Core.DTOs;
using TechTrendEmporium.Core.Interfaces;

namespace TechTrendEmporium.Infrastructure.Services;

public class FakeStoreApiService : IFakeStoreApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public FakeStoreApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://fakestoreapi.com/");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IEnumerable<FakeStoreProductDto>> GetProductsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("products");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<FakeStoreProductDto[]>(content, _jsonOptions);
            
            return products ?? Array.Empty<FakeStoreProductDto>();
        }
        catch (Exception ex)
        {
            // Log the exception in a real application
            Console.WriteLine($"Error fetching products from FakeStore API: {ex.Message}");
            return Array.Empty<FakeStoreProductDto>();
        }
    }

    public async Task<FakeStoreProductDto?> GetProductByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"products/{id}");
            if (!response.IsSuccessStatusCode)
                return null;
            
            var content = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<FakeStoreProductDto>(content, _jsonOptions);
            
            return product;
        }
        catch (Exception ex)
        {
            // Log the exception in a real application
            Console.WriteLine($"Error fetching product {id} from FakeStore API: {ex.Message}");
            return null;
        }
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("products/categories");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var categories = JsonSerializer.Deserialize<string[]>(content, _jsonOptions);
            
            return categories ?? Array.Empty<string>();
        }
        catch (Exception ex)
        {
            // Log the exception in a real application
            Console.WriteLine($"Error fetching categories from FakeStore API: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    public async Task<IEnumerable<FakeStoreProductDto>> GetProductsByCategoryAsync(string category)
    {
        try
        {
            var response = await _httpClient.GetAsync($"products/category/{category}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<FakeStoreProductDto[]>(content, _jsonOptions);
            
            return products ?? Array.Empty<FakeStoreProductDto>();
        }
        catch (Exception ex)
        {
            // Log the exception in a real application
            Console.WriteLine($"Error fetching products for category {category} from FakeStore API: {ex.Message}");
            return Array.Empty<FakeStoreProductDto>();
        }
    }
}