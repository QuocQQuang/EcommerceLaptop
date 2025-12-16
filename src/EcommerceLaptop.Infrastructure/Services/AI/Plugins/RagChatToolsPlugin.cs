using Microsoft.SemanticKernel;
using System.ComponentModel;
using EcommerceLaptop.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.AI.Plugins;

public class RagChatToolsPlugin
{
    private readonly IToolRegistry _toolRegistry;
    private readonly ILogger<RagChatToolsPlugin> _logger;

    public RagChatToolsPlugin(IToolRegistry toolRegistry, ILogger<RagChatToolsPlugin> logger)
    {
        _toolRegistry = toolRegistry;
        _logger = logger;
    }

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
            return System.Text.Json.JsonSerializer.Serialize(result.Data);
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
        // Placeholder
        _logger.LogInformation("RagChatToolsPlugin: AddToCart called for ProductId {ProductId}, Quantity {Quantity}", productId, quantity);
        await Task.Delay(100);
        return $"Added Product {productId} (Quantity: {quantity}) to cart successfully. Total items: {quantity}.";
    }

    [KernelFunction]
    [Description("Get the current items in the user's shopping cart")]
    public async Task<string> GetCart()
    {
        // Placeholder
        _logger.LogInformation("RagChatToolsPlugin: GetCart called");
        await Task.Delay(100);
        return "Your cart contains: 1x Dell XPS 15 ($1499), 1x Gaming Mouse ($49). Total: $1548.";
    }
    
    [KernelFunction]
    [Description("Get user account details and profile information")]
    public async Task<string> GetUserAccount()
    {
        // Placeholder
        _logger.LogInformation("RagChatToolsPlugin: GetUserAccount called");
        await Task.Delay(100);
        return "User Profile: John Doe, Email: john@example.com, Membership: Gold, Saved Addresses: 2";
    }
}
