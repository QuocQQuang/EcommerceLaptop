using Microsoft.SemanticKernel;
using System.ComponentModel;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs.Cart;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Linq;
using System.Text;
using EcommerceLaptop.Infrastructure.Services.Security;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceLaptop.Infrastructure.Services.AI.Plugins;

public class RagChatToolsPlugin
{
    private readonly IToolRegistry _toolRegistry;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Core.Interfaces.IVectorDbService _vectorDbService; 
    private readonly Core.Interfaces.IEmbeddingService _embeddingService;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<RagChatToolsPlugin> _logger;

    public RagChatToolsPlugin(
        IToolRegistry toolRegistry,
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor,
        Core.Interfaces.IVectorDbService vectorDbService,
        Core.Interfaces.IEmbeddingService embeddingService,
        IRateLimitingService rateLimitingService,
        ILogger<RagChatToolsPlugin> logger)
    {
        _toolRegistry = toolRegistry;
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
        _vectorDbService = vectorDbService;
        _embeddingService = embeddingService;
        _rateLimitingService = rateLimitingService;
        _logger = logger;
    }

    private string? _explicitUserId;
    private string? _explicitSessionId;

    public void SetContext(string? userId, string? sessionId)
    {
        _explicitUserId = userId;
        _explicitSessionId = sessionId;
    }

    public (string? userId, string? sessionId) GetUserContext()
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

    // ... GetProductInventory (unchanged would go here in full file, but focusing on replacement of relevant methods) ...

    [KernelFunction]
    [Description("Check the status of a specific order by ID")]
    public async Task<string> CheckOrderStatus(
        [Description("The Order ID to check")] int orderId)
    {
         var (userId, sessionId) = GetUserContext();
         
         // Validate UserId is an Integer (Authenticated)
         if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out _)) 
            return "Please login to view order status.";

         using (var scope = _scopeFactory.CreateScope())
         {
             var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

             try
             {
                 _logger.LogInformation("RagChatToolsPlugin: CheckOrderStatus called for OrderId {OrderId}, UserId {UserId}", orderId, userId);
                 


                 var order = await orderService.GetOrderDetailsAsync(orderId);
                 
                 // Security check - Unified response to prevent enumeration
                 // We check for both null (not found) and ownership mismatch
                 if (order == null || order.CustomerId.ToString() != userId) 
                 {
                     // Log the specific reason internally for security monitoring
                     if (order == null)
                     {
                         _logger.LogWarning("Security: Order lookup failed - Order #{OrderId} not found. Requested by User {UserId}", orderId, userId);
                     }
                     else
                     {
                         _logger.LogWarning("SECURITY ALERT: Authorization denied. User {UserId} attempted to access Order #{OrderId} belonging to Customer {OwnerId}", userId, orderId, order.CustomerId);
                     }

                     // Return generic message to user
                     return $"Order #{orderId} not found.";
                 }

                 return $@"
### Order #{order.Id}
- **Status:** {order.Status}
- **Date:** {order.CreatedAt:MMM dd, yyyy}
- **Total:** ${order.TotalAmount:N2}
- **Items:**
{string.Join("\n", order.Items.Select(i => $"  - {i.ProductName} x{i.Quantity}"))}

[Tracking Info]: {order.TrackingNumber ?? "N/A"}
";
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error checking order status");
                 return "Unable to retrieve order details at this time.";
             }
         }
    }

    [KernelFunction]
    [Description("Get a list of the user's recent orders")]
    public async Task<string> GetMyOrders()
    {
        var (userId, sessionId) = GetUserContext();
        
        // Validate UserId is an Integer (Authenticated)
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out _)) 
            return "Please login to view your orders.";

        using (var scope = _scopeFactory.CreateScope())
        {
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            
            try
            {
                _logger.LogInformation("RagChatToolsPlugin: GetMyOrders called for UserId {UserId}", userId);
                
                // Fetch last 5 orders
                var result = await orderService.GetCustomerOrdersAsync(int.Parse(userId), 1, 5);
                
                if (result.Items == null || !result.Items.Any())
                {
                    return "You have no orders yet.";
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Here are your recent orders:");
                sb.AppendLine("| Order # | Date | Status | Total |");
                sb.AppendLine("|---|---|---|---|");
                
                foreach (var order in result.Items)
                {
                    sb.AppendLine($"| {order.Id} | {order.CreatedAt:MMM dd} | {order.Status} | ${order.TotalAmount:N2} |");
                }
                sb.AppendLine("\nAsk for a specific order ID to see more details.");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders");
                return $"Error retrieving orders: {ex.Message}";
            }
        }
    }

    [KernelFunction]
    [Description("Add an item to the shopping cart")]
    public async Task<string> AddToCart(
        [Description("The Product ID to add")] int productId,
        [Description("Quantity to add")] int quantity = 1)
    {
        var (userId, sessionId) = GetUserContext();
        
        // Determine effective User ID and Cart Session ID
        string? effectiveUserId = null;
        string? effectiveCartSessionId = sessionId; // Default to chat session, but override if guest

        if (!string.IsNullOrEmpty(userId))
        {
            if (int.TryParse(userId, out _))
            {
                effectiveUserId = userId; // It's a real User ID
            }
            else
            {
                // userId is a String/GUID -> It's a Guest Cart Session ID (passed from ChatHub fallback)
                effectiveCartSessionId = userId;
            }
        }
        
        if (string.IsNullOrEmpty(effectiveUserId) && string.IsNullOrEmpty(effectiveCartSessionId))
        {
            return "Error: Could not identify user session. Please verify your connection.";
        }

        using (var scope = _scopeFactory.CreateScope())
        {
            var cartService = scope.ServiceProvider.GetRequiredService<IShoppingCartService>();

            try 
            {
                _logger.LogInformation("RagChatToolsPlugin: AddToCart called for ProductId {ProductId}. RealUserId: {RealUserId}, CartSessionId: {CartSessionId}", productId, effectiveUserId, effectiveCartSessionId);
                
                var request = new AddToCartDto 
                { 
                    ProductId = productId, 
                    Quantity = quantity,
                    SessionId = effectiveCartSessionId
                };

                var result = await cartService.AddToCartAsync(request, effectiveUserId);
                
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
    }

    [KernelFunction]
    [Description("Get the current items in the user's shopping cart")]
    public async Task<string> GetCart()
    {
        var (userId, sessionId) = GetUserContext();
        
        // Determine effective User ID and Cart Session ID
        string? effectiveUserId = null;
        string? effectiveCartSessionId = sessionId; // Default to chat session, but override if guest

        if (!string.IsNullOrEmpty(userId))
        {
            if (int.TryParse(userId, out _))
            {
                effectiveUserId = userId;
            }
            else
            {
                // userId is a String/GUID -> It's a Guest Cart Session ID
                effectiveCartSessionId = userId;
            }
        }

        if (string.IsNullOrEmpty(effectiveUserId) && string.IsNullOrEmpty(effectiveCartSessionId)) 
            return "Error: Could not identify user session.";

        using (var scope = _scopeFactory.CreateScope())
        {
            var cartService = scope.ServiceProvider.GetRequiredService<IShoppingCartService>();

            try
            {
                _logger.LogInformation("RagChatToolsPlugin: GetCart called. RealUserId: {RealUserId}, CartSessionId: {CartSessionId}", effectiveUserId, effectiveCartSessionId);
                var cart = await cartService.GetCartAsync(effectiveUserId, effectiveCartSessionId);
                
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

    [KernelFunction]
    [Description("Search for products by name or description to find their IDs and details")]
    public async Task<string> SearchProducts(
        [Description("The search query (e.g., 'Dell XPS 13', 'gaming laptop')")] string query)
    {
        try
        {
            _logger.LogInformation("RagChatToolsPlugin: SearchProducts called for query '{Query}'", query);

            var embedding = await _embeddingService.GenerateEmbeddingAsync(query);
            var searchResults = await _vectorDbService.SearchAsync("products", embedding, limit: 3);

            if (!searchResults.Any())
            {
                return "No products found matching that query.";
            }

            var resultString = string.Join("\n\n", searchResults.Select(r => 
            {
                // Extract Product ID safely from metadata
                string pidStr = "Unknown";
                if (r.Metadata.TryGetValue("product_id", out var pidObj))
                {
                    pidStr = pidObj.ToString() ?? "Unknown";
                }

                return $"[Product Found]\nID: {pidStr}\nDetails: {r.Content}\nScore: {r.Score:F2}";
            }));

            return $"Found the following products. Use the exact ID to perform actions.\n\n{resultString}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products in tool");
            return $"Error searching products: {ex.Message}";
        }
    }
}
