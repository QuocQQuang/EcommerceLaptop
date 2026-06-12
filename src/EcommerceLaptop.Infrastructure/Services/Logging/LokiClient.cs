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
                    
                    // Try parsing as CompactJson first, fallback to legacy format
                    var securityEvent = TryParseCompactJson(logLine, timestampNs, timestamp, labels);
                    if (securityEvent != null)
                    {
                        events.Add(securityEvent);
                    }
                    else
                    {
                        // Fallback: legacy text format
                        var eventType = labels.GetValueOrDefault("EventType", "log_event");
                        var severity = labels.GetValueOrDefault("Severity", labels.GetValueOrDefault("Level", "Info"));
                        
                        events.Add(new SecurityEvent
                        {
                            Id = (int)(timestampNs % int.MaxValue),
                            EventType = eventType,
                            Description = logLine,
                            CreatedAt = timestamp,
                            Severity = severity,
                            IPAddress = "Unknown"
                        });
                    }
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
    /// Parses a CompactJson log line (Serilog.Formatting.Compact output).
    /// Format: {"@t":"...","@mt":"template","@m":"rendered",..."Property":"value"...}
    /// </summary>
    private SecurityEvent? TryParseCompactJson(string logLine, long timestampNs, DateTime timestamp, Dictionary<string, string> labels)
    {
        // Quick check if it's JSON
        if (string.IsNullOrEmpty(logLine) || !logLine.TrimStart().StartsWith("{"))
            return null;
            
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(logLine);
            var root = doc.RootElement;
            
            // Description: prefer @m (rendered message), fallback to @mt (template), then raw
            string? description = null;
            if (root.TryGetProperty("@m", out var mProp))
                description = mProp.GetString();
            else if (root.TryGetProperty("@mt", out var mtProp))
                description = mtProp.GetString();
            
            // EventType: from label first (already indexed), then from JSON body
            var eventType = labels.GetValueOrDefault("EventType", "");
            if (string.IsNullOrEmpty(eventType) && root.TryGetProperty("EventType", out var etProp))
                eventType = etProp.GetString() ?? "log_event";
            if (string.IsNullOrEmpty(eventType))
                eventType = "log_event";
            
            // Severity: from label first, then from @l, then fallback
            var severity = labels.GetValueOrDefault("Severity", "");
            if (string.IsNullOrEmpty(severity) && root.TryGetProperty("@l", out var levelProp))
                severity = levelProp.GetString() ?? "Info";
            if (string.IsNullOrEmpty(severity))
                severity = labels.GetValueOrDefault("Level", "Info");
            
            // High-cardinality fields from JSON body (not labels)
            string? ipAddress = null;
            if (root.TryGetProperty("IPAddress", out var ipProp))
                ipAddress = ipProp.GetString();
            else if (root.TryGetProperty("ClientIp", out var clientIpProp))
                ipAddress = clientIpProp.GetString();
            
            int? userId = null;
            if (root.TryGetProperty("UserId", out var uidProp))
            {
                var uidStr = uidProp.ValueKind == System.Text.Json.JsonValueKind.Number 
                    ? uidProp.GetInt32().ToString() 
                    : uidProp.GetString();
                if (int.TryParse(uidStr, out var uid))
                    userId = uid;
            }

            int? adminUserId = null;
            if (root.TryGetProperty("AdminUserId", out var adminUidProp))
            {
                var adminUidStr = adminUidProp.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? adminUidProp.GetInt32().ToString()
                    : adminUidProp.GetString();
                if (int.TryParse(adminUidStr, out var adminUid))
                    adminUserId = adminUid;
            }
            
            string? correlationId = null;
            if (root.TryGetProperty("CorrelationId", out var cidProp))
                correlationId = cidProp.GetString();

            string? details = null;
            if (root.TryGetProperty("Details", out var detailsProp))
                details = detailsProp.ValueKind == System.Text.Json.JsonValueKind.String
                    ? detailsProp.GetString()
                    : detailsProp.ToString();

            string? userAgent = null;
            if (root.TryGetProperty("UserAgent", out var userAgentProp))
                userAgent = userAgentProp.GetString();
            
            return new SecurityEvent
            {
                Id = (int)(timestampNs % int.MaxValue),
                EventType = eventType,
                Description = description ?? logLine, // Fallback to raw if no @m/@mt
                CreatedAt = timestamp,
                Severity = severity,
                IPAddress = ipAddress ?? "Unknown",
                UserId = userId,
                AdminUserId = adminUserId,
                CorrelationId = correlationId,
                Details = details,
                UserAgent = userAgent
            };
        }
        catch
        {
            return null; // Not valid JSON, let caller use fallback
        }
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
