using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.Core.Interfaces.Services;

/// <summary>
/// Interface for chatbot tool implementations
/// </summary>
public interface IToolService
{
    /// <summary>
    /// Unique tool name (e.g., "get_product_inventory")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable description for LLM
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Get tool definition for function calling
    /// </summary>
    ToolDefinition GetDefinition();

    /// <summary>
    /// Execute the tool with given parameters
    /// </summary>
    Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> parameters,
        string? userId = null,
        CancellationToken cancellationToken = default);
}
