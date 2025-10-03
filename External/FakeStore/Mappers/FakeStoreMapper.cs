using External.FakeStore.Models;
using Data.Entities;
using Data.Entities.Enums;

namespace External.FakeStore.Mappers
{
    /// <summary>
    /// Mapper para convertir entre DTOs de FakeStore y Entidades del dominio
    /// Este mapper crea entidades que luego necesitarán ser persistidas con ExternalMapping
    /// </summary>
    public static class FakeStoreMapper
    {
        public static Product ToProduct(this FakeStoreProductResponse fakeStoreProduct)
        {
            return new Product
            {
                Title = fakeStoreProduct.Title,
                Price = fakeStoreProduct.Price,
                Description = fakeStoreProduct.Description,
                ImageUrl = fakeStoreProduct.Image,
                State = ApprovalState.PendingApproval,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
                // CategoryId y CreatedBy necesitarán ser establecidos por el servicio que use este mapper
            };
        }

        public static ExternalMapping ToExternalMapping(this FakeStoreProductResponse fakeStoreProduct, Guid internalProductId)
        {
            return new ExternalMapping
            {
                Source = ExternalSource.FakeStore,
                SourceType = "PRODUCT",
                SourceId = fakeStoreProduct.Id.ToString(),
                InternalId = internalProductId,
                SnapshotJson = System.Text.Json.JsonSerializer.Serialize(fakeStoreProduct),
                ImportedAt = DateTime.UtcNow
            };
        }

        public static Cart ToCart(this FakeStoreCartResponse fakeStoreCart)
        {
            var cart = new Cart
            {
                UserId = Guid.NewGuid(), // Necesitarías resolver esto al UserId interno real
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var cartItems = fakeStoreCart.Products.Select(p => new CartItem
            {
                ProductId = Guid.NewGuid(), // Necesitarías resolver esto desde ExternalMapping
                Quantity = p.Quantity,
                Cart = cart,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            cart.CartItems = cartItems;
            return cart;
        }
    }
}