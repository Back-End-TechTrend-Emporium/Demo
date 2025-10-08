namespace Logica.Models.Carts
{
    public class CartSyncResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid? LocalCartId { get; set; }
        public int FakeStoreCartId { get; set; }
        public int ProductsSynced { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<int> InvalidProductIds { get; set; } = new();
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    }

    public class CartSyncBatchResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalCartsProcessed { get; set; }
        public int CartsSuccessful { get; set; }
        public int CartsFailed { get; set; }
        public List<CartSyncResultDto> Results { get; set; } = new();
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
    }
}