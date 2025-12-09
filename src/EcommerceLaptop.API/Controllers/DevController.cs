using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DevController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("seed-permissions")]
    public async Task<IActionResult> SeedPermissions()
    {
        try
        {
            await PermissionSeeder.SeedPermissionsAsync(_context);
            return Ok(new { message = "Permissions seeded successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.ToString() });
        }
    }

    [HttpGet("debug-admin")]
    public async Task<IActionResult> DebugAdmin()
    {
        try
        {
            // Legacy AdminUsers system removed - use unified Users system instead
            var adminUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(u => u.UserRoles.Any(ur => ur.Role.IsAdminRole == true))
                .ToListAsync();

            var result = adminUsers.Select(u => new
            {
                AdminUser = new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    AdminRoles = u.UserRoles.Where(ur => ur.Role.IsAdminRole == true).Select(ur => ur.Role.Name).ToList()
                },
                Roles = u.UserRoles.Where(ur => ur.Role.IsAdminRole == true).Select(ur => new
                {
                    ur.Role.Id,
                    ur.Role.Name,
                    ur.Role.Description,
                    ur.Role.IsAdminRole,
                    PermissionCount = ur.Role.RolePermissions?.Count ?? 0,
                    Permissions = ur.Role.RolePermissions?.Select(rp => new
                    {
                        rp.Permission.Id,
                        rp.Permission.Name,
                        rp.Permission.Module,
                        rp.Permission.Action
                    }).ToList()
                }).ToList()
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.ToString() });
        }
    }
}