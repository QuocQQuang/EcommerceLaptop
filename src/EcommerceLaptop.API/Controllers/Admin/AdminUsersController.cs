using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Interfaces.Services;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _userService;
    private readonly IMapper _mapper;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(IAdminUserService userService, IMapper mapper, ILogger<AdminUsersController> logger)
    {
        _userService = userService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all admin users from unified Users table
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequirePermission:users:read")]
    public async Task<ActionResult<UsersResponseDto>> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
    {
        var result = await _userService.GetAdminUsersAsync(page, limit, search);
        var totalPages = (int)Math.Ceiling(result.TotalCount / (double)limit);

        var userDtos = _mapper.Map<List<AdminUserManagementDto>>(result.Items);

        return Ok(new UsersResponseDto
        {
            Users = userDtos,
            TotalCount = result.TotalCount,
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
        try
        {
            // Email uniqueness check (can be moved to Service, but checking here for specific 400 response msg structure if needed)
            if (await _userService.GetByEmailAsync(request.Email) != null)
            {
                return BadRequest(new { message = "Email already exists" });
            }

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };

            var createdUser = await _userService.CreateAdminUserAsync(user, request.Password, request.RoleId, adminId);

            var userDto = _mapper.Map<AdminUserManagementDto>(createdUser);

            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, userDto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
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
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "Admin user not found" });
        }

        // Verify it's an admin user
        var adminRole = user.UserRoles.FirstOrDefault(ur => ur.Role.IsAdminRole);
        if (adminRole == null)
        {
             return NotFound(new { message = "Admin user not found" });
        }

        var userDto = _mapper.Map<AdminUserManagementDto>(user);

        return Ok(userDto);
    }

    /// <summary>
    /// Update admin user in unified Users table
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePermission:users:write")]
    public async Task<ActionResult<AdminUserManagementDto>> UpdateUser(int id, [FromBody] UpdateUserRequestDto request)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null || !user.UserRoles.Any(ur => ur.Role.IsAdminRole))
            {
                return NotFound(new { message = "Admin user not found" });
            }

            // Email uniqueness check
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                var existingUser = await _userService.GetByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != id)
                {
                    return BadRequest(new { message = "Email already exists" });
                }
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.FirstName)) user.FirstName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName)) user.LastName = request.LastName;

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            
            var updatedUser = await _userService.UpdateAdminUserAsync(user, request.Password, request.RoleId, adminId);

            var userDto = _mapper.Map<AdminUserManagementDto>(updatedUser);

            return Ok(userDto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
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
        var user = await _userService.GetByIdAsync(id);
        if (user == null || !user.UserRoles.Any(ur => ur.Role.IsAdminRole))
        {
            return NotFound(new { message = "Admin user not found" });
        }

        // Prevent deleting current user
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString())
        {
            return BadRequest(new { message = "Cannot delete your own account" });
        }

        await _userService.DeleteUserAsync(id, currentUserId ?? "unknown");

        return NoContent();
    }

    /// <summary>
    /// Toggle admin user active status
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [Authorize(Policy = "RequirePermission:users:write")]
    public async Task<ActionResult<AdminUserManagementDto>> ToggleUserStatus(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null || !user.UserRoles.Any(ur => ur.Role.IsAdminRole))
        {
            return NotFound(new { message = "Admin user not found" });
        }

        // Prevent deactivating current user
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString() && user.IsActive)
        {
            return BadRequest(new { message = "Cannot deactivate your own account" });
        }

        var updatedUser = await _userService.ToggleUserStatusAsync(id, currentUserId ?? "unknown");

        var userDto = _mapper.Map<AdminUserManagementDto>(updatedUser);

        return Ok(userDto);
    }
}