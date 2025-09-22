using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechTrendEmporium.Core.DTOs;
using TechTrendEmporium.Core.Entities;
using TechTrendEmporium.Core.Interfaces;

namespace TechTrendEmporium.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var userId = GetCurrentUserId();
        var cart = await GetOrCreateCartAsync(userId);
        
        return Ok(MapToCartDto(cart));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemRequest request)
    {
        var userId = GetCurrentUserId();
        
        // Check if product exists
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            return BadRequest("Product not found");
        }

        if (!product.IsActive)
        {
            return BadRequest("Product is not available");
        }

        if (product.StockQuantity < request.Quantity)
        {
            return BadRequest("Insufficient stock");
        }

        var cart = await GetOrCreateCartAsync(userId);
        var items = await _unitOfWork.Carts.GetCartItemsAsync(cart.Id);
        
        // Check if item already exists in cart
        var existingItem = items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.UnitPrice = product.Price; // Update price in case it changed
            await _unitOfWork.Carts.UpdateItemAsync(existingItem);
        }
        else
        {
            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPrice = product.Price,
                AddedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Carts.AddItemAsync(cartItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        // Reload cart with items
        cart = await _unitOfWork.Carts.GetActiveCartByUserIdAsync(userId);
        return Ok(MapToCartDto(cart!));
    }

    [HttpPut("items/{productId}")]
    public async Task<ActionResult<CartDto>> UpdateItem(int productId, [FromBody] UpdateCartItemRequest request)
    {
        var userId = GetCurrentUserId();
        var cart = await GetOrCreateCartAsync(userId);
        var items = await _unitOfWork.Carts.GetCartItemsAsync(cart.Id);
        
        var existingItem = items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem == null)
        {
            return NotFound("Item not found in cart");
        }

        if (request.Quantity <= 0)
        {
            await _unitOfWork.Carts.RemoveItemAsync(existingItem.Id);
        }
        else
        {
            // Check stock
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product != null && product.StockQuantity < request.Quantity)
            {
                return BadRequest("Insufficient stock");
            }

            existingItem.Quantity = request.Quantity;
            await _unitOfWork.Carts.UpdateItemAsync(existingItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        // Reload cart with items
        cart = await _unitOfWork.Carts.GetActiveCartByUserIdAsync(userId);
        return Ok(MapToCartDto(cart!));
    }

    [HttpDelete("items/{productId}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int productId)
    {
        var userId = GetCurrentUserId();
        var cart = await GetOrCreateCartAsync(userId);
        var items = await _unitOfWork.Carts.GetCartItemsAsync(cart.Id);
        
        var existingItem = items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem == null)
        {
            return NotFound("Item not found in cart");
        }

        await _unitOfWork.Carts.RemoveItemAsync(existingItem.Id);
        
        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        // Reload cart with items
        cart = await _unitOfWork.Carts.GetActiveCartByUserIdAsync(userId);
        return Ok(MapToCartDto(cart!));
    }

    [HttpDelete]
    public async Task<ActionResult<CartDto>> ClearCart()
    {
        var userId = GetCurrentUserId();
        var cart = await GetOrCreateCartAsync(userId);
        
        await _unitOfWork.Carts.ClearCartAsync(cart.Id);
        
        cart.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Carts.UpdateAsync(cart);
        await _unitOfWork.SaveChangesAsync();

        // Reload cart with items
        cart = await _unitOfWork.Carts.GetActiveCartByUserIdAsync(userId);
        return Ok(MapToCartDto(cart!));
    }

    [HttpPost("calculate-total")]
    public async Task<ActionResult<CartTotalDto>> CalculateTotal([FromBody] CalculateCartTotalRequest request)
    {
        var userId = GetCurrentUserId();
        var cart = await GetOrCreateCartAsync(userId);
        var items = await _unitOfWork.Carts.GetCartItemsAsync(cart.Id);
        
        var subTotal = items.Sum(i => i.Quantity * i.UnitPrice);
        var discountAmount = 0m;
        
        // Apply coupon if provided
        if (!string.IsNullOrEmpty(request.CouponCode))
        {
            var coupon = await _unitOfWork.Coupons.GetByCodeAsync(request.CouponCode);
            if (coupon != null && await _unitOfWork.Coupons.IsValidAsync(request.CouponCode, subTotal))
            {
                discountAmount = (subTotal * coupon.DiscountPercentage / 100);
                if (coupon.MaxDiscountAmount.HasValue && discountAmount > coupon.MaxDiscountAmount.Value)
                {
                    discountAmount = coupon.MaxDiscountAmount.Value;
                }
            }
        }
        
        var total = subTotal - discountAmount;
        
        return Ok(new CartTotalDto(subTotal, discountAmount, total, request.CouponCode));
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User not authenticated");
    }

    private async Task<Cart> GetOrCreateCartAsync(string userId)
    {
        var cart = await _unitOfWork.Carts.GetActiveCartByUserIdAsync(userId);
        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            await _unitOfWork.Carts.AddAsync(cart);
            await _unitOfWork.SaveChangesAsync();
        }
        
        return cart;
    }

    private CartDto MapToCartDto(Cart cart)
    {
        var items = cart.Items.Select(i => new CartItemDto(
            i.Id,
            i.ProductId,
            i.Product.Title,
            i.Product.ImageUrl,
            i.UnitPrice,
            i.Quantity,
            i.Quantity * i.UnitPrice,
            i.AddedAt));

        var subTotal = items.Sum(i => i.TotalPrice);
        
        return new CartDto(
            cart.Id,
            cart.UserId,
            items,
            subTotal,
            0, // No discount applied here
            subTotal,
            cart.UpdatedAt);
    }
}

public record UpdateCartItemRequest(int Quantity);
public record CalculateCartTotalRequest(string? CouponCode);
public record CartTotalDto(decimal SubTotal, decimal DiscountAmount, decimal Total, string? CouponCode);