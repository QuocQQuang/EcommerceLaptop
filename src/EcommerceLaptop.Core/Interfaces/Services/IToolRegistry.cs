using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.Core.Interfaces.Services;

/// <summary>
/// Registry for managing chatbot tools
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Register a tool
    /// </summary>
    void RegisterTool(IToolService tool);

    /// <summary>
    /// Get all tool definitions
    /// </summary>
    List<ToolDefinition> GetAllDefinitions();

    /// <summary>
    /// Get tool definitions in OpenAI format
    /// </summary>
    List<object> GetOpenAIToolDefinitions();

    /// <summary>
    /// Execute a tool by name
    /// </summary>
    Task<ToolResult> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object> parameters,
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if tool exists
    /// </summary>
    bool HasTool(string toolName);
}
