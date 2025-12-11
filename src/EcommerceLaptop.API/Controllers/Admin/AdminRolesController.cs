using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Interfaces.Services;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers.Admin;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = "AdminPolicy")]
public class AdminRolesController : ControllerBase
{
    private readonly IAdminRoleService _roleService;
    private readonly ILogger<AdminRolesController> _logger;

    public AdminRolesController(IAdminRoleService roleService, ILogger<AdminRolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Get all roles with permissions
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequirePermission:roles:read")]
    public async Task<ActionResult<List<RoleWithPermissionsDto>>> GetRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(roles);
    }

    /// <summary>
    /// Get role by ID with permissions
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "RequirePermission:roles:read")]
    public async Task<ActionResult<RoleWithPermissionsDto>> GetRole(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
        {
            return NotFound(new { message = "Role not found" });
        }
        return Ok(role);
    }

    /// <summary>
    /// Create new role
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequirePermission:roles:write")]
    public async Task<ActionResult<RoleWithPermissionsDto>> CreateRole([FromBody] CreateRoleRequestDto request)
    {
        try
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var role = await _roleService.CreateRoleAsync(request, adminId);
            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update role
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePermission:roles:write")]
    public async Task<ActionResult<RoleWithPermissionsDto>> UpdateRole(int id, [FromBody] UpdateRoleRequestDto request)
    {
        try
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var role = await _roleService.UpdateRoleAsync(id, request, adminId);
            return Ok(role);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Role not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequirePermission:roles:delete")]
    public async Task<ActionResult> DeleteRole(int id)
    {
        try
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            await _roleService.DeleteRoleAsync(id, adminId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Role not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Assign permissions to role
    /// </summary>
    [HttpPatch("{roleId}/permissions")]
    [Authorize(Policy = "RequirePermission:roles:write")]
    public async Task<ActionResult<RoleWithPermissionsDto>> AssignRolePermissions(int roleId, [FromBody] AssignPermissionsRequestDto request)
    {
        try
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var role = await _roleService.AssignPermissionsAsync(roleId, request.PermissionIds, adminId);
            return Ok(role);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Role not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Assign permissions request DTO
/// </summary>
public class AssignPermissionsRequestDto
{
    public List<int> PermissionIds { get; set; } = new();
}