namespace Logica.Models.Reviews
{
    public class ReviewsResponseDto
    {
        public Guid Product_Id { get; set; }
        public List<ReviewDto> Reviews { get; set; } = new();
    }
}
