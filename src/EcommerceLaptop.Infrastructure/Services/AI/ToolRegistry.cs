using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Models.AI;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.AI;

/// <summary>
/// Manages registration and execution of AI chatbot tools
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, IToolService> _tools = new();
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(ILogger<ToolRegistry> logger)
    {
        _logger = logger;
    }

    public void RegisterTool(IToolService tool)
    {
        if (string.IsNullOrWhiteSpace(tool.Name))
        {
            throw new ArgumentException("Tool name cannot be empty", nameof(tool));
        }

        if (_tools.ContainsKey(tool.Name))
        {
            _logger.LogWarning("Tool {ToolName} is already registered. Overwriting.", tool.Name);
        }

        _tools[tool.Name] = tool;
        _logger.LogInformation("Registered tool: {ToolName}", tool.Name);
    }

    public List<ToolDefinition> GetAllDefinitions()
    {
        return _tools.Values
            .Select(t => t.GetDefinition())
            .ToList();
    }

    public List<object> GetOpenAIToolDefinitions()
    {
        return _tools.Values
            .Select(t => t.GetDefinition().ToOpenAIFormat())
            .ToList();
    }

    public async Task<ToolResult> ExecuteToolAsync(
        string toolName, 
        Dictionary<string, object> parameters,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            var errorMsg = $"Tool '{toolName}' not found";
            _logger.LogError(errorMsg);
            return ToolResult.CreateError(errorMsg);
        }

        try
        {
            _logger.LogInformation("Executing tool {ToolName} with parameters: {Parameters}", 
                toolName, 
                System.Text.Json.JsonSerializer.Serialize(parameters));

            var result = await tool.ExecuteAsync(parameters, userId, cancellationToken);

            _logger.LogInformation("Tool {ToolName} execution {Status}", 
                toolName, 
                result.Success ? "succeeded" : "failed");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return ToolResult.CreateError($"Tool execution failed: {ex.Message}");
        }
    }

    public bool HasTool(string toolName)
    {
        return _tools.ContainsKey(toolName);
    }
}
