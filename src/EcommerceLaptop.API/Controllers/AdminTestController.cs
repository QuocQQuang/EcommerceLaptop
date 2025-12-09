using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.API.Authorization;
using EcommerceLaptop.Core.Constants;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Test controller for admin authorization
/// </summary>
[ApiController]
[Route("api/admin/[controller]")]
[RequireAdmin]
public class AdminTestController : ControllerBase
{
    private readonly ILogger<AdminTestController> _logger;

    public AdminTestController(ILogger<AdminTestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test basic admin access (any admin can access)
    /// </summary>
    [HttpGet("basic")]
    public IActionResult GetBasicInfo()
    {
        _logger.LogInformation("Admin basic access test - User: {User}", User.Identity?.Name);
        
        return Ok(new
        {
            message = "Admin access granted",
            timestamp = DateTime.UtcNow,
            adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            adminEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        });
    }

    /// <summary>
    /// Test dashboard read permission
    /// </summary>
    [HttpGet("dashboard")]
    [RequireAdminPermission(AdminPermissions.DashboardRead)]
    public IActionResult GetDashboard()
    {
        _logger.LogInformation("Dashboard access test - User: {User}", User.Identity?.Name);
        
        return Ok(new
        {
            message = "Dashboard access granted",
            permission = AdminPermissions.DashboardRead,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test users read permission
    /// </summary>
    [HttpGet("users")]
    [RequireAdminPermission(AdminPermissions.UsersRead)]
    public IActionResult GetUsers()
    {
        _logger.LogInformation("Users read access test - User: {User}", User.Identity?.Name);
        
        return Ok(new
        {
            message = "Users read access granted",
            permission = AdminPermissions.UsersRead,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test users write permission
    /// </summary>
    [HttpPost("users")]
    [RequireAdminPermission(AdminPermissions.UsersWrite)]
    public IActionResult CreateUser()
    {
        _logger.LogInformation("Users write access test - User: {User}", User.Identity?.Name);
        
        return Ok(new
        {
            message = "Users write access granted",
            permission = AdminPermissions.UsersWrite,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test users delete permission
    /// </summary>
    [HttpDelete("users/{id}")]
    [RequireAdminPermission(AdminPermissions.UsersDelete)]
    public IActionResult DeleteUser(int id)
    {
        _logger.LogInformation("Users delete access test - User: {User}, UserId: {UserId}", User.Identity?.Name, id);
        
        return Ok(new
        {
            message = "Users delete access granted",
            permission = AdminPermissions.UsersDelete,
            userId = id,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test high-level users management permission
    /// </summary>
    [HttpPut("users/manage")]
    [RequireAdminPermission(AdminPermissions.UsersManage)]
    public IActionResult ManageUsers()
    {
        _logger.LogInformation("Users manage access test - User: {User}", User.Identity?.Name);
        
        return Ok(new
        {
            message = "Users manage access granted",
            permission = AdminPermissions.UsersManage,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test products management permission
    /// </summary>
    [HttpGet("products")]
    [RequireAdminPermission(AdminPermissions.ProductsRead)]
    public IActionResult GetProducts()
    {
        _logger.LogInformation("Products read access test - User: {User}", User.Identity?.Name);
        
        return Ok(new
        {
            message = "Products read access granted",
            permission = AdminPermissions.ProductsRead,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test high security permission
    /// </summary>
    [HttpGet("security")]
    [RequireAdminPermission(AdminPermissions.SecurityManage)]
    public IActionResult GetSecuritySettings()
    {
        _logger.LogInformation("Security manage access test - User: {User}", User.Identity?.Name);
        
        return Ok(new
        {
            message = "Security management access granted",
            permission = AdminPermissions.SecurityManage,
            timestamp = DateTime.UtcNow
        });
    }
}