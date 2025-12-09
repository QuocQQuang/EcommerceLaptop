using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Controller for managing user VIP tier assignments
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UserVipTierController : BaseApiController
{
    private readonly IUserVipTierService _userVipTierService;

    public UserVipTierController(IUserVipTierService userVipTierService, ILogger<UserVipTierController> logger)
        : base(logger)
    {
        _userVipTierService = userVipTierService;
    }

    /// <summary>
    /// Get all user VIP tier assignments with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserVipTiers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? tierId = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var result = await _userVipTierService.GetUserVipTiersAsync(
                pageNumber, pageSize, searchTerm, tierId, isActive);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user VIP tiers");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user VIP tier assignment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserVipTier(int id)
    {
        try
        {
            var result = await _userVipTierService.GetUserVipTierByIdAsync(id);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            if (result.Data == null)
                return NotFound(new { error = "User VIP tier assignment not found" });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user VIP tier with ID {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get user's current VIP tier by user ID
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserCurrentTier(string userId)
    {
        try
        {
            var result = await _userVipTierService.GetUserCurrentTierAsync(userId);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current VIP tier for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Assign VIP tier to user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AssignVipTier([FromBody] AssignUserVipTierRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userVipTierService.AssignVipTierAsync(
                request.UserId, 
                request.VipTierId, 
                request.Reason);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning VIP tier");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update user VIP tier assignment
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUserVipTier(int id, [FromBody] UpdateUserVipTierRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userVipTierService.UpdateVipTierAssignmentAsync(
                id.ToString(), 
                request.VipTierId, 
                request.Reason);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user VIP tier assignment with ID {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Revoke user VIP tier assignment
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeVipTier(int id, [FromBody] RevokeVipTierRequest request)
    {
        try
        {
            var result = await _userVipTierService.RevokeVipTierAsync(id, request.Reason);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { message = "VIP tier revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking VIP tier assignment with ID {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get VIP tier statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetVipTierStatistics()
    {
        try
        {
            var result = await _userVipTierService.GetVipTierStatisticsAsync();

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving VIP tier statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Bulk update VIP tier assignments
    /// </summary>
    [HttpPost("bulk-update")]
    public async Task<IActionResult> BulkUpdateVipTiers([FromBody] BulkUpdateVipTiersRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userVipTierService.BulkUpdateVipTiersAsync(
                request.UserIds, 
                request.VipTierId, 
                request.ExpiresAt, 
                request.Reason);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk VIP tier update");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}