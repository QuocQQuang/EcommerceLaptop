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

    private string? _explicitUserId;
    private string? _explicitSessionId;

    public void SetContext(string? userId, string? sessionId)
    {
        _explicitUserId = userId;
        _explicitSessionId = sessionId;
    }

    private (string? userId, string? sessionId) GetUserContext()
    {
        // Prioritize explicit context set by RagChatService (SignalR safe)
        if (!string.IsNullOrEmpty(_explicitUserId) || !string.IsNullOrEmpty(_explicitSessionId))
        {
            return (_explicitUserId, _explicitSessionId);
        }

        var context = _httpContextAccessor.HttpContext;
        var userId = context?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Try get session id from header or cookie
        string? sessionId = null;
        if (context != null)
        {
            if (context.Request.Cookies.TryGetValue("elevate-session-id", out var cookieSession)) // Assuming cookie name
                sessionId = cookieSession;
            else if (context.Request.Headers.TryGetValue("X-Session-ID", out var headerSession))
                sessionId = headerSession.ToString();
        }
        
        return (userId, sessionId);
    }

    // ... GetProductInventory (unchanged) ...

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
        var (userId, sessionId) = GetUserContext();
        
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId))
        {
            return "Error: Could not identify user session. Please verify your connection.";
        }

        try 
        {
            _logger.LogInformation("RagChatToolsPlugin: AddToCart called for ProductId {ProductId}, userId {UserId}, sessionId {SessionId}", productId, userId, sessionId);
            
            var request = new AddToCartDto 
            { 
                ProductId = productId, 
                Quantity = quantity,
                SessionId = sessionId
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
        var (userId, sessionId) = GetUserContext();
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(sessionId)) return "Error: Could not identify user session.";

        try
        {
            _logger.LogInformation("RagChatToolsPlugin: GetCart called for userId {UserId}, sessionId {SessionId}", userId, sessionId);
            var cart = await _cartService.GetCartAsync(userId, sessionId);
            
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
