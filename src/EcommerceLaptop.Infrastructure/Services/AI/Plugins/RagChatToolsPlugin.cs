using Microsoft.SemanticKernel;
using System.ComponentModel;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs.Cart;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services.AI.Plugins;

public class RagChatToolsPlugin
{
    private readonly IToolRegistry _toolRegistry;
    private readonly IShoppingCartService _cartService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RagChatToolsPlugin> _logger;

    public RagChatToolsPlugin(
        IToolRegistry toolRegistry,
        IShoppingCartService cartService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RagChatToolsPlugin> logger)
    {
        _toolRegistry = toolRegistry;
        _cartService = cartService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private string? GetUserId() => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [KernelFunction]
    [Description("Get real-time inventory information for a specific product including stock quantity and availability")]
    public async Task<string> GetProductInventory(
        [Description("The ID of the product to check inventory for")] int productId)
    {
        _logger.LogInformation("RagChatToolsPlugin: GetProductInventory called for ProductId {ProductId}", productId);

        var parameters = new Dictionary<string, object>
        {
            { "productId", productId }
        };

        var result = await _toolRegistry.ExecuteToolAsync("get_product_inventory", parameters);

        if (result.Success)
        {
            return JsonSerializer.Serialize(result.Data);
        }
        else
        {
            _logger.LogWarning("RagChatToolsPlugin: Tool execution failed: {ErrorMessage}", result.ErrorMessage);
            return $"Error: {result.ErrorMessage}";
        }
    }

    [KernelFunction]
    [Description("Check the status of an order")]
    public async Task<string> CheckOrderStatus(
        [Description("The Order ID to check")] int orderId)
    {
        // Placeholder for now, can be connected to real tool later
         _logger.LogInformation("RagChatToolsPlugin: CheckOrderStatus called for OrderId {OrderId}", orderId);
         await Task.Delay(100); // Simulate work
         return "Processing (In Transit) - Estimated Delivery: Dec 20, 2025";
    }

    [KernelFunction]
    [Description("Add an item to the shopping cart")]
    public async Task<string> AddToCart(
        [Description("The Product ID to add")] int productId,
        [Description("Quantity to add")] int quantity = 1)
    {
        var userId = GetUserId();
        // Fallback for demo/dev if not logged in (optional, but good for testing)
        // userId ??= "guest-session"; 
        
        if (string.IsNullOrEmpty(userId))
        {
            return "Error: Users must be logged in to add items to cart.";
        }

        try 
        {
            _logger.LogInformation("RagChatToolsPlugin: AddToCart called for ProductId {ProductId}, userId {UserId}", productId, userId);
            
            var request = new AddToCartDto 
            { 
                ProductId = productId, 
                Quantity = quantity 
            };

            var result = await _cartService.AddToCartAsync(request, userId);
            
            // Format result
            var itemCount = result.Summary.ItemCount;
            var total = result.Summary.TotalAmount;
            return $"Successfully added product to cart. Cart now has {itemCount} items. Total value: ${total}.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return $"Error adding to cart: {ex.Message}";
        }
    }

    [KernelFunction]
    [Description("Get the current items in the user's shopping cart")]
    public async Task<string> GetCart()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return "Error: You must be logged in to view your cart.";

        try
        {
            _logger.LogInformation("RagChatToolsPlugin: GetCart called for userId {UserId}", userId);
            var cart = await _cartService.GetCartAsync(userId, null);
            
            if (cart.Items.Count == 0) return "Your cart is empty.";
            
            // Format meaningful string for LLM
            var itemsInfo = string.Join(", ", cart.Items.Select(i => $"{i.Quantity}x {i.ProductName} (${i.FinalPrice})"));
            return $"Your cart contains {cart.Items.Count} items: {itemsInfo}. Total: ${cart.Summary.TotalAmount}.";
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error getting cart");
            return $"Error getting cart: {ex.Message}";
        }
    }
    
    [KernelFunction]
    [Description("Get user account details and profile information")]
    public async Task<string> GetUserAccount()
    {
        // Placeholder
        _logger.LogInformation("RagChatToolsPlugin: GetUserAccount called");
        await Task.Delay(100);
        return "User access verified. Profile viewing not yet implemented.";
    }
}
