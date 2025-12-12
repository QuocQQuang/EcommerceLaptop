using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Search Request DTOs

/// <summary>
/// Product search request DTO
/// </summary>
public record ProductSearchRequestDto
{
    /// <summary>
    /// Search query string
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Product types to filter by
    /// </summary>
    public string[]? ProductTypes { get; init; }

    /// <summary>
    /// Brands to filter by
    /// </summary>
    public string[]? Brands { get; init; }

    /// <summary>
    /// Minimum price filter
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Minimum price must be non-negative")]
    public decimal? MinPrice { get; init; }

    /// <summary>
    /// Maximum price filter
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Maximum price must be non-negative")]
    public decimal? MaxPrice { get; init; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Sort field
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string? SortDirection { get; init; }

    /// <summary>
    /// Additional filters as key-value pairs
    /// </summary>
    public Dictionary<string, object>? Filters { get; init; }
}

/// <summary>
/// Faceted search request DTO
/// </summary>
public record FacetedSearchRequestDto : ProductSearchRequestDto
{
    /// <summary>
    /// Fields to generate facets for
    /// </summary>
    public string[]? FacetFields { get; init; }

    /// <summary>
    /// Maximum number of facet values per field
    /// </summary>
    [Range(1, 50, ErrorMessage = "Max facet values must be between 1 and 50")]
    public int MaxFacetValues { get; init; } = 10;

    /// <summary>
    /// Include facet values with zero counts
    /// </summary>
    public bool IncludeZeroCounts { get; init; } = false;
}

#endregion

#region Search Response DTOs

/// <summary>
/// Product search response DTO
/// </summary>
public record ProductSearchResponseDto
{
    /// <summary>
    /// Search result items
    /// </summary>
    public IEnumerable<ProductDto> Items { get; init; } = new List<ProductDto>();

    /// <summary>
    /// Total number of results
    /// </summary>
    public long TotalCount { get; init; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Search execution time in milliseconds
    /// </summary>
    public long SearchTime { get; init; }

    /// <summary>
    /// Original search query
    /// </summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// Applied search filters
    /// </summary>
    public Dictionary<string, string[]> Filters { get; init; } = new Dictionary<string, string[]>();
}

/// <summary>
/// Faceted search response DTO
/// </summary>
public record FacetedSearchResponseDto : ProductSearchResponseDto
{
    /// <summary>
    /// Search facets with aggregated values
    /// </summary>
    public Dictionary<string, FacetResultDto> Facets { get; init; } = new Dictionary<string, FacetResultDto>();
}

/// <summary>
/// Facet result DTO
/// </summary>
public record FacetResultDto
{
    /// <summary>
    /// Facet field name
    /// </summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// Facet values with counts
    /// </summary>
    public IEnumerable<FacetValueDto> Values { get; init; } = new List<FacetValueDto>();

    /// <summary>
    /// Total count of all facet values
    /// </summary>
    public long TotalCount { get; init; }
}

/// <summary>
/// Individual facet value DTO
/// </summary>
public record FacetValueDto
{
    /// <summary>
    /// Facet value
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Number of documents with this value
    /// </summary>
    public long Count { get; init; }

    /// <summary>
    /// Whether this value is currently selected in the search
    /// </summary>
    public bool IsSelected { get; init; }
}

#endregion

#region Analytics DTOs

/// <summary>
/// Search analytics response DTO
/// </summary>
public record SearchAnalyticsDto
{
    /// <summary>
    /// Total number of searches in the period
    /// </summary>
    public long TotalSearches { get; init; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; init; }

    /// <summary>
    /// Top search queries
    /// </summary>
    public IEnumerable<TopSearchQueryDto> TopQueries { get; init; } = new List<TopSearchQueryDto>();

    /// <summary>
    /// Queries with zero results
    /// </summary>
    public IEnumerable<ZeroResultQueryDto> ZeroResultQueries { get; init; } = new List<ZeroResultQueryDto>();

    /// <summary>
    /// Daily search counts
    /// </summary>
    public Dictionary<string, long> SearchesByDay { get; init; } = new Dictionary<string, long>();
}

/// <summary>
/// Top search query DTO
/// </summary>
public record TopSearchQueryDto
{
    /// <summary>
    /// Search query text
    /// </summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// Number of times this query was searched
    /// </summary>
    public long Count { get; init; }

    /// <summary>
    /// Average number of results returned
    /// </summary>
    public double AverageResultsCount { get; init; }

    /// <summary>
    /// Average response time for this query
    /// </summary>
    public double AverageResponseTime { get; init; }
}

/// <summary>
/// Zero result query DTO
/// </summary>
public record ZeroResultQueryDto
{
    /// <summary>
    /// Search query that returned no results
    /// </summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>
    /// Number of times this query was attempted
    /// </summary>
    public long Count { get; init; }

    /// <summary>
    /// Last time this query was searched
    /// </summary>
    public DateTime LastSearched { get; init; }
}

#endregion

#region Search Service Models

/// <summary>
/// Product search request for the service layer
/// </summary>
public record ProductSearchRequest
{
    public string Query { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string[]? ProductTypes { get; init; }
    public string[]? Brands { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? IsActive { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
    public Dictionary<string, object>? Filters { get; init; }
}

/// <summary>
/// Faceted search request for the service layer
/// </summary>
public record FacetedSearchRequest : ProductSearchRequest
{
    public string[]? FacetFields { get; init; }
    public int MaxFacetValues { get; init; } = 10;
    public bool IncludeZeroCounts { get; init; } = false;
}

/// <summary>
/// Product search result for the service layer
/// </summary>
public record ProductSearchResult
{
    public IEnumerable<EcommerceLaptop.Core.Entities.Product> Items { get; init; } = new List<EcommerceLaptop.Core.Entities.Product>();
    public long TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public long SearchTime { get; init; }
    public string Query { get; init; } = string.Empty;
}

/// <summary>
/// Faceted search result for the service layer
/// </summary>
public record FacetedSearchResult : ProductSearchResult
{
    public Dictionary<string, FacetResult> Facets { get; init; } = new Dictionary<string, FacetResult>();
}

/// <summary>
/// Facet result for the service layer
/// </summary>
public record FacetResult
{
    public string Field { get; init; } = string.Empty;
    public IEnumerable<FacetValue> Values { get; init; } = new List<FacetValue>();
    public long TotalCount { get; init; }
}

/// <summary>
/// Individual facet value for the service layer
/// </summary>
public record FacetValue
{
    public string Value { get; init; } = string.Empty;
    public long Count { get; init; }
    public bool IsSelected { get; init; }
}

/// <summary>
/// Search analytics result for the service layer
/// </summary>
public record SearchAnalyticsResult
{
    public long TotalSearches { get; init; }
    public double AverageResponseTime { get; init; }
    public IEnumerable<TopSearchQuery> TopQueries { get; init; } = new List<TopSearchQuery>();
    public IEnumerable<ZeroResultQuery> ZeroResultQueries { get; init; } = new List<ZeroResultQuery>();
    public Dictionary<string, long> SearchesByDay { get; init; } = new Dictionary<string, long>();
}

/// <summary>
/// Top search query for the service layer
/// </summary>
public record TopSearchQuery
{
    public string Query { get; init; } = string.Empty;
    public long Count { get; init; }
    public double AverageResultsCount { get; init; }
    public double AverageResponseTime { get; init; }
}

/// <summary>
/// Zero result query for the service layer
/// </summary>
public record ZeroResultQuery
{
    public string Query { get; init; } = string.Empty;
    public long Count { get; init; }
    public DateTime LastSearched { get; init; }
}

#endregion
