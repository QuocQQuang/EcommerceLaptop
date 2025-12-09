using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Advanced product search service interface
/// Provides comprehensive search capabilities using Elasticsearch
/// </summary>
public interface IProductSearchService
{
    /// <summary>
    /// Performs advanced product search with full-text capabilities
    /// </summary>
    Task<SearchResult<Product>> SearchProductsAsync(ProductSearchRequest request);

    /// <summary>
    /// Gets search suggestions and autocomplete results
    /// </summary>
    Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int maxSuggestions = 10);

    /// <summary>
    /// Performs faceted search with aggregations
    /// </summary>
    Task<FacetedSearchResult<Product>> FacetedSearchAsync(FacetedSearchRequest request);

    /// <summary>
    /// Indexes a single product
    /// </summary>
    Task<bool> IndexProductAsync(Product product);

    /// <summary>
    /// Bulk indexes multiple products
    /// </summary>
    Task<bool> BulkIndexProductsAsync(IEnumerable<Product> products);

    /// <summary>
    /// Removes a product from search index
    /// </summary>
    Task<bool> RemoveProductFromIndexAsync(int productId);

    /// <summary>
    /// Updates product in search index
    /// </summary>
    Task<bool> UpdateProductInIndexAsync(Product product);

    /// <summary>
    /// Creates or updates the search index mapping
    /// </summary>
    Task<bool> CreateOrUpdateIndexAsync();

    /// <summary>
    /// Gets search analytics data
    /// </summary>
    Task<SearchAnalytics> GetSearchAnalyticsAsync(DateTime from, DateTime to);

    /// <summary>
    /// Records search query for analytics
    /// </summary>
    Task RecordSearchQueryAsync(string query, int resultsCount, long responseTime);
}

/// <summary>
/// Product search request model
/// </summary>
public class ProductSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string[]? ProductTypes { get; set; }
    public string[]? Brands { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsActive { get; set; } = true;
    public string? SortBy { get; set; } = "relevance";
    public string? SortDirection { get; set; } = "desc";
    public Dictionary<string, string[]>? Filters { get; set; }
}

/// <summary>
/// Faceted search request with aggregation support
/// </summary>
public class FacetedSearchRequest : ProductSearchRequest
{
    public string[]? FacetFields { get; set; }
    public int MaxFacetValues { get; set; } = 10;
    public bool IncludeZeroCounts { get; set; } = false;
}

/// <summary>
/// Search result wrapper
/// </summary>
public class SearchResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long SearchTime { get; set; } // in milliseconds
    public string? Query { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Faceted search result with aggregations
/// </summary>
public class FacetedSearchResult<T> : SearchResult<T>
{
    public Dictionary<string, FacetResult> Facets { get; set; } = new();
}

/// <summary>
/// Facet result for aggregations
/// </summary>
public class FacetResult
{
    public string Field { get; set; } = string.Empty;
    public IEnumerable<FacetValue> Values { get; set; } = new List<FacetValue>();
    public long TotalCount { get; set; }
}

/// <summary>
/// Individual facet value
/// </summary>
public class FacetValue
{
    public string Value { get; set; } = string.Empty;
    public long Count { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>
/// Search analytics data
/// </summary>
public class SearchAnalytics
{
    public long TotalSearches { get; set; }
    public double AverageResponseTime { get; set; }
    public IEnumerable<TopSearchQuery> TopQueries { get; set; } = new List<TopSearchQuery>();
    public IEnumerable<ZeroResultQuery> ZeroResultQueries { get; set; } = new List<ZeroResultQuery>();
    public Dictionary<string, long> SearchesByDay { get; set; } = new();
}

/// <summary>
/// Top search query data
/// </summary>
public class TopSearchQuery
{
    public string Query { get; set; } = string.Empty;
    public long Count { get; set; }
    public double AverageResultsCount { get; set; }
    public double AverageResponseTime { get; set; }
}

/// <summary>
/// Zero result query for optimization
/// </summary>
public class ZeroResultQuery
{
    public string Query { get; set; } = string.Empty;
    public long Count { get; set; }
    public DateTime LastSearched { get; set; }
}
