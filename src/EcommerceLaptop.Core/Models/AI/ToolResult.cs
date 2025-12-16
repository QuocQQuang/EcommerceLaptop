namespace EcommerceLaptop.Core.Models.AI;

/// <summary>
/// Represents the result of a tool execution
/// </summary>
public class ToolResult
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static ToolResult CreateSuccess(object? data, Dictionary<string, object>? metadata = null)
    {
        return new ToolResult
        {
            Success = true,
            Data = data,
            Metadata = metadata
        };
    }

    public static ToolResult CreateError(string errorMessage)
    {
        return new ToolResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
