using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Infrastructure.Services.Logging;

/// <summary>
/// Interface for querying logs from Grafana Loki
/// Enables testability and dependency inversion
/// </summary>
public interface ILokiClient
{
    /// <summary>
    /// Query Loki using LogQL and return parsed SecurityEvents
    /// </summary>
    Task<List<SecurityEvent>> QueryAsync(string query, DateTime start, DateTime end, int limit = 100);

    /// <summary>
    /// Execute a metric query (e.g. count_over_time) and return a scalar value
    /// </summary>
    Task<int> CountAsync(string query, DateTime start, DateTime end);

    /// <summary>
    /// Execute a metric query returning a time series dictionary (label -> value)
    /// </summary>
    Task<Dictionary<string, int>> GetMetricAsync(string query, DateTime start, DateTime end);
}
