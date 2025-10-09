using External.FakeStore.Models;
using Data.Entities;
using Data.Entities.Enums;

namespace External.FakeStore.Mappers
{
    /// <summary>
    /// Mapper unificado para convertir entre DTOs de FakeStore y Entidades del dominio
    /// Incluye conversiones bidireccionales: desde FakeStore hacia entidades internas y viceversa
    /// </summary>
    public static class FakeStoreMapper
    {
        #region From FakeStore to Internal Entities

        /// <summary>
        /// Convierte un producto de FakeStore a una entidad Product interna
        /// </summary>
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

        /// <summary>
        /// Convierte un producto de FakeStore a un mapping externo
        /// </summary>
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

        /// <summary>
        /// Convierte un carrito de FakeStore a una entidad Cart interna
        /// </summary>
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

        #endregion

        #region From Internal Entities to FakeStore Requests

        /// <summary>
        /// Crea un request para crear un carrito en FakeStore
        /// </summary>
        public static FakeStoreCartCreateRequest ToCreateRequest(int userId, List<FakeStoreCartProduct> products)
        {
            return new FakeStoreCartCreateRequest
            {
                UserId = userId,
                Products = products ?? new List<FakeStoreCartProduct>()
            };
        }

        /// <summary>
        /// Crea un request para actualizar un carrito en FakeStore
        /// </summary>
        public static FakeStoreCartUpdateRequest ToUpdateRequest(int id, int userId, List<FakeStoreCartProduct> products)
        {
            return new FakeStoreCartUpdateRequest
            {
                Id = id,
                UserId = userId,
                Products = products ?? new List<FakeStoreCartProduct>()
            };
        }

        /// <summary>
        /// Convierte items del carrito interno a productos de FakeStore
        /// </summary>
        public static List<FakeStoreCartProduct> ToFakeStoreCartProducts(this IEnumerable<CartItem> cartItems, Func<Guid, int?> getExternalProductId)
        {
            return cartItems.Select(item => new FakeStoreCartProduct
            {
                ProductId = getExternalProductId(item.ProductId) ?? 0,
                Quantity = item.Quantity
            }).Where(p => p.ProductId > 0).ToList();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Resuelve el ID de usuario interno desde el ID de FakeStore
        /// </summary>
        public static Guid ResolveInternalUserId(int fakeStoreUserId, Func<int, Guid?> userResolver)
        {
            return userResolver(fakeStoreUserId) ?? Guid.NewGuid();
        }

        /// <summary>
        /// Resuelve el ID de producto interno desde el ID de FakeStore
        /// </summary>
        public static Guid ResolveInternalProductId(int fakeStoreProductId, Func<int, Guid?> productResolver)
        {
            return productResolver(fakeStoreProductId) ?? Guid.NewGuid();
        }

        #endregion
    }
}