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
public class UserVipTierController(IUserVipTierService userVipTierService, ILogger<UserVipTierController> logger)
    : BaseApiController(logger)
{
    private readonly IUserVipTierService _userVipTierService = userVipTierService;

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
        var result = await _userVipTierService.GetUserVipTiersAsync(
            pageNumber, pageSize, searchTerm, tierId, isActive);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get user VIP tier assignment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserVipTier(int id)
    {
        var result = await _userVipTierService.GetUserVipTierByIdAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return NotFound(new { error = "User VIP tier assignment not found" });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get user's current VIP tier by user ID
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserCurrentTier(string userId)
    {
        var result = await _userVipTierService.GetUserCurrentTierAsync(userId);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Assign VIP tier to user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AssignVipTier([FromBody] AssignUserVipTierRequest request)
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

    /// <summary>
    /// Update user VIP tier assignment
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUserVipTier(int id, [FromBody] UpdateUserVipTierRequest request)
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

    /// <summary>
    /// Revoke user VIP tier assignment
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeVipTier(int id, [FromBody] RevokeVipTierRequest request)
    {
        var result = await _userVipTierService.RevokeVipTierAsync(id, request.Reason);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { message = "VIP tier revoked successfully" });
    }

    /// <summary>
    /// Get VIP tier statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetVipTierStatistics()
    {
        var result = await _userVipTierService.GetVipTierStatisticsAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Bulk update VIP tier assignments
    /// </summary>
    [HttpPost("bulk-update")]
    public async Task<IActionResult> BulkUpdateVipTiers([FromBody] BulkUpdateVipTiersRequest request)
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
}