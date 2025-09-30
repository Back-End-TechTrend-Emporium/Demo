using System;
using System.Collections.Generic;

namespace External.FakeStore.Models
{
    public class FakeStoreProductResponse
    {
        public int id { get; set; }
        public string title { get; set; } = string.Empty;
        public decimal price { get; set; }
        public string description { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string image { get; set; } = string.Empty;
        public FakeStoreRating? rating { get; set; }
    }

    public class FakeStoreRating
    {
        public double rate { get; set; }
        public int count { get; set; }
    }

    public class FakeStoreCartResponse
    {
        public int id { get; set; }
        public int userId { get; set; }
        public DateTime date { get; set; }
        public List<FakeStoreCartProduct> products { get; set; } = new();
    }

    public class FakeStoreCartProduct
    {
        public int productId { get; set; }
        public int quantity { get; set; }
    }
}