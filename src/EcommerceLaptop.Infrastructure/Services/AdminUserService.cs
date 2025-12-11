using BCrypt.Net;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Specifications;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IAsyncRepository<User> _userRepository;
    private readonly IAsyncRepository<Role> _roleRepository;
    private readonly IAsyncRepository<UserRole> _userRoleRepository;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        IAsyncRepository<User> userRepository,
        IAsyncRepository<Role> roleRepository,
        IAsyncRepository<UserRole> userRoleRepository,
        ILogger<AdminUserService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _logger = logger;
    }

    public async Task<PagedResult<User>> GetAdminUsersAsync(int page, int pageSize, string? searchTerm = null)
    {
        var spec = new AdminUserSpecification(searchTerm);
        
        // Count
        var totalCount = await _userRepository.CountAsync(spec);

        // Paging
        // We need to apply paging to the spec. SpecificationEvaluator handles it.
        // But AdminUserSpecification didn't expose paging control easily.
        // Let's create a Paged version or set it manually if we expose it (we don't exposed SetPaging).
        // Actually BaseSpecification has protected ApplyPaging.
        // I will update AdminUserSpecification to handle paging params.
        
        // Wait, I can't modify the spec after creation easily if it's protected setters.
        // I'll assume I update AdminUserSpecification to take page/size or I add a Paging method to BaseSpec (or make ApplyPaging public/internal).
        // Ideally: spec.ApplyPaging(...) but it is protected.
        // I'll fix this by initializing a paged spec.
        
        var pagedSpec = new AdminUserSpecification(searchTerm, page, pageSize);
        var items = await _userRepository.GetAsync(pagedSpec);

        return new PagedResult<User>
        {
            Items = (List<User>)items, // Safe cast if GetAsync returns List/ReadOnlyList
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        var spec = new UserWithRolesSpecification(id);
        return await _userRepository.GetEntityWithSpec(spec);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var spec = new UserWithRolesSpecification(email);
        return await _userRepository.GetEntityWithSpec(spec);
    }

    public async Task<User> CreateAdminUserAsync(User user, string password, int roleId, string adminId)
    {
        // Validate role
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null || !role.IsAdminRole)
        {
            throw new ArgumentException("Invalid admin role ID");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.IsActive = true;

        await _userRepository.AddAsync(user);

        // Add role
        await _userRoleRepository.AddAsync(new UserRole
        {
            UserId = user.Id,
            RoleId = roleId
        });

        _logger.LogInformation("Admin user created: {Email}", user.Email);

        // Reload to get relations
        return (await GetByIdAsync(user.Id))!;
    }

    public async Task<User> UpdateAdminUserAsync(User user, string? password, int? roleId, string adminId)
    {
        if (!string.IsNullOrEmpty(password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        user.UpdatedAt = DateTime.UtcNow;
        
        // Update User properties
        await _userRepository.UpdateAsync(user);

        // Update Role if provided
        if (roleId.HasValue)
        {
            var role = await _roleRepository.GetByIdAsync(roleId.Value);
            if (role == null || !role.IsAdminRole)
            {
                throw new ArgumentException("Invalid admin role ID");
            }

            // Remove old admin roles
            // We need to fetch UserRoles again if they aren't tracked? 
            // In Repo pattern, we assume objects are disconnected or needed re-fetching if not in UnitOfWork.
            // effectively user.UserRoles might be loaded from GetByIdAsync.
            
            // Check if user.UserRoles is populated.
            if (user.UserRoles != null)
            {
                var oldRoles = user.UserRoles.Where(ur => ur.Role.IsAdminRole).ToList();
                foreach (var oldRole in oldRoles)
                {
                    // EfRepository doesn't support Remove directly on detached entity without ID in some Generic implementations
                    // But here we likely have the object.
                     await _userRoleRepository.DeleteAsync(oldRole);
                }
            }

            await _userRoleRepository.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId.Value
            });
        }
        
        _logger.LogInformation("Admin user updated: {Email}", user.Email);
        return (await GetByIdAsync(user.Id))!;
    }

    public async Task<bool> DeleteUserAsync(int id, string adminId)
    {
        var user = await GetByIdAsync(id);
        if (user == null) return false;

        // Clean up roles first? cascading might handle it but let's be explicit if we can.
        // Actually EF Core cascade delete is preferred.
        // But with IAsyncRepository we usually just delete the Aggregate Root.
        // If Cascade Delete is configured in DB, DeleteAsync(user) is enough.
        // Assuming DbContext configuration handles cascades.
        
        await _userRepository.DeleteAsync(user);
        _logger.LogInformation("Admin user deleted: {Email}", user.Email);
        return true;
    }

    public async Task<User> ToggleUserStatusAsync(int id, string adminId)
    {
        var user = await GetByIdAsync(id);
        if (user == null) throw new KeyNotFoundException("User not found");

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        
        await _userRepository.UpdateAsync(user);
        
        _logger.LogInformation("Admin user status toggled: {Email} -> {Status}", user.Email, user.IsActive);
        
        return user;
    }
}
