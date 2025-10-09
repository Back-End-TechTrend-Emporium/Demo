using Data.Entities;
using Logica.Models;
using Logica.Models.Category;

namespace Logica.Mappers
{
    public static class CartMapper
    {
        public static CartDto ToCartDtoExtended(this Cart cart)
        {
            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId.ToString(),
                
                // ShoppingCart: Lista de IDs como enteros (para compatibilidad con FakeStore)
                ShoppingCart = cart.CartItems?.Select(ci => ConvertGuidToInt(ci.ProductId)).ToList() ?? new List<int>(),
                
                // Items: Información detallada de cada producto en el carrito
                Items = cart.CartItems?.Select(ci => new CartItemSimpleDto
                {
                    ProductId = ci.ProductId,
                    ProductTitle = ci.Product?.Title ?? ci.TitleSnapshot ?? "Unknown Product",
                    Quantity = ci.Quantity,
                    UnitPrice = ci.UnitPriceSnapshot,
                    TotalPrice = ci.UnitPriceSnapshot * ci.Quantity,
                    ProductImage = ci.Product?.ImageUrl ?? ci.ImageUrlSnapshot
                }).ToList() ?? new List<CartItemSimpleDto>(),
                
                CouponApplied = cart.AppliedCoupon != null ? new CouponAppliedDto
                {
                    CouponCode = cart.AppliedCoupon.Code,
                    DiscountPercentage = cart.AppliedCoupon.DiscountPercentage
                } : null,
                
                TotalBeforeDiscount = cart.TotalBeforeDiscount,
                TotalAfterDiscount = cart.TotalBeforeDiscount - cart.DiscountAmount,
                ShippingCost = cart.ShippingCost,
                FinalTotal = cart.FinalTotal,
                
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,
                Status = cart.Status.ToString()
            };
        }

        public static CartItemDto ToCartItemDto(this CartItem cartItem)
        {
            return new CartItemDto
            {
                ProductId = cartItem.ProductId,
                ProductTitle = cartItem.Product?.Title ?? cartItem.TitleSnapshot,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPriceSnapshot,
                TotalPrice = cartItem.UnitPriceSnapshot * cartItem.Quantity,
                ProductImage = cartItem.Product?.ImageUrl ?? cartItem.ImageUrlSnapshot
            };
        }

        public static Cart ToCart(this CartDto cartDto)
        {
            return new Cart
            {
                Id = cartDto.Id,
                UserId = Guid.Parse(cartDto.UserId),
                TotalBeforeDiscount = cartDto.TotalBeforeDiscount,
                DiscountAmount = cartDto.TotalBeforeDiscount - cartDto.TotalAfterDiscount,
                ShippingCost = cartDto.ShippingCost,
                FinalTotal = cartDto.FinalTotal
            };
        }

        // === MÉTODOS PARA VER INFORMACIÓN COMPLETA DE LA BD ===

        /// <summary>
        /// Convierte un Cart a un DTO con información completa para administradores/debugging
        /// Incluye todos los detalles de la base de datos
        /// </summary>
        public static CartFullDetailsDto ToCartFullDetailsDto(this Cart cart)
        {
            return new CartFullDetailsDto
            {
                // Información básica del carrito
                CartId = cart.Id,
                CartStatus = cart.Status.ToString(),
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt,

                // Información del usuario
                UserId = cart.UserId,
                UserEmail = cart.User?.Email ?? "N/A",
                UserUsername = cart.User?.Username ?? "N/A",
                UserRole = cart.User?.Role.ToString() ?? "N/A",
                UserIsActive = cart.User?.IsActive ?? false,

                // Información financiera
                TotalBeforeDiscount = cart.TotalBeforeDiscount,
                DiscountAmount = cart.DiscountAmount,
                ShippingCost = cart.ShippingCost,
                FinalTotal = cart.FinalTotal,

                // Información del cupón (si aplica)
                AppliedCouponId = cart.AppliedCouponId,
                CouponCode = cart.AppliedCoupon?.Code,
                CouponDiscountPercentage = cart.AppliedCoupon?.DiscountPercentage,
                CouponIsActive = cart.AppliedCoupon?.IsActive,
                CouponValidFrom = cart.AppliedCoupon?.ValidFrom,
                CouponValidTo = cart.AppliedCoupon?.ValidTo,

                // Items del carrito con detalles completos
                CartItems = cart.CartItems?.Select(ci => ci.ToCartItemFullDetailsDto()).ToList() ?? new List<CartItemFullDetailsDto>(),

                // Estadísticas
                TotalItems = cart.CartItems?.Count ?? 0,
                TotalQuantity = cart.CartItems?.Sum(ci => ci.Quantity) ?? 0
            };
        }

        /// <summary>
        /// Convierte un CartItem a un DTO con información completa
        /// </summary>
        public static CartItemFullDetailsDto ToCartItemFullDetailsDto(this CartItem cartItem)
        {
            return new CartItemFullDetailsDto
            {
                // Información básica del item
                CartItemId = cartItem.Id,
                CartId = cartItem.CartId,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                UnitPriceSnapshot = cartItem.UnitPriceSnapshot,
                TotalPrice = cartItem.UnitPriceSnapshot * cartItem.Quantity,
                CreatedAt = cartItem.CreatedAt,
                UpdatedAt = cartItem.UpdatedAt,

                // Snapshots guardados (datos históricos)
                TitleSnapshot = cartItem.TitleSnapshot,
                ImageUrlSnapshot = cartItem.ImageUrlSnapshot,
                CategoryNameSnapshot = cartItem.CategoryNameSnapshot,

                // Información actual del producto (si está disponible)
                CurrentProductTitle = cartItem.Product?.Title,
                CurrentProductPrice = cartItem.Product?.Price,
                CurrentProductDescription = cartItem.Product?.Description,
                CurrentProductImageUrl = cartItem.Product?.ImageUrl,
                CurrentProductCategoryName = cartItem.Product?.Category?.Name,
                CurrentProductInventoryAvailable = cartItem.Product?.InventoryAvailable,
                CurrentProductRatingAverage = cartItem.Product?.RatingAverage,
                CurrentProductRatingCount = cartItem.Product?.RatingCount,
                CurrentProductState = cartItem.Product?.State.ToString(),

                // Información del creador del producto
                ProductCreatedBy = cartItem.Product?.CreatedBy,
                ProductCreatorUsername = cartItem.Product?.Creator?.Username,
                ProductCreatedAt = cartItem.Product?.CreatedAt,
                ProductApprovedBy = cartItem.Product?.ApprovedBy,
                ProductApproverUsername = cartItem.Product?.Approver?.Username,
                ProductApprovedAt = cartItem.Product?.ApprovedAt,

                // Comparación de precios
                PriceDifference = (cartItem.Product?.Price ?? 0) - cartItem.UnitPriceSnapshot,
                HasPriceChanged = cartItem.Product != null && cartItem.Product.Price != cartItem.UnitPriceSnapshot
            };
        }

        /// <summary>
        /// Convierte una lista de carritos a un resumen para dashboard administrativo
        /// </summary>
        public static CartsDashboardSummaryDto ToCartsDashboardSummary(this IEnumerable<Cart> carts)
        {
            var cartsList = carts.ToList();
            
            return new CartsDashboardSummaryDto
            {
                TotalCarts = cartsList.Count,
                ActiveCarts = cartsList.Count(c => c.Status == Data.Entities.Enums.CartStatus.Active),
                CheckedOutCarts = cartsList.Count(c => c.Status == Data.Entities.Enums.CartStatus.CheckedOut),
                AbandonedCarts = cartsList.Count(c => c.Status == Data.Entities.Enums.CartStatus.Abandoned),
                
                TotalRevenue = cartsList.Where(c => c.Status == Data.Entities.Enums.CartStatus.CheckedOut)
                                       .Sum(c => c.FinalTotal),
                
                AverageCartValue = cartsList.Any() ? cartsList.Average(c => c.FinalTotal) : 0,
                
                TotalItems = cartsList.Sum(c => c.CartItems?.Count ?? 0),
                TotalQuantity = cartsList.Sum(c => c.CartItems?.Sum(ci => ci.Quantity) ?? 0),
                
                CartsWithCoupons = cartsList.Count(c => c.AppliedCouponId.HasValue),
                TotalDiscountAmount = cartsList.Sum(c => c.DiscountAmount),
                
                RecentCarts = cartsList.Where(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-7)).Count(),
                
                // Top usuarios por número de carritos
                TopUsersByCarts = cartsList.GroupBy(c => new { c.UserId, c.User.Username, c.User.Email })
                                          .Select(g => new UserCartSummaryDto
                                          {
                                              UserId = g.Key.UserId,
                                              Username = g.Key.Username ?? "N/A",
                                              Email = g.Key.Email ?? "N/A",
                                              CartCount = g.Count(),
                                              TotalSpent = g.Where(c => c.Status == Data.Entities.Enums.CartStatus.CheckedOut)
                                                           .Sum(c => c.FinalTotal)
                                          })
                                          .OrderByDescending(u => u.CartCount)
                                          .Take(10)
                                          .ToList(),

                GeneratedAt = DateTime.UtcNow
            };
        }

        private static int ConvertGuidToInt(Guid guid)
        {
            var bytes = guid.ToByteArray();
            return BitConverter.ToInt32(bytes, 0);
        }

        private static Guid ConvertIntToGuid(int id)
        {
            var bytes = new byte[16];
            var idBytes = BitConverter.GetBytes(id);
            Array.Copy(idBytes, 0, bytes, 0, 4);
            return new Guid(bytes);
        }
    }

    // === DTOs PARA INFORMACIÓN COMPLETA ===

    /// <summary>
    /// DTO con información completa del carrito para administradores
    /// </summary>
    public class CartFullDetailsDto
    {
        // Información básica del carrito
        public Guid CartId { get; set; }
        public string CartStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Información del usuario
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserUsername { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public bool UserIsActive { get; set; }

        // Información financiera
        public decimal TotalBeforeDiscount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal FinalTotal { get; set; }

        // Información del cupón
        public Guid? AppliedCouponId { get; set; }
        public string? CouponCode { get; set; }
        public decimal? CouponDiscountPercentage { get; set; }
        public bool? CouponIsActive { get; set; }
        public DateTime? CouponValidFrom { get; set; }
        public DateTime? CouponValidTo { get; set; }

        // Items del carrito
        public List<CartItemFullDetailsDto> CartItems { get; set; } = new();

        // Estadísticas
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
    }

    /// <summary>
    /// DTO con información completa del item del carrito
    /// </summary>
    public class CartItemFullDetailsDto
    {
        // Información básica del item
        public Guid CartItemId { get; set; }
        public Guid CartId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPriceSnapshot { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Snapshots guardados
        public string? TitleSnapshot { get; set; }
        public string? ImageUrlSnapshot { get; set; }
        public string? CategoryNameSnapshot { get; set; }

        // Información actual del producto
        public string? CurrentProductTitle { get; set; }
        public decimal? CurrentProductPrice { get; set; }
        public string? CurrentProductDescription { get; set; }
        public string? CurrentProductImageUrl { get; set; }
        public string? CurrentProductCategoryName { get; set; }
        public int? CurrentProductInventoryAvailable { get; set; }
        public decimal? CurrentProductRatingAverage { get; set; }
        public int? CurrentProductRatingCount { get; set; }
        public string? CurrentProductState { get; set; }

        // Información del creador del producto
        public Guid? ProductCreatedBy { get; set; }
        public string? ProductCreatorUsername { get; set; }
        public DateTime? ProductCreatedAt { get; set; }
        public Guid? ProductApprovedBy { get; set; }
        public string? ProductApproverUsername { get; set; }
        public DateTime? ProductApprovedAt { get; set; }

        // Análisis de cambios
        public decimal PriceDifference { get; set; }
        public bool HasPriceChanged { get; set; }
    }

    /// <summary>
    /// DTO para resumen dashboard de carritos
    /// </summary>
    public class CartsDashboardSummaryDto
    {
        public int TotalCarts { get; set; }
        public int ActiveCarts { get; set; }
        public int CheckedOutCarts { get; set; }
        public int AbandonedCarts { get; set; }
        
        public decimal TotalRevenue { get; set; }
        public decimal AverageCartValue { get; set; }
        
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        
        public int CartsWithCoupons { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        
        public int RecentCarts { get; set; }
        
        public List<UserCartSummaryDto> TopUsersByCarts { get; set; } = new();
        
        public DateTime GeneratedAt { get; set; }
    }

    /// <summary>
    /// DTO para resumen de carritos por usuario
    /// </summary>
    public class UserCartSummaryDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CartCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}