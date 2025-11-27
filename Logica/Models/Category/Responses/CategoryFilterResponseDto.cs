using Logica.Models.Products;

namespace Logica.Models.Category.Responses
{
    public class CategoryFilterResponseDto
    {
        public string SelectedCategory { get; set; } = string.Empty;
        public IEnumerable<ProductListItemDto> FilteredProducts { get; set; } = new List<ProductListItemDto>();
    }
}