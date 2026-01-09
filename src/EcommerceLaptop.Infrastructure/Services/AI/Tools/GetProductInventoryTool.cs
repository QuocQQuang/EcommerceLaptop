using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Models.AI;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.Infrastructure.Services.AI.Tools;

/// <summary>
/// Tool for retrieving real-time product inventory information
/// </summary>
public class GetProductInventoryTool : IToolService
{
    private readonly IProductService _productService;

    public string Name => "get_product_inventory";
    public string Description => "Get real-time inventory information for a specific product including stock quantity and availability";

    public GetProductInventoryTool(IProductService productService)
    {
        _productService = productService;
    }

    public ToolDefinition GetDefinition()
    {
        return new ToolDefinition
        {
            Name = Name,
            Description = Description,
            Parameters = new List<ToolParameter>
            {
                new()
                {
                    Name = "productId",
                    Type = "number",
                    Description = "The ID of the product to check inventory for",
                    Required = true
                }
            }
        };
    }

    public async Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> parameters, 
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract productId parameter
            if (!parameters.TryGetValue("productId", out var productIdObj))
            {
                return ToolResult.CreateError("Missing required parameter: productId");
            }

            int productId;
            if (productIdObj is int id)
            {
                productId = id;
            }
            else if (productIdObj is long longId)
            {
                productId = (int)longId;
            }
            else if (productIdObj is string strId && int.TryParse(strId, out var parsed))
            {
                productId = parsed;
            }
            else
            {
                return ToolResult.CreateError($"Invalid productId format: {productIdObj}");
            }

            // Fetch product with inventory
            var product = await _productService.GetByIdAsync(productId);
            if (product == null)
            {
                return ToolResult.CreateError($"Product with ID {productId} not found");
            }

            // Build inventory data
            var inventoryData = new
            {
                productId = product.Id,
                productName = product.Name,
                quantityInStock = product.Inventory?.QuantityInStock ?? 0,
                isAvailable = (product.Inventory?.QuantityInStock ?? 0) > 0,
                reservedQuantity = product.Inventory?.ReservedQuantity ?? 0,
                availableQuantity = Math.Max(0, (product.Inventory?.QuantityInStock ?? 0) - (product.Inventory?.ReservedQuantity ?? 0)),
                lastStockUpdate = product.Inventory?.LastStockUpdate
            };

            return ToolResult.CreateSuccess(inventoryData);
        }
        catch (Exception ex)
        {
            return ToolResult.CreateError($"Error retrieving inventory: {ex.Message}");
        }
    }
}
