using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Models.AI;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.AI;

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
        if (string.IsNullOrEmpty(tool.Name))
        {
            throw new ArgumentException("Tool name cannot be empty");
        }

        if (_tools.ContainsKey(tool.Name))
        {
            _logger.LogWarning("Tool {ToolName} already registered, replacing", tool.Name);
        }

        _tools[tool.Name] = tool;
        _logger.LogInformation("Registered tool: {ToolName}", tool.Name);
    }

    public List<ToolDefinition> GetAllDefinitions()
    {
        return _tools.Values.Select(t => t.GetDefinition()).ToList();
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
            _logger.LogError("Tool {ToolName} not found", toolName);
            return ToolResult.CreateError($"Tool '{toolName}' not found");
        }

        try
        {
            _logger.LogInformation("Executing tool: {ToolName}", toolName);
            var result = await tool.ExecuteAsync(parameters, userId, cancellationToken);
            _logger.LogInformation("Tool {ToolName} executed successfully", toolName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            return ToolResult.CreateError($"Error executing tool: {ex.Message}");
        }
    }

    public bool HasTool(string toolName)
    {
        return _tools.ContainsKey(toolName);
    }
}
