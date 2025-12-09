using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.DTOs.Admin;
using System.Security.Claims;
using BCrypt.Net;

namespace EcommerceLaptop.API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize]
public class AdminUsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(ApplicationDbContext context, ILogger<AdminUsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all admin users from unified Users table
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequirePermission:users:read")]
    public async Task<ActionResult<UsersResponseDto>> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
    {
        // Query unified Users table, filter for admin users only
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.UserRoles.Any(ur => ur.Role.IsAdminRole)) // Only admin users
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.FirstName.Contains(search) ||
                                   u.LastName.Contains(search) ||
                                   u.Email.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)limit);

        var users = await query
            .OrderBy(u => u.FirstName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(u => new AdminUserManagementDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                IsActive = u.IsActive,
                // Get the first admin role (user might have multiple admin roles)
                RoleId = u.UserRoles.First(ur => ur.Role.IsAdminRole).RoleId,
                RoleName = u.UserRoles.First(ur => ur.Role.IsAdminRole).Role.Name,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return Ok(new UsersResponseDto
        {
            Users = users,
            TotalCount = totalCount,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = limit
        });
    }

    /// <summary>
    /// Create new admin user in unified Users table
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequirePermission:users:write")]
    public async Task<ActionResult<AdminUserManagementDto>> CreateUser([FromBody] CreateUserRequestDto request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validate email uniqueness in unified Users table
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Validate role is admin role
            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null || !role.IsAdminRole)
            {
                return BadRequest(new { message = "Invalid admin role ID" });
            }

            // Create user in unified Users table
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign admin role via UserRoles table
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = request.RoleId
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Return response
            var userDto = new AdminUserManagementDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                IsActive = user.IsActive,
                RoleId = request.RoleId,
                RoleName = role.Name,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            _logger.LogInformation("Admin user created in unified system: {Email} by {AdminId}",
                user.Email, User.FindFirstValue(ClaimTypes.NameIdentifier));

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating admin user: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    /// <summary>
    /// Get admin user by ID from unified Users table
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "RequirePermission:users:read")]
    public async Task<ActionResult<AdminUserManagementDto>> GetUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.Id == id && u.UserRoles.Any(ur => ur.Role.IsAdminRole))
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new { message = "Admin user not found" });
        }

        var adminRole = user.UserRoles.First(ur => ur.Role.IsAdminRole);

        var userDto = new AdminUserManagementDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActive = user.IsActive,
            RoleId = adminRole.RoleId,
            RoleName = adminRole.Role.Name,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Ok(userDto);
    }

    /// <summary>
    /// Update admin user in unified Users table
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePermission:users:write")]
    public async Task<ActionResult<AdminUserManagementDto>> UpdateUser(int id, [FromBody] UpdateUserRequestDto request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.Id == id && u.UserRoles.Any(ur => ur.Role.IsAdminRole))
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "Admin user not found" });
            }

            // Check email uniqueness if changing email
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                {
                    return BadRequest(new { message = "Email already exists" });
                }
                user.Email = request.Email;
            }

            // Update basic info
            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;

            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;

            // Update password if provided
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

            // Update role if provided
            if (request.RoleId.HasValue)
            {
                var newRole = await _context.Roles.FindAsync(request.RoleId.Value);
                if (newRole == null || !newRole.IsAdminRole)
                {
                    return BadRequest(new { message = "Invalid admin role ID" });
                }

                // Remove old admin roles and add new one
                var oldAdminRoles = user.UserRoles.Where(ur => ur.Role.IsAdminRole).ToList();
                foreach (var oldRole in oldAdminRoles)
                {
                    _context.UserRoles.Remove(oldRole);
                }

                var newUserRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = request.RoleId.Value
                };
                _context.UserRoles.Add(newUserRole);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Reload for response
            await _context.Entry(user)
                .Collection(u => u.UserRoles)
                .Query()
                .Include(ur => ur.Role)
                .LoadAsync();

            var adminRole = user.UserRoles.First(ur => ur.Role.IsAdminRole);

            var userDto = new AdminUserManagementDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                IsActive = user.IsActive,
                RoleId = adminRole.RoleId,
                RoleName = adminRole.Role.Name,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            _logger.LogInformation("Admin user updated: {Email} by {AdminId}",
                user.Email, User.FindFirstValue(ClaimTypes.NameIdentifier));

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating admin user: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the user" });
        }
    }

    /// <summary>
    /// Delete admin user from unified Users table
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequirePermission:users:delete")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.Id == id && u.UserRoles.Any(ur => ur.Role.IsAdminRole))
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new { message = "Admin user not found" });
        }

        // Prevent deleting current user
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString())
        {
            return BadRequest(new { message = "Cannot delete your own account" });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin user deleted: {Email} by {AdminId}",
            user.Email, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Toggle admin user active status
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [Authorize(Policy = "RequirePermission:users:write")]
    public async Task<ActionResult<AdminUserManagementDto>> ToggleUserStatus(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Where(u => u.Id == id && u.UserRoles.Any(ur => ur.Role.IsAdminRole))
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new { message = "Admin user not found" });
        }

        // Prevent deactivating current user
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString() && user.IsActive)
        {
            return BadRequest(new { message = "Cannot deactivate your own account" });
        }

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var adminRole = user.UserRoles.First(ur => ur.Role.IsAdminRole);

        var userDto = new AdminUserManagementDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActive = user.IsActive,
            RoleId = adminRole.RoleId,
            RoleName = adminRole.Role.Name,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        _logger.LogInformation("Admin user status toggled: {Email} -> {IsActive} by {AdminId}",
            user.Email, user.IsActive, currentUserId);

        return Ok(userDto);
    }
}