using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using BCrypt.Net;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// User service implementation with role-based access control
/// Implements secure password hashing and user management
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    }

    public async Task<User> CreateUserAsync(User user, string password, IEnumerable<string> roles)
    {
        // Hash password using BCrypt
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.IsActive = true;

        // Add user to context
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assign roles
        foreach (var roleName in roles)
        {
            await AssignRoleAsync(user.Id, roleName);
        }

        return user;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeactivateUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    public async Task<bool> AssignRoleAsync(int userId, string roleName)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return false;

        var existingUserRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
        
        if (existingUserRole != null) return true; // Already assigned

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = role.Id
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveRoleAsync(int userId, string roleName)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null) return false;

        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
        
        if (userRole == null) return false;

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyPasswordAsync(User user, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        var userRoles = await GetUserRolesAsync(userId);
        
        // Define role-based permissions
        var rolePermissions = new Dictionary<string, List<string>>
        {
            ["Admin"] = new List<string> { "*" }, // Admin has all permissions
            ["Sales"] = new List<string> { "order.read", "order.update", "product.read", "customer.read" },
            ["Marketing"] = new List<string> { "campaign.read", "campaign.write", "coupon.read", "coupon.write", "product.read" },
            ["Customer"] = new List<string> { "order.read.own", "profile.read.own", "profile.update.own" }
        };

        foreach (var role in userRoles)
        {
            if (rolePermissions.ContainsKey(role))
            {
                var permissions = rolePermissions[role];
                if (permissions.Contains("*") || permissions.Contains(permission))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<PagedResult<User>> GetUsersAsync(int page, int pageSize, string? searchTerm = null)
    {
        var query = _context.Users.Where(u => u.IsActive);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(u => 
                u.Email.Contains(searchTerm) ||
                u.FirstName.Contains(searchTerm) ||
                u.LastName.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<User>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<Address>> GetUserAddressesAsync(int userId)
    {
        return await _context.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Address> CreateUserAddressAsync(Address address)
    {
        // If this is set as default, unset all other default addresses for this user
        if (address.IsDefault)
        {
            await UnsetAllDefaultAddressesForUserAsync(address.UserId);
        }

        address.CreatedAt = DateTime.UtcNow;
        _context.Addresses.Add(address);
        await _context.SaveChangesAsync();
        return address;
    }

    public async Task<Address?> UpdateUserAddressAsync(Address address)
    {
        var existingAddress = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == address.Id && a.UserId == address.UserId);

        if (existingAddress == null)
            return null;

        // If this is being set as default, unset all other default addresses for this user
        if (address.IsDefault && !existingAddress.IsDefault)
        {
            await UnsetAllDefaultAddressesForUserAsync(address.UserId);
        }

        // Update properties
        existingAddress.FullName = address.FullName;
        existingAddress.PhoneNumber = address.PhoneNumber;
        existingAddress.Street = address.Street;
        existingAddress.Ward = address.Ward;
        existingAddress.District = address.District;
        existingAddress.City = address.City;
        existingAddress.Country = address.Country;
        existingAddress.IsDefault = address.IsDefault;
        existingAddress.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existingAddress;
    }

    public async Task<bool> DeleteUserAddressAsync(int userId, int addressId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (address == null)
            return false;

        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync();

        // If this was the default address, try to set another address as default
        if (address.IsDefault)
        {
            var nextAddress = await _context.Addresses
                .Where(a => a.UserId == userId)
                .FirstOrDefaultAsync();

            if (nextAddress != null)
            {
                nextAddress.IsDefault = true;
                await _context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> SetDefaultAddressAsync(int userId, int addressId)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

        if (address == null)
            return false;

        // Unset all other default addresses for this user
        await UnsetAllDefaultAddressesForUserAsync(userId);

        // Set this address as default
        address.IsDefault = true;
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task UnsetAllDefaultAddressesForUserAsync(int userId)
    {
        var defaultAddresses = await _context.Addresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync();

        foreach (var address in defaultAddresses)
        {
            address.IsDefault = false;
        }

        if (defaultAddresses.Any())
        {
            await _context.SaveChangesAsync();
        }
    }
}
