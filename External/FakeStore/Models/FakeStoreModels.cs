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

    // Modelos para las operaciones de creación y actualización
    public class FakeStoreCartCreateRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public List<FakeStoreCartProduct> Products { get; set; } = new();
    }

    public class FakeStoreCartUpdateRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public List<FakeStoreCartProduct> Products { get; set; } = new();
    }

    // User models for FakeStore API
    public class FakeStoreUserResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public FakeStoreUserName Name { get; set; } = new();
        public FakeStoreUserAddress Address { get; set; } = new();
        public string Phone { get; set; } = string.Empty;
        public int __v { get; set; }
    }

    public class FakeStoreUserName
    {       
        public string Firstname { get; set; } = string.Empty;
        public string Lastname { get; set; } = string.Empty;
    }

    public class FakeStoreUserAddress
    {
        public FakeStoreGeolocation Geolocation { get; set; } = new();
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public int Number { get; set; }
        public string Zipcode { get; set; } = string.Empty;
    }

    public class FakeStoreGeolocation
    {
        public string Lat { get; set; } = string.Empty;
        public string Long { get; set; } = string.Empty;
    }

    // User creation request model
    public class FakeStoreUserCreateRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public FakeStoreUserName Name { get; set; } = new();
        public FakeStoreUserAddress Address { get; set; } = new();
        public string Phone { get; set; } = string.Empty;
    }
}