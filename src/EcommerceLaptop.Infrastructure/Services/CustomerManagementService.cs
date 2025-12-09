using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services.Security;
// Use DTOs namespace for export types to avoid ambiguity
using ExportResult = EcommerceLaptop.Core.DTOs.ExportResult;
using ExportFormat = EcommerceLaptop.Core.DTOs.ExportFormat;
using ActivityLogParameters = EcommerceLaptop.Core.DTOs.ActivityLogParameters;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Customer management service implementation with comprehensive security and privacy controls
/// Implements GDPR-compliant data access with role-based permissions
/// </summary>
public class CustomerManagementService : ICustomerManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IEmailService _emailService;
    private readonly ILogger<CustomerManagementService> _logger;

    // Permission constants for customer management
    private const string CUSTOMERS_READ = "customers:read";
    private const string CUSTOMERS_WRITE = "customers:write";
    private const string CUSTOMERS_DELETE = "customers:delete";
    private const string CUSTOMERS_MANAGE = "customers:manage";
    private const string CUSTOMERS_PERSONAL_DATA = "customers:personal-data";
    private const string CUSTOMERS_SECURITY_INFO = "customers:security-info";
    private const string CUSTOMERS_EXPORT = "customers:export";

    public CustomerManagementService(
        ApplicationDbContext context,
        IAuthService authService,
        IAuditLoggingService auditLoggingService,
        IEmailService emailService,
        ILogger<CustomerManagementService> logger)
    {
        _context = context;
        _authService = authService;
        _auditLoggingService = auditLoggingService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<PagedResult<CustomerManagementDto>> GetCustomersAsync(CustomerSearchParameters parameters)
    {
        try
        {
            var query = _context.Users
                .Where(u => !u.IsAdminRole) // Customers only - use direct property
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.VipTier)
                .Include(u => u.Orders)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(parameters.Email))
            {
                query = query.Where(u => u.Email.ToLower().Contains(parameters.Email.ToLower()));
            }

            if (parameters.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == parameters.IsActive.Value);
            }

            if (parameters.EmailVerified.HasValue)
            {
                query = query.Where(u => u.EmailConfirmed == parameters.EmailVerified.Value);
            }

            if (parameters.RegisteredFrom.HasValue)
            {
                query = query.Where(u => u.CreatedAt >= parameters.RegisteredFrom.Value);
            }

            if (parameters.RegisteredTo.HasValue)
            {
                query = query.Where(u => u.CreatedAt <= parameters.RegisteredTo.Value);
            }

            if (parameters.MinTotalSpent.HasValue)
            {
                query = query.Where(u => u.TotalSpent >= parameters.MinTotalSpent.Value);
            }

            if (parameters.MaxTotalSpent.HasValue)
            {
                query = query.Where(u => u.TotalSpent <= parameters.MaxTotalSpent.Value);
            }

            if (parameters.VipTierId.HasValue)
            {
                query = query.Where(u => u.VipTierId == parameters.VipTierId.Value);
            }

            // Apply sorting
            query = parameters.SortBy?.ToLower() switch
            {
                "firstname" => parameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.FirstName)
                    : query.OrderBy(u => u.FirstName),
                "lastname" => parameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.LastName)
                    : query.OrderBy(u => u.LastName),
                "email" => parameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.Email)
                    : query.OrderBy(u => u.Email),
                "totalspent" => parameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.TotalSpent)
                    : query.OrderBy(u => u.TotalSpent),
                "lastloginat" => parameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.LastLoginAt)
                    : query.OrderBy(u => u.LastLoginAt),
                _ => parameters.SortOrder?.ToLower() == "desc"
                    ? query.OrderByDescending(u => u.CreatedAt)
                    : query.OrderBy(u => u.CreatedAt)
            };

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var customers = await query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(u => new CustomerManagementDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    MaskedEmail = MaskEmail(u.Email),
                    PhoneNumber = u.PhoneNumber,
                    MaskedPhoneNumber = MaskPhoneNumber(u.PhoneNumber),
                    IsActive = u.IsActive,
                    EmailConfirmed = u.EmailConfirmed,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    TotalOrders = u.Orders.Count,
                    TotalSpent = u.TotalSpent,
                    LastOrderDate = u.Orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault()!.CreatedAt,
                    VipTierId = u.VipTierId,
                    VipTierName = u.VipTier != null ? u.VipTier.Name : null,
                    FailedLoginAttempts = u.FailedLoginAttempts,
                    LockedUntil = u.LockedUntil
                })
                .ToListAsync();

            // Set permission flags (this would be set based on requester's permissions)
            foreach (var customer in customers)
            {
                customer.CanViewDetails = true; // Set based on actual permissions
                customer.CanEdit = true; // Set based on actual permissions
                customer.CanDelete = true; // Set based on actual permissions
                customer.CanViewPersonalData = true; // Set based on actual permissions
            }

            return new PagedResult<CustomerManagementDto>
            {
                Items = customers,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers with parameters: {@Parameters}", parameters);
            throw;
        }
    }

    public async Task<CustomerDetailDto?> GetCustomerDetailAsync(int customerId, int requesterUserId)
    {
        try
        {
            // Check permissions
            var canViewDetails = await _authService.HasPermissionAsync(requesterUserId, CUSTOMERS_READ);
            if (!canViewDetails)
            {
                _logger.LogWarning("User {RequesterUserId} attempted to access customer {CustomerId} without permission",
                    requesterUserId, customerId);
                return null;
            }

            var customer = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.VipTier)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                .Include(u => u.Addresses)
                .Include(u => u.ActivityLogs.OrderByDescending(al => al.CreatedAt).Take(10))
                .FirstOrDefaultAsync(u => u.Id == customerId && !u.IsAdminRole);

            if (customer == null)
            {
                return null;
            }

            // Check permission levels
            var canViewPersonalData = await _authService.HasPermissionAsync(requesterUserId, CUSTOMERS_PERSONAL_DATA);
            var canViewSecurityInfo = await _authService.HasPermissionAsync(requesterUserId, CUSTOMERS_SECURITY_INFO);
            var canEdit = await _authService.HasPermissionAsync(requesterUserId, CUSTOMERS_WRITE);
            var canDelete = await _authService.HasPermissionAsync(requesterUserId, CUSTOMERS_DELETE);

            // Build detailed DTO with permission-based filtering
            var customerDetail = new CustomerDetailDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                DateOfBirth = null, // TODO: Add DateOfBirth to User entity if needed
                Gender = null, // TODO: Add Gender to User entity if needed
                ProfilePictureUrl = customer.ProfilePictureUrl,
                IsActive = customer.IsActive,
                EmailConfirmed = customer.EmailConfirmed,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                LastLoginAt = customer.LastLoginAt,
                LastLoginIP = canViewSecurityInfo ? customer.LastLoginIP : null,
                FailedLoginAttempts = canViewSecurityInfo ? customer.FailedLoginAttempts : 0,
                LockedUntil = canViewSecurityInfo ? customer.LockedUntil : null,
                LastPasswordChangeDate = canViewSecurityInfo ? customer.LastPasswordChangeDate : null,
                VipTierId = customer.VipTierId,
                VipTierName = customer.VipTier?.Name,
                TotalSpent = customer.TotalSpent,
                VipTierUpdatedAt = customer.VipTierUpdatedAt,
                TotalOrders = customer.Orders.Count,
                CompletedOrders = customer.Orders.Count(o => o.Status == OrderStatus.Delivered),
                CancelledOrders = customer.Orders.Count(o => o.Status == OrderStatus.Cancelled),
                FirstOrderDate = customer.Orders.OrderBy(o => o.CreatedAt).FirstOrDefault()?.CreatedAt,
                LastOrderDate = customer.Orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault()?.CreatedAt,
                AverageOrderValue = customer.Orders.Any() ? customer.Orders.Average(o => o.TotalAmount) : 0,
                Notes = canEdit ? customer.Notes : null,
                NotesUpdatedAt = null, // TODO: Add NotesUpdatedAt to User entity if needed
                CanEdit = canEdit,
                CanDelete = canDelete,
                CanViewOrderHistory = canViewDetails,
                CanViewPersonalData = canViewPersonalData,
                CanViewSecurityInfo = canViewSecurityInfo
            };

            // Add addresses with privacy filtering
            if (canViewPersonalData)
            {
                customerDetail.Addresses = customer.Addresses.Select(a => new CustomerAddressDto
                {
                    Id = a.Id,
                    Street = a.Street,
                    City = a.City,
                    District = a.District,
                    Ward = a.Ward,
                    ZipCode = null, // TODO: Add ZipCode to Address entity if needed
                    IsDefault = a.IsDefault,
                    CreatedAt = a.CreatedAt,
                    IsFullDataVisible = true
                }).ToList();
            }
            else
            {
                customerDetail.Addresses = customer.Addresses.Select(a => new CustomerAddressDto
                {
                    Id = a.Id,
                    MaskedStreet = MaskAddress(a.Street),
                    City = a.City,
                    District = a.District,
                    Ward = a.Ward,
                    IsDefault = a.IsDefault,
                    CreatedAt = a.CreatedAt,
                    IsFullDataVisible = false
                }).ToList();
            }

            // Add recent activities
            customerDetail.RecentActivities = customer.ActivityLogs.Select(al => new CustomerRecentActivityDto
            {
                ActivityDate = al.CreatedAt,
                ActivityType = al.Action,
                Description = al.Description,
                IpAddress = canViewSecurityInfo ? al.IPAddress : null,
                UserAgent = canViewSecurityInfo ? al.UserAgent : null
            }).ToList();

            // Log access for audit
            await _auditLoggingService.LogEventAsync(
                "Customer", customerId.ToString(), "view_details", "customer_management",
                null, null, requesterUserId.ToString(), null, null, null, null);

            return customerDetail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer detail for customer {CustomerId} by user {RequesterUserId}",
                customerId, requesterUserId);
            throw;
        }
    }

    public async Task<CustomerDetailDto?> UpdateCustomerAsync(int customerId, UpdateCustomerRequest updateRequest, int updatedByUserId)
    {
        try
        {
            // Check permissions
            var canEdit = await _authService.HasPermissionAsync(updatedByUserId, CUSTOMERS_WRITE);
            if (!canEdit)
            {
                _logger.LogWarning("User {UpdatedByUserId} attempted to update customer {CustomerId} without permission",
                    updatedByUserId, customerId);
                return null;
            }

            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == customerId && !u.IsAdminRole);

            if (customer == null)
            {
                return null;
            }

            // Store original values for audit
            var originalValues = new
            {
                customer.FirstName,
                customer.LastName,
                customer.PhoneNumber,
                // customer.DateOfBirth, // TODO: Add if needed
                // customer.Gender, // TODO: Add if needed
                customer.IsActive,
                customer.EmailConfirmed,
                customer.LockedUntil,
                customer.VipTierId,
                customer.Notes
            };

            // Update fields
            if (!string.IsNullOrEmpty(updateRequest.FirstName))
                customer.FirstName = updateRequest.FirstName;

            if (!string.IsNullOrEmpty(updateRequest.LastName))
                customer.LastName = updateRequest.LastName;

            if (!string.IsNullOrEmpty(updateRequest.PhoneNumber))
                customer.PhoneNumber = updateRequest.PhoneNumber;

            // TODO: Add DateOfBirth and Gender support when added to User entity
            // if (updateRequest.DateOfBirth.HasValue)
            // {
            //     var canViewPersonalData = await _authService.HasPermissionAsync(updatedByUserId, CUSTOMERS_PERSONAL_DATA);
            //     if (canViewPersonalData)
            //         customer.DateOfBirth = updateRequest.DateOfBirth;
            // }

            // if (!string.IsNullOrEmpty(updateRequest.Gender))
            //     customer.Gender = updateRequest.Gender;

            if (updateRequest.IsActive.HasValue)
                customer.IsActive = updateRequest.IsActive.Value;

            if (!string.IsNullOrEmpty(updateRequest.Notes))
                customer.Notes = updateRequest.Notes;

            // Admin-only fields
            var canManage = await _authService.HasPermissionAsync(updatedByUserId, CUSTOMERS_MANAGE);
            if (canManage)
            {
                if (updateRequest.EmailConfirmed.HasValue)
                    customer.EmailConfirmed = updateRequest.EmailConfirmed.Value;

                if (updateRequest.LockedUntil.HasValue)
                    customer.LockedUntil = updateRequest.LockedUntil.Value;

                if (updateRequest.VipTierId.HasValue)
                {
                    customer.VipTierId = updateRequest.VipTierId.Value;
                    customer.VipTierUpdatedAt = DateTime.UtcNow;
                }
            }

            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log update for audit
            await _auditLoggingService.LogEventAsync(
                "Customer", customerId.ToString(), "update", "customer_management",
                originalValues, updateRequest, updatedByUserId.ToString(), null, null, null, null);

            _logger.LogInformation("Customer {CustomerId} updated by user {UpdatedByUserId}",
                customerId, updatedByUserId);

            // Return updated customer details
            return await GetCustomerDetailAsync(customerId, updatedByUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId} by user {UpdatedByUserId}",
                customerId, updatedByUserId);
            throw;
        }
    }

    public async Task<bool> DeactivateCustomerAsync(int customerId, int deactivatedByUserId, string reason)
    {
        try
        {
            var canDelete = await _authService.HasPermissionAsync(deactivatedByUserId, CUSTOMERS_DELETE);
            if (!canDelete)
            {
                _logger.LogWarning("User {DeactivatedByUserId} attempted to deactivate customer {CustomerId} without permission",
                    deactivatedByUserId, customerId);
                return false;
            }

            var customer = await _context.Users.FindAsync(customerId);
            if (customer == null || !customer.IsActive)
            {
                return false;
            }

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.Notes = $"{customer.Notes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Deactivated: {reason}";

            await _context.SaveChangesAsync();

            // Log deactivation
            await _auditLoggingService.LogEventAsync(
                "Customer", customerId.ToString(), "deactivate", "customer_management",
                new { IsActive = true }, new { IsActive = false, Reason = reason },
                deactivatedByUserId.ToString(), null, null, null, null);

            _logger.LogInformation("Customer {CustomerId} deactivated by user {DeactivatedByUserId}. Reason: {Reason}",
                customerId, deactivatedByUserId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating customer {CustomerId} by user {DeactivatedByUserId}",
                customerId, deactivatedByUserId);
            throw;
        }
    }

    public async Task<bool> ReactivateCustomerAsync(int customerId, int reactivatedByUserId, string reason)
    {
        try
        {
            var canManage = await _authService.HasPermissionAsync(reactivatedByUserId, CUSTOMERS_MANAGE);
            if (!canManage)
            {
                _logger.LogWarning("User {ReactivatedByUserId} attempted to reactivate customer {CustomerId} without permission",
                    reactivatedByUserId, customerId);
                return false;
            }

            var customer = await _context.Users.FindAsync(customerId);
            if (customer == null || customer.IsActive)
            {
                return false;
            }

            customer.IsActive = true;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.Notes = $"{customer.Notes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Reactivated: {reason}";

            await _context.SaveChangesAsync();

            // Log reactivation
            await _auditLoggingService.LogEventAsync(
                "Customer", customerId.ToString(), "reactivate", "customer_management",
                new { IsActive = false }, new { IsActive = true, Reason = reason },
                reactivatedByUserId.ToString(), null, null, null, null);

            _logger.LogInformation("Customer {CustomerId} reactivated by user {ReactivatedByUserId}. Reason: {Reason}",
                customerId, reactivatedByUserId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating customer {CustomerId} by user {ReactivatedByUserId}",
                customerId, reactivatedByUserId);
            throw;
        }
    }

    public async Task<CustomerOrderHistoryDto> GetCustomerOrderHistoryAsync(int customerId, int requesterUserId)
    {
        try
        {
            var canViewDetails = await _authService.HasPermissionAsync(requesterUserId, CUSTOMERS_READ);
            if (!canViewDetails)
            {
                throw new UnauthorizedAccessException("Insufficient permissions to view customer order history");
            }

            var customer = await _context.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(u => u.Id == customerId);

            if (customer == null)
            {
                throw new ArgumentException("Customer not found", nameof(customerId));
            }

            var orders = customer.Orders.ToList();

            var orderHistory = new CustomerOrderHistoryDto
            {
                CustomerId = customerId,
                TotalOrders = orders.Count,
                TotalSpent = orders.Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                FirstOrderDate = orders.OrderBy(o => o.CreatedAt).FirstOrDefault()?.CreatedAt,
                LastOrderDate = orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault()?.CreatedAt,
                PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
                CompletedOrders = orders.Count(o => o.Status == OrderStatus.Delivered),
                CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
                RefundedOrders = orders.Count(o => o.Status == OrderStatus.Refunded),
                RecentOrders = orders.OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new CustomerOrderSummaryDto
                    {
                        OrderId = o.Id,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.CreatedAt,
                        Total = o.TotalAmount,
                        Status = o.Status.ToString(),
                        ItemCount = o.OrderItems.Count
                    }).ToList(),
                MonthlySpending = orders
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new MonthlySpendingDto
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Amount = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count()
                    })
                    .OrderByDescending(m => m.Year)
                    .ThenByDescending(m => m.Month)
                    .Take(12)
                    .ToList()
            };

            return orderHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order history for customer {CustomerId} by user {RequesterUserId}",
                customerId, requesterUserId);
            throw;
        }
    }

    public async Task<CustomerStatisticsDto> GetCustomerStatisticsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            var thirtyDaysAgo = now.AddDays(-30);

            var stats = new CustomerStatisticsDto
            {
                TotalCustomers = await _context.Users
                    .CountAsync(u => !u.IsAdminRole),

                ActiveCustomers = await _context.Users
                    .CountAsync(u => !u.IsAdminRole && u.IsActive),

                NewCustomersThisMonth = await _context.Users
                    .CountAsync(u => !u.IsAdminRole && u.CreatedAt >= thisMonth),

                NewCustomersToday = await _context.Users
                    .CountAsync(u => !u.IsAdminRole && u.CreatedAt >= today),

                EmailVerifiedCustomers = await _context.Users
                    .CountAsync(u => !u.IsAdminRole && u.EmailConfirmed),

                UnverifiedCustomers = await _context.Users
                    .CountAsync(u => !u.IsAdminRole && !u.EmailConfirmed),

                CustomersLoggedInToday = await _context.Users
                    .CountAsync(u => !u.IsAdminRole &&
                        u.LastLoginAt.HasValue && u.LastLoginAt.Value >= today),

                CustomersLoggedInThisWeek = await _context.Users
                    .CountAsync(u => !u.IsAdminRole &&
                        u.LastLoginAt.HasValue && u.LastLoginAt.Value >= now.AddDays(-7)),

                InactiveCustomers30Days = await _context.Users
                    .CountAsync(u => !u.IsAdminRole &&
                        (!u.LastLoginAt.HasValue || u.LastLoginAt.Value < thirtyDaysAgo))
            };

            // Calculate average customer value
            var totalSpent = await _context.Users
                .Where(u => !u.IsAdminRole)
                .SumAsync(u => u.TotalSpent);
            stats.AverageCustomerValue = stats.TotalCustomers > 0 ? totalSpent / stats.TotalCustomers : 0;

            // Get VIP tier statistics
            stats.VipTierStats = await _context.UserVipTiers
                .Include(vt => vt.Users)
                .Select(vt => new VipTierStatDto
                {
                    TierId = vt.Id,
                    TierName = vt.Name,
                    CustomerCount = vt.Users.Count(u => !u.IsAdminRole),
                    TotalSpent = vt.Users.Where(u => !u.IsAdminRole).Sum(u => u.TotalSpent),
                    AverageSpent = vt.Users.Any(u => !u.IsAdminRole)
                        ? vt.Users.Where(u => !u.IsAdminRole).Average(u => u.TotalSpent)
                        : 0
                })
                .ToListAsync();

            // Get registration trends (last 30 days)
            stats.RegistrationTrends = await _context.Users
                .Where(u => !u.IsAdminRole && u.CreatedAt >= thirtyDaysAgo)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new CustomerRegistrationTrendDto
                {
                    Date = g.Key,
                    NewRegistrations = g.Count(),
                    EmailVerifications = g.Count(u => u.EmailConfirmed)
                })
                .OrderBy(t => t.Date)
                .ToListAsync();

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer statistics");
            throw;
        }
    }

    public async Task<ExportResult> ExportCustomersAsync(CustomerExportParameters parameters, int requesterUserId, ExportFormat format)
    {
        try
        {
            var canExport = await _authService.HasPermissionAsync(requesterUserId, CUSTOMERS_EXPORT);
            if (!canExport)
            {
                throw new UnauthorizedAccessException("Insufficient permissions to export customer data");
            }

            // Implementation for export would go here
            // This is a placeholder - actual implementation would generate CSV/Excel/PDF
            var data = new byte[0];
            var fileName = $"customers_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            var contentType = format switch
            {
                ExportFormat.CSV => "text/csv",
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.PDF => "application/pdf",
                _ => "application/octet-stream"
            };

            fileName += format switch
            {
                ExportFormat.CSV => ".csv",
                ExportFormat.Excel => ".xlsx",
                ExportFormat.PDF => ".pdf",
                _ => ".bin"
            };

            // Log export activity
            await _auditLoggingService.LogEventAsync(
                "Customer", "bulk", "export", "customer_management",
                null, new { Format = format, Parameters = parameters },
                requesterUserId.ToString(), null, null, null, null);

            return new ExportResult
            {
                Data = data,
                FileName = fileName,
                ContentType = contentType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting customers by user {RequesterUserId}", requesterUserId);
            throw;
        }
    }

    public async Task<bool> SendCustomerNotificationAsync(int customerId, CustomerNotificationRequest notification, int sentByUserId)
    {
        try
        {
            var canManage = await _authService.HasPermissionAsync(sentByUserId, CUSTOMERS_MANAGE);
            if (!canManage)
            {
                _logger.LogWarning("User {SentByUserId} attempted to send notification to customer {CustomerId} without permission",
                    sentByUserId, customerId);
                return false;
            }

            var customer = await _context.Users.FindAsync(customerId);
            if (customer == null || !customer.IsActive)
            {
                return false;
            }

            if (notification.SendEmail)
            {
                await _emailService.SendCustomEmailAsync(
                    customer.Email,
                    $"{customer.FirstName} {customer.LastName}",
                    notification.Subject,
                    notification.Message);
            }

            // Log notification
            await _auditLoggingService.LogEventAsync(
                "Customer", customerId.ToString(), "send_notification", "customer_management",
                null, notification, sentByUserId.ToString(), null, null, null, null);

            _logger.LogInformation("Notification sent to customer {CustomerId} by user {SentByUserId}",
                customerId, sentByUserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to customer {CustomerId} by user {SentByUserId}",
                customerId, sentByUserId);
            throw;
        }
    }

    public async Task<PagedResult<CustomerActivityLogDto>> GetCustomerActivityLogsAsync(int customerId, int requesterUserId, ActivityLogParameters parameters)
    {
        try
        {
            var canViewLogs = await _authService.HasPermissionAsync(requesterUserId, CUSTOMERS_MANAGE);
            if (!canViewLogs)
            {
                throw new UnauthorizedAccessException("Insufficient permissions to view customer activity logs");
            }

            var query = _context.UserActivityLogs
                .Where(al => al.UserId == customerId)
                .AsQueryable();

            // Apply filters
            if (parameters.FromDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt >= parameters.FromDate.Value);
            }

            if (parameters.ToDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt <= parameters.ToDate.Value);
            }

            if (!string.IsNullOrEmpty(parameters.ActivityType))
            {
                query = query.Where(al => al.Action.Contains(parameters.ActivityType));
            }

            // Apply sorting
            query = parameters.SortOrder?.ToLower() == "asc"
                ? query.OrderBy(al => al.CreatedAt)
                : query.OrderByDescending(al => al.CreatedAt);

            var totalCount = await query.CountAsync();

            var logs = await query
                .Skip((parameters.Page - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(al => new CustomerActivityLogDto
                {
                    Id = al.Id,
                    CustomerId = al.UserId,
                    Action = al.Action,
                    Description = al.Description,
                    CreatedAt = al.CreatedAt,
                    IpAddress = al.IPAddress,
                    UserAgent = al.UserAgent,
                    EntityType = al.EntityType,
                    EntityId = al.EntityId
                })
                .ToListAsync();

            return new PagedResult<CustomerActivityLogDto>
            {
                Items = logs,
                TotalCount = totalCount,
                Page = parameters.Page,
                PageSize = parameters.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activity logs for customer {CustomerId} by user {RequesterUserId}",
                customerId, requesterUserId);
            throw;
        }
    }

    // Helper methods for data masking
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.com";

        var parts = email.Split('@');
        var username = parts[0];
        var domain = parts[1];

        var maskedUsername = username.Length > 2
            ? username.Substring(0, 2) + new string('*', Math.Max(1, username.Length - 2))
            : new string('*', username.Length);

        var domainParts = domain.Split('.');
        var maskedDomain = domainParts.Length > 1
            ? new string('*', domainParts[0].Length) + "." + domainParts[^1]
            : new string('*', domain.Length);

        return $"{maskedUsername}@{maskedDomain}";
    }

    private static string? MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return null;

        if (phoneNumber.Length <= 4)
            return new string('*', phoneNumber.Length);

        return phoneNumber.Substring(0, 3) + new string('*', phoneNumber.Length - 6) + phoneNumber.Substring(phoneNumber.Length - 3);
    }

    private static string MaskAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return "***";

        var words = address.Split(' ');
        if (words.Length <= 2)
            return new string('*', address.Length);

        return words[0] + " " + new string('*', address.Length - words[0].Length - words[^1].Length - 1) + words[^1];
    }
}