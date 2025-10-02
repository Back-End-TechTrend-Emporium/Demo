using System;
using System.Collections.Generic;

namespace External.FakeStore.Models
{
    public class FakeStoreProductResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public FakeStoreRating? Rating { get; set; }
    }

    public class FakeStoreRating
    {
        public double Rate { get; set; }
        public int Count { get; set; }
    }

    public class FakeStoreCartResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Date { get; set; }
        public List<FakeStoreCartProduct> Products { get; set; } = new();
    }

    public class FakeStoreCartProduct
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}