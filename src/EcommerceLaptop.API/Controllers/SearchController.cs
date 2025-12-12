using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.API.Controllers;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Advanced product search controller
/// Provides comprehensive search capabilities using Elasticsearch
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SearchController(
    IProductSearchService searchService,
    IAsyncRepository<Product> productRepository,
    ILogger<SearchController> logger) : BaseApiController(logger)
{
    private readonly IProductSearchService _searchService = searchService;
    private readonly IAsyncRepository<Product> _productRepository = productRepository;

    /// <summary>
    /// Performs advanced product search with full-text capabilities
    /// </summary>
    /// <param name="request">Search parameters including query, filters, and pagination</param>
    /// <returns>Search results with products and metadata</returns>
    [HttpPost("products")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchProducts([FromBody] ProductSearchRequestDto request)
    {
        // Manual mapping from DTO to Service Model
        // In a real app, AutoMapper would simplify this
        var searchRequest = new EcommerceLaptop.Core.Services.ProductSearchRequest
        {
            Query = request.Query ?? string.Empty,
            Page = Math.Max(1, request.Page),
            PageSize = Math.Min(100, Math.Max(1, request.PageSize)),
            ProductTypes = request.ProductTypes,
            Brands = request.Brands,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            IsActive = request.IsActive ?? true,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection,
            Filters = ConvertFilters(request.Filters)
        };

        var result = await _searchService.SearchProductsAsync(searchRequest);

        var response = new ProductSearchResponseDto
        {
            Items = result.Items.Select(MapToProductDto),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            SearchTime = result.SearchTime,
            Query = result.Query ?? string.Empty,
            Filters = new Dictionary<string, string[]>() // Basic search doesn't have facets
        };

        return SuccessResponse(response);
    }

    /// <summary>
    /// Gets search suggestions and autocomplete results
    /// </summary>
    /// <param name="query">Query string for suggestions</param>
    /// <param name="maxSuggestions">Maximum number of suggestions (default: 10)</param>
    /// <returns>List of search suggestions</returns>
    [HttpGet("suggestions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSearchSuggestions(
        [FromQuery] string query,
        [FromQuery] int maxSuggestions = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return SuccessResponse(new string[0]);
        }

        maxSuggestions = Math.Min(20, Math.Max(1, maxSuggestions));

        var suggestions = await _searchService.GetSearchSuggestionsAsync(query, maxSuggestions);

        return SuccessResponse(suggestions);
    }

    /// <summary>
    /// Performs faceted search with aggregations for dynamic filtering
    /// </summary>
    /// <param name="request">Faceted search parameters</param>
    /// <returns>Search results with facet aggregations</returns>
    [HttpPost("faceted")]
    [AllowAnonymous]
    public async Task<IActionResult> FacetedSearch([FromBody] FacetedSearchRequestDto request)
    {
        var searchRequest = new EcommerceLaptop.Core.Services.FacetedSearchRequest
        {
            Query = request.Query ?? string.Empty,
            Page = Math.Max(1, request.Page),
            PageSize = Math.Min(100, Math.Max(1, request.PageSize)),
            ProductTypes = request.ProductTypes,
            Brands = request.Brands,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            IsActive = request.IsActive ?? true,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection,
            Filters = ConvertFilters(request.Filters),
            FacetFields = request.FacetFields,
            MaxFacetValues = Math.Min(50, Math.Max(1, request.MaxFacetValues)),
            IncludeZeroCounts = request.IncludeZeroCounts
        };

        var result = await _searchService.FacetedSearchAsync(searchRequest);

        var filters = new Dictionary<string, string[]>();
        foreach (var kvp in result.Facets)
        {
            if (kvp.Value.Values.Any(v => v.IsSelected))
            {
                filters[kvp.Key] = kvp.Value.Values
                    .Where(v => v.IsSelected)
                    .Select(v => v.Value)
                    .ToArray();
            }
        }

        var response = new FacetedSearchResponseDto
        {
            Items = result.Items.Select(MapToProductDto),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            SearchTime = result.SearchTime,
            Query = result.Query ?? string.Empty,
            Facets = result.Facets.ToDictionary(
                f => f.Key,
                f => new FacetResultDto
                {
                    Field = f.Value.Field,
                    Values = f.Value.Values.Select(v => new FacetValueDto
                    {
                        Value = v.Value,
                        Count = v.Count,
                        IsSelected = v.IsSelected
                    }),
                    TotalCount = f.Value.TotalCount
                }),
            Filters = filters
        };

        return SuccessResponse(response);
    }

    /// <summary>
    /// Quick search endpoint for simple queries
    /// </summary>
    /// <param name="q">Search query</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="size">Page size (default: 20)</param>
    /// <param name="type">Product type filter</param>
    /// <param name="brand">Brand filter</param>
    /// <returns>Simple search results</returns>
    [HttpGet("quick")]
    [AllowAnonymous]
    public async Task<IActionResult> QuickSearch(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? type = null,
        [FromQuery] string? brand = null)
    {
        var searchRequest = new EcommerceLaptop.Core.Services.ProductSearchRequest
        {
            Query = q ?? string.Empty,
            Page = Math.Max(1, page),
            PageSize = Math.Min(100, Math.Max(1, size)),
            ProductTypes = !string.IsNullOrEmpty(type) ? new[] { type } : null,
            Brands = !string.IsNullOrEmpty(brand) ? new[] { brand } : null,
            IsActive = true
        };

        var result = await _searchService.SearchProductsAsync(searchRequest);

        return SuccessResponse(new
        {
            items = result.Items.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                brand = p.Brand,
                price = p.Price,
                type = p.GetType().Name
            }),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize,
            searchTime = result.SearchTime
        });
    }

    /// <summary>
    /// Gets search analytics data (Admin only)
    /// </summary>
    /// <param name="from">Start date for analytics period</param>
    /// <param name="to">End date for analytics period</param>
    /// <returns>Search analytics data</returns>
    [HttpGet("analytics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSearchAnalytics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        if (fromDate > toDate)
        {
            return BadRequest("From date cannot be greater than to date");
        }

        var analytics = await _searchService.GetSearchAnalyticsAsync(fromDate, toDate);

        var response = new
        {
            TotalSearches = analytics.TotalSearches,
            AverageResponseTime = analytics.AverageResponseTime,
            TopQueries = analytics.TopQueries.Select(q => new
            {
                Query = q.Query,
                Count = q.Count,
                AverageResultsCount = q.AverageResultsCount,
                AverageResponseTime = q.AverageResponseTime
            }),
            ZeroResultQueries = analytics.ZeroResultQueries.Select(q => new
            {
                Query = q.Query,
                Count = q.Count,
                LastSearched = q.LastSearched
            }),
            SearchesByDay = analytics.SearchesByDay,
            Period = new
            {
                From = fromDate,
                To = toDate
            }
        };

        return SuccessResponse(response);
    }

    /// <summary>
    /// Creates or updates the search index (Admin only)
    /// </summary>
    /// <returns>Index creation status</returns>
    [HttpPost("index/create")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSearchIndex()
    {
        var success = await _searchService.CreateOrUpdateIndexAsync();

        if (success)
        {
            return SuccessResponse(new { message = "Search index created/updated successfully" });
        }
        else
        {
            return ErrorResponse("Failed to create search index");
        }
    }

    /// <summary>
    /// Indexes a specific product (Admin only)
    /// </summary>
    /// <param name="productId">Product ID to index</param>
    /// <returns>Indexing status</returns>
    [HttpPost("index/product/{productId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> IndexProduct(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {productId} not found" });
        }

        await _searchService.IndexProductAsync(product);
        
        return SuccessResponse(new { 
            message = $"Product indexing queued for product ID: {productId}",
            productId = productId
        });
    }

    /// <summary>
    /// Removes a product from search index (Admin only)
    /// </summary>
    /// <param name="productId">Product ID to remove</param>
    /// <returns>Removal status</returns>
    [HttpDelete("index/product/{productId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveProductFromIndex(int productId)
    {
        var success = await _searchService.RemoveProductFromIndexAsync(productId);

        if (success)
        {
            return SuccessResponse(new { 
                message = $"Product removed from search index",
                productId = productId
            });
        }
        else
        {
            return ErrorResponse("Failed to remove product from search index");
        }
    }

    #region Private Helper Methods

    private ProductDto MapToProductDto(Product product)
    {
        // Simplified mapping - in practice, you might use AutoMapper or a more comprehensive mapping
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Brand = product.Brand,
            Model = product.Model,
            Price = product.Price,
            SKU = product.SKU,
            IsActive = product.IsActive,
            ProductType = product.GetType().Name,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    private Dictionary<string, string[]>? ConvertFilters(Dictionary<string, object>? filters)
    {
        if (filters == null || filters.Count == 0)
            return null;

        var result = new Dictionary<string, string[]>();
        
        foreach (var kvp in filters)
        {
            if (kvp.Value == null)
                continue;

            if (kvp.Value is string[] stringArray)
            {
                result[kvp.Key] = stringArray;
            }
            else if (kvp.Value is string singleString)
            {
                result[kvp.Key] = new[] { singleString };
            }
            else if (kvp.Value is IEnumerable<object> objectArray)
            {
                result[kvp.Key] = objectArray.Select(obj => obj?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToArray();
            }
            else
            {
                result[kvp.Key] = new[] { kvp.Value.ToString() ?? "" };
            }
        }

        return result.Count > 0 ? result : null;
    }

    #endregion
}
