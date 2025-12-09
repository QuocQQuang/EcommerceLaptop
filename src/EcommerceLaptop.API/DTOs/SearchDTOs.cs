using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Search Request DTOs

/// <summary>
/// Product search request DTO
/// </summary>
public class ProductSearchRequestDto
{
    /// <summary>
    /// Search query string
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be at least 1")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Product types to filter by
    /// </summary>
    public string[]? ProductTypes { get; set; }

    /// <summary>
    /// Brands to filter by
    /// </summary>
    public string[]? Brands { get; set; }

    /// <summary>
    /// Minimum price filter
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Minimum price must be non-negative")]
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price filter
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Maximum price must be non-negative")]
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Sort field
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string? SortDirection { get; set; }

    /// <summary>
    /// Additional filters as key-value pairs
    /// </summary>
    public Dictionary<string, object>? Filters { get; set; }
}

/// <summary>
/// Faceted search request DTO
/// </summary>
public class FacetedSearchRequestDto : ProductSearchRequestDto
{
    /// <summary>
    /// Fields to generate facets for
    /// </summary>
    public string[]? FacetFields { get; set; }

    /// <summary>
    /// Maximum number of facet values per field
    /// </summary>
    [Range(1, 50, ErrorMessage = "Max facet values must be between 1 and 50")]
    public int MaxFacetValues { get; set; } = 10;

    /// <summary>
    /// Include facet values with zero counts
    /// </summary>
    public bool IncludeZeroCounts { get; set; } = false;
}

#endregion

#region Search Response DTOs

/// <summary>
/// Product search response DTO
/// <summary>
/// Product search response DTO
/// </summary>
public class ProductSearchResponseDto
{
    /// <summary>
    /// Search result items
    /// </summary>
    public IEnumerable<ProductDto> Items { get; set; } = new List<ProductDto>();

    /// <summary>
    /// Total number of results
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Search execution time in milliseconds
    /// </summary>
    public long SearchTime { get; set; }

    /// <summary>
    /// Original search query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Applied search filters
    /// </summary>
    public Dictionary<string, string[]> Filters { get; set; } = new Dictionary<string, string[]>();
}

/// <summary>
/// Faceted search response DTO
/// </summary>
public class FacetedSearchResponseDto : ProductSearchResponseDto
{
    /// <summary>
    /// Search facets with aggregated values
    /// </summary>
    public Dictionary<string, FacetResultDto> Facets { get; set; } = new Dictionary<string, FacetResultDto>();
}

/// <summary>
/// Facet result DTO
/// </summary>
public class FacetResultDto
{
    /// <summary>
    /// Facet field name
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Facet values with counts
    /// </summary>
    public IEnumerable<FacetValueDto> Values { get; set; } = new List<FacetValueDto>();

    /// <summary>
    /// Total count of all facet values
    /// </summary>
    public long TotalCount { get; set; }
}

/// <summary>
/// Individual facet value DTO
/// </summary>
public class FacetValueDto
{
    /// <summary>
    /// Facet value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Number of documents with this value
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Whether this value is currently selected in the search
    /// </summary>
    public bool IsSelected { get; set; }
}

#endregion

#region Analytics DTOs

/// <summary>
/// Search analytics response DTO
/// </summary>
public class SearchAnalyticsDto
{
    /// <summary>
    /// Total number of searches in the period
    /// </summary>
    public long TotalSearches { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Top search queries
    /// </summary>
    public IEnumerable<TopSearchQueryDto> TopQueries { get; set; } = new List<TopSearchQueryDto>();

    /// <summary>
    /// Queries with zero results
    /// </summary>
    public IEnumerable<ZeroResultQueryDto> ZeroResultQueries { get; set; } = new List<ZeroResultQueryDto>();

    /// <summary>
    /// Daily search counts
    /// </summary>
    public Dictionary<string, long> SearchesByDay { get; set; } = new Dictionary<string, long>();
}

/// <summary>
/// Top search query DTO
/// </summary>
public class TopSearchQueryDto
{
    /// <summary>
    /// Search query text
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Number of times this query was searched
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Average number of results returned
    /// </summary>
    public double AverageResultsCount { get; set; }

    /// <summary>
    /// Average response time for this query
    /// </summary>
    public double AverageResponseTime { get; set; }
}

/// <summary>
/// Zero result query DTO
/// </summary>
public class ZeroResultQueryDto
{
    /// <summary>
    /// Search query that returned no results
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Number of times this query was attempted
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Last time this query was searched
    /// </summary>
    public DateTime LastSearched { get; set; }
}

#endregion

#region Search Service Models

/// <summary>
/// Product search request for the service layer
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
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public Dictionary<string, object>? Filters { get; set; }
}

/// <summary>
/// Faceted search request for the service layer
/// </summary>
public class FacetedSearchRequest : ProductSearchRequest
{
    public string[]? FacetFields { get; set; }
    public int MaxFacetValues { get; set; } = 10;
    public bool IncludeZeroCounts { get; set; } = false;
}

/// <summary>
/// Product search result for the service layer
/// </summary>
public class ProductSearchResult
{
    public IEnumerable<EcommerceLaptop.Core.Entities.Product> Items { get; set; } = new List<EcommerceLaptop.Core.Entities.Product>();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public long SearchTime { get; set; }
    public string Query { get; set; } = string.Empty;
}

/// <summary>
/// Faceted search result for the service layer
/// </summary>
public class FacetedSearchResult : ProductSearchResult
{
    public Dictionary<string, FacetResult> Facets { get; set; } = new Dictionary<string, FacetResult>();
}

/// <summary>
/// Facet result for the service layer
/// </summary>
public class FacetResult
{
    public string Field { get; set; } = string.Empty;
    public IEnumerable<FacetValue> Values { get; set; } = new List<FacetValue>();
    public long TotalCount { get; set; }
}

/// <summary>
/// Individual facet value for the service layer
/// </summary>
public class FacetValue
{
    public string Value { get; set; } = string.Empty;
    public long Count { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>
/// Search analytics result for the service layer
/// </summary>
public class SearchAnalyticsResult
{
    public long TotalSearches { get; set; }
    public double AverageResponseTime { get; set; }
    public IEnumerable<TopSearchQuery> TopQueries { get; set; } = new List<TopSearchQuery>();
    public IEnumerable<ZeroResultQuery> ZeroResultQueries { get; set; } = new List<ZeroResultQuery>();
    public Dictionary<string, long> SearchesByDay { get; set; } = new Dictionary<string, long>();
}

/// <summary>
/// Top search query for the service layer
/// </summary>
public class TopSearchQuery
{
    public string Query { get; set; } = string.Empty;
    public long Count { get; set; }
    public double AverageResultsCount { get; set; }
    public double AverageResponseTime { get; set; }
}

/// <summary>
/// Zero result query for the service layer
/// </summary>
public class ZeroResultQuery
{
    public string Query { get; set; } = string.Empty;
    public long Count { get; set; }
    public DateTime LastSearched { get; set; }
}

#endregion
