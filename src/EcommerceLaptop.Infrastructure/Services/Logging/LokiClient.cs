using System.Text.Json;
using System.Text.Json.Serialization;
using EcommerceLaptop.Core.Entities;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.Logging;

public class LokiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LokiClient> _logger;

    public LokiClient(HttpClient httpClient, ILogger<LokiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<SecurityEvent>> QueryAsync(string query, DateTime start, DateTime end, int limit = 100)
    {
        try
        {
            // Loki API: /loki/api/v1/query_range
            // Parameters: query, start, end, limit, direction
            // start/end are in nanoseconds
            
            var startNs = ((DateTimeOffset)start).ToUnixTimeMilliseconds() * 1000000;
            var endNs = ((DateTimeOffset)end).ToUnixTimeMilliseconds() * 1000000;
            
            var url = $"/loki/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={startNs}&end={endNs}&limit={limit}&direction=backward";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var lokiResponse = JsonSerializer.Deserialize<LokiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (lokiResponse?.Data?.Result == null)
            {
                return new List<SecurityEvent>();
            }

            return ParseLokiResponse(lokiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Loki");
            // Return empty list on failure to avoid breaking UI
            return new List<SecurityEvent>();
        }
    }

    private List<SecurityEvent> ParseLokiResponse(LokiResponse? response)
    {
        var events = new List<SecurityEvent>();

        if (response?.Data?.Result == null) return events;

        foreach (var stream in response.Data.Result)
        {
            foreach (var value in stream.Values)
            {
                // value[0] is timestamp (ns string), value[1] is log line
                if (value.Count < 2) continue;

                var timestampNs = long.Parse(value[0].ToString()!);
                var logLine = value[1].ToString();
                
                // Parse log line - Serilog usually outputs JSON if configured nicely, or plain text
                // Assuming we can extract info or the log line itself is the description
                
                // Try to parse JSON log if structured
                try 
                {
                    // This is a naive implementation assuming logs are somewhat structured or we just use the line as description
                    // In a real scenario you would parse Serilog's JSON output
                    // For now, let's create a generic event wrapping the log line
                    
                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampNs / 1000000).UtcDateTime;

                    // Extract labels
                    var labels = stream.Stream;
                    var eventType = labels.ContainsKey("EventType") ? labels["EventType"] : "log_event";
                    var level = labels.ContainsKey("Level") ? labels["Level"] : "Info";

                    events.Add(new SecurityEvent
                    {
                        Id = 0, // No ID in Loki
                        EventType = eventType,
                        Description = logLine ?? "No content",
                        CreatedAt = timestamp,
                        Severity = level,
                        IPAddress = labels.ContainsKey("IPAddress") ? labels["IPAddress"] : "Unknown"
                        // Source property not available in entity, omitting
                    });
                }
                catch
                {
                    // Fallback to plain text
                }
            }
        }

        return events;
    }
}

// DTOs for Loki Response
public class LokiResponse
{
    public string Status { get; set; } = string.Empty;
    public LokiData? Data { get; set; }
}

public class LokiData
{
    public string ResultType { get; set; } = string.Empty; // "streams" or "matrix"
    public List<LokiStream>? Result { get; set; }
}

public class LokiStream
{
    public Dictionary<string, string> Stream { get; set; } = new();
    public List<List<object>> Values { get; set; } = new(); // [["timestamp", "line"], ...]
}
