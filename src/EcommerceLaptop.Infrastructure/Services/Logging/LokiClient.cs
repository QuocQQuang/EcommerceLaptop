using System.Text.Json;
using System.Text.Json.Serialization;
using EcommerceLaptop.Core.Entities;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.Logging;

public class LokiClient : ILokiClient
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
            return new List<SecurityEvent>();
        }
    }

    public async Task<int> CountAsync(string query, DateTime start, DateTime end)
    {
        try
        {
            // wrap query in count_over_time for the full duration
            var duration = (end - start).TotalSeconds;
            // Avoid range syntax in the query parameter itself if using query_range? 
            // Actually, for instant query over a range or explicit range query:
            // We want a single number. 
            // Query: sum(count_over_time({selector}[duration_s]))
            
            var metricQuery = $"sum(count_over_time({query}[{(int)duration}s]))";
            var url = BuildQueryUrl(metricQuery, start, end, 1);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var lokiResponse = JsonSerializer.Deserialize<LokiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (lokiResponse?.Data?.Result == null) return 0;

            // Matrix response: Result -> Values -> [timestamp, value]
            // We expect one series with one or more values. Sum them or take the last?
            // With count_over_time[range] and enough step, we should get one value representing the count.
            
            double total = 0;
            foreach (var stream in lokiResponse.Data.Result)
            {
                foreach (var value in stream.Values)
                {
                     if (value.Count >= 2 && double.TryParse(value[1].ToString(), out var val))
                     {
                         total = val; // Usually correct for single vector
                     }
                }
            }

            return (int)total;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting Loki events");
            return 0;
        }
    }

    public async Task<Dictionary<string, int>> GetMetricAsync(string query, DateTime start, DateTime end)
    {
        try
        {
            var url = BuildQueryUrl(query, start, end, 1000);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var lokiResponse = JsonSerializer.Deserialize<LokiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var result = new Dictionary<string, int>();
            if (lokiResponse?.Data?.Result == null) return result;

            foreach (var stream in lokiResponse.Data.Result)
            {
                // Key from labels
                var labels = stream.Stream ?? stream.Metric; // Handle both 'stream' and 'metric'
                if (labels == null) continue;

                // Create a key from labels (e.g. EventType=login)
                var key = string.Join(",", labels.Select(kv => $"{kv.Key}={kv.Value}"));
                if (string.IsNullOrEmpty(key)) key = "Total";

                // Sum values or take latest? For metrics over time, we might want sum or just the series.
                // Assuming query returns values we want to sum or aggregating query.
                // Let's take the LAST value of the series as the current state.
                
                if (stream.Values.Any())
                {
                    var lastValue = stream.Values.Last();
                    if (lastValue.Count >= 2 && double.TryParse(lastValue[1].ToString(), out var val))
                    {
                        result[key] = (int)val;
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Loki metrics");
            return new Dictionary<string, int>();
        }
    }

    private string BuildQueryUrl(string query, DateTime start, DateTime end, int limit)
    {
        var startNs = ((DateTimeOffset)start).ToUnixTimeMilliseconds() * 1000000;
        var endNs = ((DateTimeOffset)end).ToUnixTimeMilliseconds() * 1000000;
        return $"/loki/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={startNs}&end={endNs}&limit={limit}&direction=backward";
    }

    // Existing ParseLokiResponse method... (omitted only for brevity in this replace, need to ensure I don't delete it)
    // Wait, I strictly used 'EndLine: 129' which overwrites everything from 56 onwards including helper class!
    // I must be careful not to delete ParseLokiResponse unless I reprint it.
    // I will rewrite ParseLokiResponse and DTOs below.

    private List<SecurityEvent> ParseLokiResponse(LokiResponse? response)
    {
        var events = new List<SecurityEvent>();
        if (response?.Data?.Result == null) return events;

        foreach (var stream in response.Data.Result)
        {
            var labels = stream.Stream ?? stream.Metric ?? new Dictionary<string, string>();
            
            foreach (var value in stream.Values)
            {
                if (value.Count < 2) continue;
                
                try 
                {
                    // Safe parsing: handle both string and numeric timestamp formats
                    long timestampNs = 0;
                    var tsValue = value[0];
                    if (tsValue is System.Text.Json.JsonElement jsonElement)
                    {
                        timestampNs = jsonElement.ValueKind == System.Text.Json.JsonValueKind.String 
                            ? long.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetInt64();
                    }
                    else if (long.TryParse(tsValue?.ToString(), out var parsed))
                    {
                        timestampNs = parsed;
                    }
                    else continue; // Skip if can't parse timestamp
                    
                    var logLine = value[1]?.ToString() ?? "";
                    var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampNs / 1_000_000).UtcDateTime;
                    
                    // Extract from labels (low cardinality)
                    var eventType = labels.GetValueOrDefault("EventType", "log_event");
                    var severity = labels.GetValueOrDefault("Severity", labels.GetValueOrDefault("Level", "Info"));
                    
                    // Extract IP from log body (high cardinality - not a label anymore)
                    var ip = ExtractIPFromLogLine(logLine);
                    
                    // Generate composite LokiId: timestamp_hash for React key compatibility
                    var lokiId = (int)(timestampNs % int.MaxValue); // Pseudo-unique ID from nanosecond timestamp
                    
                    events.Add(new SecurityEvent
                    {
                        Id = lokiId, // Now unique per event (practically)
                        EventType = eventType,
                        Description = logLine,
                        CreatedAt = timestamp,
                        Severity = severity,
                        IPAddress = ip
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse Loki log entry");
                }
            }
        }
        return events;
    }
    
    /// <summary>
    /// Extracts IP address from structured log line.
    /// Expected format: "... IP:{IPAddress} ..." or JSON with IPAddress field
    /// </summary>
    private static string ExtractIPFromLogLine(string logLine)
    {
        // Try pattern: IP:{value}
        var ipMatch = System.Text.RegularExpressions.Regex.Match(logLine, @"IP:([^\s]+)");
        if (ipMatch.Success) return ipMatch.Groups[1].Value;
        
        // Try JSON extraction
        try
        {
            if (logLine.Contains("\"IPAddress\""))
            {
                var doc = System.Text.Json.JsonDocument.Parse(logLine);
                if (doc.RootElement.TryGetProperty("IPAddress", out var ipProp))
                {
                    return ipProp.GetString() ?? "Unknown";
                }
            }
        }
        catch { /* Not JSON or no IPAddress field */ }
        
        return "Unknown";
    }
}

public class LokiResponse
{
    public string Status { get; set; } = string.Empty;
    public LokiData? Data { get; set; }
}

public class LokiData
{
    public string ResultType { get; set; } = string.Empty;
    public List<LokiStream>? Result { get; set; }
}

public class LokiStream
{
    public Dictionary<string, string>? Stream { get; set; }
    public Dictionary<string, string>? Metric { get; set; } // Added for matrix responses
    public List<List<object>> Values { get; set; } = new();
}
