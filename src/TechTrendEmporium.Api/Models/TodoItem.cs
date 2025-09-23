namespace Back_End_TechTrend_Emporium.Models
{
    public record TodoItem
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public bool IsCompleted { get; init; }
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
}