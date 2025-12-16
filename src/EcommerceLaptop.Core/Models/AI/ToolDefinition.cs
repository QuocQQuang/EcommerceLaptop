namespace EcommerceLaptop.Core.Models.AI;

/// <summary>
/// Defines the schema for a tool parameter
/// </summary>
public class ToolParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, number, boolean, object
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public List<string>? EnumValues { get; set; }
}

/// <summary>
/// Metadata describing a tool for LLM function calling
/// </summary>
public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ToolParameter> Parameters { get; set; } = new();

    /// <summary>
    /// Convert to OpenAI function calling format
    /// </summary>
    public object ToOpenAIFormat()
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var param in Parameters)
        {
            var propDef = new Dictionary<string, object>
            {
                ["type"] = param.Type,
                ["description"] = param.Description
            };

            if (param.EnumValues?.Any() == true)
            {
                propDef["enum"] = param.EnumValues;
            }

            properties[param.Name] = propDef;

            if (param.Required)
            {
                required.Add(param.Name);
            }
        }

        return new
        {
            type = "function",
            function = new
            {
                name = Name,
                description = Description,
                parameters = new
                {
                    type = "object",
                    properties,
                    required
                }
            }
        };
    }
}
