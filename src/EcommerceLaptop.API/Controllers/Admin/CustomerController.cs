using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.API.Authorization;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.Controllers.Admin;

/// <summary>
/// Customer management API controller with comprehensive security and privacy controls
/// Provides full CRUD operations for customer management with role-based permissions
/// </summary>
[ApiController]
[Route("api/admin/customers")]
[RequireAdmin]
public class CustomerController : ControllerBase
{
    private readonly ICustomerManagementService _customerManagementService;
    private readonly IAuthService _authService;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(
        ICustomerManagementService customerManagementService,
        IAuthService authService,
        ILogger<CustomerController> logger)
    {
        _customerManagementService = customerManagementService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated list of customers with search and filtering
    /// Requires customers:read permission
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CustomerManagementDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [RequireAdminPermission("customers:read")]
    public async Task<ActionResult<PagedResult<CustomerManagementDto>>> GetCustomers(
        [FromQuery] CustomerSearchParameters parameters)
    {
        try
        {
            var result = await _customerManagementService.GetCustomersAsync(parameters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers with parameters: {@Parameters}", parameters);
            return StatusCode(500, new { message = "An error occurred while retrieving customers" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific customer
    /// Requires customers:read permission, with permission-based field filtering
    /// </summary>
    [HttpGet("{customerId}")]
    [ProducesResponseType(typeof(CustomerDetailDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [RequireAdminPermission("customers:read")]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomerDetail(
        [FromRoute] int customerId)
    {
        try
        {
            var requesterUserId = GetCurrentUserId();
            var customer = await _customerManagementService.GetCustomerDetailAsync(customerId, requesterUserId);

            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            return Ok(customer);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer detail for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while retrieving customer details" });
        }
    }

    /// <summary>
    /// Update customer information
    /// Requires customers:write permission, with some fields requiring customers:manage
    /// </summary>
    [HttpPut("{customerId}")]
    [ProducesResponseType(typeof(CustomerDetailDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [RequireAdminPermission("customers:write")]
    public async Task<ActionResult<CustomerDetailDto>> UpdateCustomer(
        [FromRoute] int customerId,
        [FromBody] UpdateCustomerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedByUserId = GetCurrentUserId();
            var updatedCustomer = await _customerManagementService.UpdateCustomerAsync(
                customerId, request, updatedByUserId);

            if (updatedCustomer == null)
            {
                return NotFound(new { message = "Customer not found or cannot be updated" });
            }

            return Ok(updatedCustomer);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while updating customer" });
        }
    }

    /// <summary>
    /// Deactivate a customer account
    /// Requires customers:delete permission
    /// </summary>
    [HttpPost("{customerId}/deactivate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [RequireAdminPermission("customers:delete")]
    public async Task<IActionResult> DeactivateCustomer(
        [FromRoute] int customerId,
        [FromBody] DeactivateCustomerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var deactivatedByUserId = GetCurrentUserId();
            var result = await _customerManagementService.DeactivateCustomerAsync(
                customerId, deactivatedByUserId, request.Reason);

            if (!result)
            {
                return NotFound(new { message = "Customer not found or already inactive" });
            }

            return Ok(new { message = "Customer deactivated successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while deactivating customer" });
        }
    }

    /// <summary>
    /// Reactivate a customer account
    /// Requires customers:manage permission
    /// </summary>
    [HttpPost("{customerId}/reactivate")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [RequireAdminPermission("customers:manage")]
    public async Task<IActionResult> ReactivateCustomer(
        [FromRoute] int customerId,
        [FromBody] ReactivateCustomerRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var reactivatedByUserId = GetCurrentUserId();
            var result = await _customerManagementService.ReactivateCustomerAsync(
                customerId, reactivatedByUserId, request.Reason);

            if (!result)
            {
                return NotFound(new { message = "Customer not found or already active" });
            }

            return Ok(new { message = "Customer reactivated successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while reactivating customer" });
        }
    }

    /// <summary>
    /// Get customer order history and statistics
    /// Requires customers:read permission
    /// </summary>
    [HttpGet("{customerId}/orders")]
    [ProducesResponseType(typeof(CustomerOrderHistoryDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [RequireAdminPermission("customers:read")]
    public async Task<ActionResult<CustomerOrderHistoryDto>> GetCustomerOrderHistory(
        [FromRoute] int customerId)
    {
        try
        {
            var requesterUserId = GetCurrentUserId();
            var orderHistory = await _customerManagementService.GetCustomerOrderHistoryAsync(
                customerId, requesterUserId);

            return Ok(orderHistory);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException)
        {
            return NotFound(new { message = "Customer not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order history for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while retrieving order history" });
        }
    }

    /// <summary>
    /// Get customer activity logs
    /// Requires customers:manage permission
    /// </summary>
    [HttpGet("{customerId}/activity-logs")]
    [ProducesResponseType(typeof(PagedResult<CustomerActivityLogDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [RequireAdminPermission("customers:manage")]
    public async Task<ActionResult<PagedResult<CustomerActivityLogDto>>> GetCustomerActivityLogs(
        [FromRoute] int customerId,
        [FromQuery] ActivityLogParameters parameters)
    {
        try
        {
            var requesterUserId = GetCurrentUserId();
            var logs = await _customerManagementService.GetCustomerActivityLogsAsync(
                customerId, requesterUserId, parameters);

            return Ok(logs);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity logs for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while retrieving activity logs" });
        }
    }

    /// <summary>
    /// Send notification to customer
    /// Requires customers:manage permission
    /// </summary>
    [HttpPost("{customerId}/notifications")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [RequireAdminPermission("customers:manage")]
    public async Task<IActionResult> SendCustomerNotification(
        [FromRoute] int customerId,
        [FromBody] CustomerNotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sentByUserId = GetCurrentUserId();
            var result = await _customerManagementService.SendCustomerNotificationAsync(
                customerId, request, sentByUserId);

            if (!result)
            {
                return NotFound(new { message = "Customer not found or notification failed" });
            }

            return Ok(new { message = "Notification sent successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while sending notification" });
        }
    }

    /// <summary>
    /// Get overall customer statistics
    /// Requires customers:read permission
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(CustomerStatisticsDto), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [RequireAdminPermission("customers:read")]
    public async Task<ActionResult<CustomerStatisticsDto>> GetCustomerStatistics()
    {
        try
        {
            var statistics = await _customerManagementService.GetCustomerStatisticsAsync();
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
        }
    }

    /// <summary>
    /// Export customer data
    /// Requires customers:export permission
    /// </summary>
    [HttpPost("export")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [Authorize(Policy = "RequirePermission:customers:export")]
    public async Task<IActionResult> ExportCustomers(
        [FromBody] CustomerExportRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var requesterUserId = GetCurrentUserId();
            var exportResult = await _customerManagementService.ExportCustomersAsync(
                request.Parameters, requesterUserId, request.Format);

            return File(exportResult.Data, exportResult.ContentType, exportResult.FileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting customers");
            return StatusCode(500, new { message = "An error occurred while exporting customer data" });
        }
    }

    /// <summary>
    /// Bulk update customers (for admin operations)
    /// Requires customers:manage permission
    /// </summary>
    [HttpPost("bulk-update")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [Authorize(Policy = "RequirePermission:customers:manage")]
    public async Task<IActionResult> BulkUpdateCustomers(
        [FromBody] BulkCustomerUpdateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedByUserId = GetCurrentUserId();
            var results = new List<object>();

            foreach (var customerId in request.CustomerIds)
            {
                try
                {
                    var result = await _customerManagementService.UpdateCustomerAsync(
                        customerId, request.UpdateData, updatedByUserId);

                    results.Add(new
                    {
                        CustomerId = customerId,
                        Success = result != null,
                        Message = result != null ? "Updated successfully" : "Customer not found or update failed"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating customer {CustomerId} in bulk operation", customerId);
                    results.Add(new
                    {
                        CustomerId = customerId,
                        Success = false,
                        Message = "Update failed due to an error"
                    });
                }
            }

            return Ok(new
            {
                Message = "Bulk update completed",
                Results = results,
                TotalRequested = request.CustomerIds.Count,
                SuccessCount = results.Count(r => (bool)r.GetType().GetProperty("Success")!.GetValue(r)!),
                FailedCount = results.Count(r => !(bool)r.GetType().GetProperty("Success")!.GetValue(r)!)
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk customer update");
            return StatusCode(500, new { message = "An error occurred during bulk update" });
        }
    }

    /// <summary>
    /// Search customers with advanced filters
    /// Requires customers:read permission
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedResult<CustomerManagementDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [Authorize(Policy = "RequirePermission:customers:read")]
    public async Task<ActionResult<PagedResult<CustomerManagementDto>>> SearchCustomers(
        [FromBody] CustomerAdvancedSearchRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Convert advanced search to basic search parameters
            var parameters = new CustomerSearchParameters
            {
                SearchTerm = request.SearchTerm,
                Email = request.Email,
                IsActive = request.IsActive,
                EmailVerified = request.EmailVerified,
                RegisteredFrom = request.RegisteredFrom,
                RegisteredTo = request.RegisteredTo,
                MinTotalSpent = request.MinTotalSpent,
                MaxTotalSpent = request.MaxTotalSpent,
                VipTierId = request.VipTierId,
                Page = request.Page,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortOrder = request.SortOrder
            };

            var result = await _customerManagementService.GetCustomersAsync(parameters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in advanced customer search");
            return StatusCode(500, new { message = "An error occurred during customer search" });
        }
    }

    /// <summary>
    /// Get customer summary for quick overview
    /// Requires customers:read permission
    /// </summary>
    [HttpGet("{customerId}/summary")]
    [ProducesResponseType(typeof(CustomerSummaryDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [Authorize(Policy = "RequirePermission:customers:read")]
    public async Task<ActionResult<CustomerSummaryDto>> GetCustomerSummary(
        [FromRoute] int customerId)
    {
        try
        {
            var requesterUserId = GetCurrentUserId();
            var customerDetail = await _customerManagementService.GetCustomerDetailAsync(customerId, requesterUserId);

            if (customerDetail == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            // Create summary from detail data
            var summary = new CustomerSummaryDto
            {
                Id = customerDetail.Id,
                FullName = $"{customerDetail.FirstName} {customerDetail.LastName}",
                Email = customerDetail.Email,
                IsActive = customerDetail.IsActive,
                EmailConfirmed = customerDetail.EmailConfirmed,
                TotalOrders = customerDetail.TotalOrders,
                TotalSpent = customerDetail.TotalSpent,
                VipTierName = customerDetail.VipTierName,
                LastOrderDate = customerDetail.LastOrderDate,
                CreatedAt = customerDetail.CreatedAt,
                LastLoginAt = customerDetail.LastLoginAt
            };

            return Ok(summary);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer summary for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while retrieving customer summary" });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
                         User.FindFirst("user_id")?.Value ??
                         User.FindFirst("UserId")?.Value ??
                         User.FindFirst("id")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user authentication");
        }

        return userId;
    }
}

// Supporting DTOs for request/response
public class DeactivateCustomerRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;
}

public class ReactivateCustomerRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string Reason { get; set; } = string.Empty;
}

public class CustomerExportRequest
{
    public CustomerExportParameters Parameters { get; set; } = new();
    public ExportFormat Format { get; set; } = ExportFormat.CSV;
}

public class BulkCustomerUpdateRequest
{
    [Required]
    public List<int> CustomerIds { get; set; } = new();

    [Required]
    public UpdateCustomerRequest UpdateData { get; set; } = new();
}

public class CustomerAdvancedSearchRequest : CustomerSearchParameters
{
    // Additional advanced search fields can be added here
    public List<string>? Tags { get; set; }
    public List<int>? ExcludeCustomerIds { get; set; }
    public bool? HasOrders { get; set; }
    public DateTime? LastLoginBefore { get; set; }
    public DateTime? LastLoginAfter { get; set; }
}

public class CustomerSummaryDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public string? VipTierName { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
