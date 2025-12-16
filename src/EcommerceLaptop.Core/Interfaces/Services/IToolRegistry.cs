using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.Core.Interfaces.Services;

/// <summary>
/// Service for managing and executing AI chatbot tools
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Register a tool for use by the chatbot
    /// </summary>
    void RegisterTool(IToolService tool);

    /// <summary>
    /// Get all registered tool definitions for LLM function calling
    /// </summary>
    List<ToolDefinition> GetAllDefinitions();

    /// <summary>
    /// Get tool definitions in OpenAI function calling format
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
    /// Check if a tool is registered
    /// </summary>
    bool HasTool(string toolName);
}
