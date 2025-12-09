namespace EcommerceLaptop.Core.Configuration;

/// <summary>
/// Elasticsearch configuration settings
/// </summary>
public class ElasticsearchSettings
{
    public string ConnectionString { get; set; } = "http://localhost:9200";
    public string Uri { get; set; } = "http://localhost:9200";
    public string IndexName { get; set; } = "products";
    public int BulkIndexingBatchSize { get; set; } = 1000;
    public TimeSpan IndexingInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan MaxRetryTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableDebugMode { get; set; } = false;
}
