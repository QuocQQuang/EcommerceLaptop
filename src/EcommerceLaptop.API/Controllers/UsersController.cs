using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Services;
using EcommerceLaptop.Infrastructure.Services.Security;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.API.DTOs;
using BCrypt.Net;
using Microsoft.AspNetCore.RateLimiting;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Users controller handling user management operations
/// Implements role-based access control and user profile management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(
    IUserService userService,
    IImageHostingService imageHostingService,
    IAuditLoggingService auditLoggingService,
    ILogger<UsersController> logger)
    : BaseApiController(logger)
{
    private readonly IUserService _userService = userService;
    private readonly IImageHostingService _imageHostingService = imageHostingService;
    private readonly IAuditLoggingService _auditLoggingService = auditLoggingService;

    /// <summary>
    /// Gets current user profile
    /// </summary>
    /// <returns>User profile</returns>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return ErrorResponse("User not found", 401);

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
            return ErrorResponse("User not found", 404);

        var roles = await _userService.GetUserRolesAsync(userId.Value);
        var userDto = MapToUserProfileDto(user, roles);

        return SuccessResponse(userDto);
    }

    /// <summary>
    /// Changes user password with enhanced security measures
    /// Rate limited to 5 attempts per 15 minutes per user
    /// </summary>
    /// <param name="request">Password change request</param>
    /// <returns>Success status</returns>
    [HttpPost("change-password")]
    [EnableRateLimiting("PasswordChangePolicy")]
    public async Task<IActionResult> ChangePassword([FromBody] DTOs.ChangePasswordRequest changePasswordRequest)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return ErrorResponse("User not found", 401);

        // Get user with security context
        var user = await _userService.GetByIdAsync(userId.Value);

        if (user == null)
            return ErrorResponse("User not found", 404);

        // Verify current password
        if (!await _userService.VerifyPasswordAsync(user, changePasswordRequest.CurrentPassword))
        {
            // Log failed password change attempt
            _logger.LogWarning("Failed password change attempt for user {UserId} from IP {IP}",
                userId.Value, GetClientIpAddress());

            // Record security event using audit logging service
            var failedIpAddress = GetClientIpAddress();
            await _auditLoggingService.LogSecurityEventAsync(
                "failed_password_change",
                "Invalid current password provided for password change",
                failedIpAddress,
                userId.Value,
                null,
                HttpContext.TraceIdentifier);

            return ErrorResponse("Mt khu hin ti khng ng", 400);
        }

        // Validate new password strength
        var passwordValidation = ValidatePasswordStrength(changePasswordRequest.NewPassword);
        if (!passwordValidation.IsValid)
        {
            return ErrorResponse(passwordValidation.ErrorMessage, 400);
        }

        // Check if new password is different from current
        if (await _userService.VerifyPasswordAsync(user, changePasswordRequest.NewPassword))
        {
            return ErrorResponse("Mt khu mi phi khc vi mt khu hin ti", 400);
        }

        // Update password
        await _userService.UpdatePasswordAsync(userId.Value, changePasswordRequest.NewPassword);

        // Log successful password change
        _logger.LogInformation("Password changed successfully for user {UserId} from IP {IP}",
            userId.Value, GetClientIpAddress());

        // Record security event using audit logging service
        var successIpAddress = GetClientIpAddress();
        await _auditLoggingService.LogSecurityEventAsync(
            "password_changed",
            "Password changed successfully",
            successIpAddress,
            userId.Value,
            null,
            HttpContext.TraceIdentifier);

        return SuccessResponse(new { success = true }, "Mt khu  c thay i thnh cng");
    }

    /// <summary>
    /// Updates current user profile
    /// </summary>
    /// <param name="request">Profile update request</param>
    /// <returns>Updated user profile</returns>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return ErrorResponse("User not found", 401);

        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
            return ErrorResponse("User not found", 404);

        // Apply updates
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;
        if (!string.IsNullOrEmpty(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;

        var updatedUser = await _userService.UpdateUserAsync(user);
        var roles = await _userService.GetUserRolesAsync(userId.Value);
        var userDto = MapToUserProfileDto(updatedUser, roles);

        return SuccessResponse(userDto, "Profile updated successfully");
    }

    /// <summary>
    /// Uploads user avatar image to the configured image hosting service
    /// </summary>
    /// <param name="file">Avatar image file</param>
    /// <returns>Updated user profile with new avatar URL</returns>
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return ErrorResponse("User not found", 401);

        // Validate file
        if (file == null || file.Length == 0)
            return ErrorResponse("No file provided", 400);

        // Validate image using configured image hosting service
        if (!_imageHostingService.IsValidImage(file.FileName, file.ContentType, file.Length))
            return ErrorResponse("Invalid image file. Supported formats: JPEG, PNG, GIF, WebP. Max size: 10MB", 400);

        // Get current user
        var user = await _userService.GetByIdAsync(userId.Value);
        if (user == null)
            return ErrorResponse("User not found", 404);

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
        {
            // Extract delete hash from URL if possible and delete old image
            // Note: This requires storing delete hash in DB for full functionality
            _logger.LogInformation("User {UserId} replacing existing avatar", userId.Value);
        }

        // Upload new avatar to image hosting service
        using var stream = file.OpenReadStream();
        var uploadResult = await _imageHostingService.UploadImageAsync(stream, file.FileName, ImageCategory.Avatars);

        // Update user profile picture URL
        user.ProfilePictureUrl = uploadResult.Url;
        
        var updatedUser = await _userService.UpdateUserAsync(user);
        var roles = await _userService.GetUserRolesAsync(userId.Value);
        var userDto = MapToUserProfileDto(updatedUser, roles);

        _logger.LogInformation("Successfully uploaded avatar for user {UserId}: {ImageUrl}", userId.Value, uploadResult.Url);

        return SuccessResponse(new
        {
            user = userDto,
            avatarUrl = uploadResult.Url,
            message = "Avatar uploaded successfully"
        });
    }

    /// <summary>
    /// Gets paginated list of users (Admin only)
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="search">Search term</param>
    /// <returns>Paginated user list</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _userService.GetUsersAsync(page, pageSize, search);
        var userDtos = new List<UserSummaryDto>();

        foreach (var user in result.Items)
        {
            var roles = await _userService.GetUserRolesAsync(user.Id);
            var userDto = MapToUserSummaryDto(user, roles);
            userDtos.Add(userDto);
        }

        return PaginatedResponse(userDtos, result.TotalCount, page, pageSize);
    }

    /// <summary>
    /// Gets user by ID (Admin only or own profile)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User profile</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (!HasRole("Admin") && currentUserId != id)
            return ErrorResponse("Access denied", 403);

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return ErrorResponse("User not found", 404);

        var roles = await _userService.GetUserRolesAsync(id);
        var userDto = MapToUserProfileDto(user, roles);

        return SuccessResponse(userDto);
    }

    /// <summary>
    /// Creates new user (Admin only)
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Check if user already exists
        var existingUser = await _userService.GetByEmailAsync(request.Email);
        if (existingUser != null)
            return ErrorResponse("User with this email already exists");

        var user = new Core.Entities.User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber
        };

        var createdUser = await _userService.CreateUserAsync(user, request.Password, request.Roles);
        var roles = await _userService.GetUserRolesAsync(createdUser.Id);
        var userDto = MapToUserProfileDto(createdUser, roles);

        return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id },
            SuccessResponse(userDto, "User created successfully"));
    }

    /// <summary>
    /// Updates user (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">User update request</param>
    /// <returns>Updated user</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return ErrorResponse("User not found", 404);

        // Apply updates
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;
        if (!string.IsNullOrEmpty(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;
        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        var updatedUser = await _userService.UpdateUserAsync(user);

        // Update roles if provided
        if (request.Roles != null)
        {
            var currentRoles = await _userService.GetUserRolesAsync(id);

            // Remove roles not in the new list
            foreach (var currentRole in currentRoles)
            {
                if (!request.Roles.Contains(currentRole))
                {
                    await _userService.RemoveRoleAsync(id, currentRole);
                }
            }

            // Add new roles
            foreach (var newRole in request.Roles)
            {
                if (!currentRoles.Contains(newRole))
                {
                    await _userService.AssignRoleAsync(id, newRole);
                }
            }
        }

        var roles = await _userService.GetUserRolesAsync(id);
        var userDto = MapToUserProfileDto(updatedUser, roles);

        return SuccessResponse(userDto, "User updated successfully");
    }

    /// <summary>
    /// Deactivates user (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUser(int id)
    {
        var success = await _userService.DeactivateUserAsync(id);
        if (!success)
            return ErrorResponse("User not found", 404);

        return SuccessResponse(new { deactivated = true }, "User deactivated successfully");
    }

    /// <summary>
    /// Assigns role to user (Admin only)
    /// </summary>
    /// <param name="request">Role assignment request</param>
    /// <returns>Success status</returns>
    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var success = await _userService.AssignRoleAsync(request.UserId, request.RoleName);
        if (!success)
            return ErrorResponse("Failed to assign role");

        return SuccessResponse(new { assigned = true }, "Role assigned successfully");
    }

    /// <summary>
    /// Removes role from user (Admin only)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="roleName">Role name</param>
    /// <returns>Success status</returns>
    [HttpDelete("{userId}/roles/{roleName}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveRole(int userId, string roleName)
    {
        var success = await _userService.RemoveRoleAsync(userId, roleName);
        if (!success)
            return ErrorResponse("Failed to remove role");

        return SuccessResponse(new { removed = true }, "Role removed successfully");
    }

    #region Address Management Endpoints

    /// <summary>
    /// Gets all addresses for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of user addresses</returns>
    [HttpGet("{userId}/addresses")]
    public async Task<IActionResult> GetUserAddresses(int userId)
    {
        var currentUserId = GetCurrentUserId();
        if (!HasRole("Admin") && currentUserId != userId)
            return ErrorResponse("Access denied", 403);

        var addresses = await _userService.GetUserAddressesAsync(userId);
        var addressDtos = addresses.Select(MapToAddressDto).ToList();

        return SuccessResponse(addressDtos);
    }

    /// <summary>
    /// Creates a new address for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="request">Address creation request</param>
    /// <returns>Created address</returns>
    [HttpPost("{userId}/addresses")]
    public async Task<IActionResult> CreateUserAddress(int userId, [FromBody] CreateUserAddressRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (!HasRole("Admin") && currentUserId != userId)
            return ErrorResponse("Access denied", 403);

        // Verify user exists
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return ErrorResponse("User not found", 404);

        var address = new Core.Entities.Address
        {
            UserId = userId,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Street = request.Street,
            City = request.City,
            Province = request.Province,
            Ward = request.District, // District DTO maps to Ward entity
            PostalCode = request.PostalCode,
            Country = request.Country,
            IsDefault = request.IsDefault
        };

        var createdAddress = await _userService.CreateUserAddressAsync(address);
        var addressDto = MapToAddressDto(createdAddress);

        return CreatedAtAction(nameof(GetUserAddresses), new { userId },
            SuccessResponse(addressDto, "Address created successfully"));
    }

    /// <summary>
    /// Updates an existing address
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="addressId">Address ID</param>
    /// <param name="request">Address update request</param>
    /// <returns>Updated address</returns>
    [HttpPut("{userId}/addresses/{addressId}")]
    public async Task<IActionResult> UpdateUserAddress(int userId, int addressId, [FromBody] UpdateAddressRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (!HasRole("Admin") && currentUserId != userId)
            return ErrorResponse("Access denied", 403);

        // Get the existing address to update
        var existingAddresses = await _userService.GetUserAddressesAsync(userId);
        var addressToUpdate = existingAddresses.FirstOrDefault(a => a.Id == addressId);

        if (addressToUpdate == null)
            return ErrorResponse("Address not found", 404);

        // Apply updates only for non-null values
        // NOTE: In a real entity tracking scenario, we might iterate.
        // Since this object comes from service (maybe detached), we just update props and call update.
        addressToUpdate.FullName = request.FullName ?? addressToUpdate.FullName;
        addressToUpdate.PhoneNumber = request.PhoneNumber ?? addressToUpdate.PhoneNumber;
        addressToUpdate.Street = request.Street ?? addressToUpdate.Street;
        addressToUpdate.City = request.City ?? addressToUpdate.City;
        addressToUpdate.Province = request.Province ?? addressToUpdate.Province;
        addressToUpdate.Ward = request.District ?? addressToUpdate.Ward;
        addressToUpdate.PostalCode = request.PostalCode ?? addressToUpdate.PostalCode;
        addressToUpdate.Country = request.Country ?? addressToUpdate.Country;
        addressToUpdate.IsDefault = request.IsDefault ?? addressToUpdate.IsDefault;
        
        var result = await _userService.UpdateUserAddressAsync(addressToUpdate);
        if (result == null)
            return ErrorResponse("Address not found (during update)", 404);

        var addressDto = MapToAddressDto(result);
        return SuccessResponse(addressDto, "Address updated successfully");
    }

    /// <summary>
    /// Deletes an address
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="addressId">Address ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{userId}/addresses/{addressId}")]
    public async Task<IActionResult> DeleteUserAddress(int userId, int addressId)
    {
        var currentUserId = GetCurrentUserId();
        if (!HasRole("Admin") && currentUserId != userId)
            return ErrorResponse("Access denied", 403);

        var success = await _userService.DeleteUserAddressAsync(userId, addressId);
        if (!success)
            return ErrorResponse("Address not found", 404);

        return SuccessResponse(new { deleted = true }, "Address deleted successfully");
    }

    /// <summary>
    /// Sets an address as the default address for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="addressId">Address ID</param>
    /// <returns>Success status</returns>
    [HttpPatch("{userId}/addresses/{addressId}/default")]
    public async Task<IActionResult> SetDefaultAddress(int userId, int addressId)
    {
        var currentUserId = GetCurrentUserId();
        if (!HasRole("Admin") && currentUserId != userId)
            return ErrorResponse("Access denied", 403);

        var success = await _userService.SetDefaultAddressAsync(userId, addressId);
        if (!success)
            return ErrorResponse("Address not found", 404);

        return SuccessResponse(new { defaultSet = true }, "Default address updated successfully");
    }

    /// <summary>
    /// Gets user statistics (Admin only)
    /// </summary>
    /// <returns>User statistics</returns>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUserStatistics()
    {
        var statistics = await _userService.GetUserStatisticsAsync();
        return SuccessResponse(statistics);
    }

    #endregion

    #region Mapping Methods

    private UserProfileDto MapToUserProfileDto(Core.Entities.User user, IEnumerable<string> roles)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsActive = user.IsActive,
            Roles = roles.ToList(),
            Addresses = user.Addresses?.Select(MapToAddressDto).ToList() ?? new List<AddressDto>()
        };
    }

    private UserSummaryDto MapToUserSummaryDto(Core.Entities.User user, IEnumerable<string> roles)
    {
        return new UserSummaryDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            Roles = roles.ToList(),
            TotalOrders = 0, // This would need to be calculated
            TotalSpent = 0   // This would need to be calculated
        };
    }

    private AddressDto MapToAddressDto(Core.Entities.Address address)
    {
        return new AddressDto
        {
            Id = address.Id,
            FullName = address.FullName,
            PhoneNumber = address.PhoneNumber,
            Street = address.Street,
            City = address.City,
            Province = address.Province,
            District = address.Ward, // Ward entity maps to District DTO
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefault = address.IsDefault
        };
    }

    #endregion

    #region Security Helper Methods

    /// <summary>
    /// Validates password strength with comprehensive security checks
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Validation result</returns>
    private (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Mt khu khng c  trng");

        if (password.Length < 8)
            return (false, "Mt khu phi c t nht 8 k t");

        if (password.Length > 128)
            return (false, "Mt khu khng c vt qu 128 k t");

        // Check for common weak patterns
        if (password.All(c => c == password[0]))
            return (false, "Mt khu khng c cha tt c k t ging nhau");

        // Check for sequential characters
        if (IsSequential(password))
            return (false, "Mt khu khng c cha chui k t lin tip");

        // Check for common passwords
        var commonPasswords = new[]
        {
            "password", "123456", "123456789", "qwerty", "abc123", "password123",
            "admin", "letmein", "welcome", "monkey", "1234567890", "dragon",
            "master", "hello", "freedom", "whatever", "qazwsx", "trustno1"
        };

        if (commonPasswords.Any(common => password.Equals(common, StringComparison.OrdinalIgnoreCase)))
            return (false, "Mt khu qu ph bin, vui lng chn mt khu mnh hn");

        // Check for at least one character from each category
        var hasLower = password.Any(c => char.IsLower(c));
        var hasUpper = password.Any(c => char.IsUpper(c));
        var hasDigit = password.Any(c => char.IsDigit(c));
        var hasSpecial = password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));

        var categoryCount = new[] { hasLower, hasUpper, hasDigit, hasSpecial }.Count(x => x);

        if (categoryCount < 3)
            return (false, "Mt khu phi cha t nht 3 trong 4 loi: ch thng, ch hoa, s, k t c bit");

        return (true, string.Empty);
    }

    /// <summary>
    /// Checks if password contains sequential characters
    /// </summary>
    /// <param name="password">Password to check</param>
    /// <returns>True if contains sequential characters</returns>
    private bool IsSequential(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (char.IsDigit(password[i]) && char.IsDigit(password[i + 1]) && char.IsDigit(password[i + 2]))
            {
                if (password[i + 1] == password[i] + 1 && password[i + 2] == password[i] + 2)
                    return true;
                if (password[i + 1] == password[i] - 1 && password[i + 2] == password[i] - 2)
                    return true;
            }
        }
        return false;
    }


    #endregion
}
