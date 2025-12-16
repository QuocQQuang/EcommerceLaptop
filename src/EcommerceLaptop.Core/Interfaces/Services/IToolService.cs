using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.Core.Interfaces.Services;

/// <summary>
/// Interface for implementing AI chatbot tools that can be called by the LLM
/// </summary>
public interface IToolService
{
    /// <summary>
    /// The unique name of the tool (used by LLM for function calling)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Human-readable description of what the tool does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Get the tool's parameter definitions for LLM function calling
    /// </summary>
    ToolDefinition GetDefinition();

    /// <summary>
    /// Execute the tool with the provided parameters
    /// </summary>
    /// <param name="parameters">Parameters passed from the LLM</param>
    /// <param name="userId">Optional user ID for user-specific operations</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the tool execution</returns>
    Task<ToolResult> ExecuteAsync(
        Dictionary<string, object> parameters, 
        string? userId = null,
        CancellationToken cancellationToken = default);
}
