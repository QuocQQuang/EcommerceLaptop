using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using BCrypt.Net;

namespace EcommerceLaptop.Infrastructure.Services;

public class AdminSetupService : IAdminSetupService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminSetupService> _logger;

    public AdminSetupService(ApplicationDbContext context, ILogger<AdminSetupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> EnsureDefaultAdminExistsAsync()
    {
        // --- TEST FIX: Ensure farfir124 is admin ---
        try 
        {
            var testUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == "farfir124@gmail.com");
                
            if (testUser != null && !testUser.UserRoles.Any(ur => ur.Role.IsAdminRole))
            {
                 var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.IsAdminRole);
                 if (adminRole != null)
                 {
                     _context.UserRoles.Add(new UserRole { UserId = testUser.Id, RoleId = adminRole.Id });
                     await _context.SaveChangesAsync();
                     _logger.LogWarning("Promoted farfir124@gmail.com to Admin for testing.");
                 }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to promote test user.");
        }
        // -------------------------------------------

        // Check if any admin user exists
        var adminExists = await _context.Users
            .AnyAsync(u => u.UserRoles.Any(ur => ur.Role.IsAdminRole));

        if (adminExists)
        {
            _logger.LogInformation("Admin users already exist in system");
            return true;
        }

        // Create default system admin if needed
        var success = await CreateAdminUserAsync(
            "admin@system.local",
            "Admin@123!",
            "System", 
            "Administrator",
            "SystemAdmin"
        );

        if (success)
        {
            _logger.LogWarning("Default admin user created: admin@system.local / Admin@123!");
            _logger.LogWarning("Please change the default password immediately!");
        }

        return success;
    }

    public async Task<bool> CreateAdminUserAsync(string email, string password, string firstName, string lastName, string roleName = "SystemAdmin")
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Check if user already exists
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                _logger.LogWarning("User with email {Email} already exists", email);
                return false;
            }

            // Get or create admin role
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.IsAdminRole);
            if (role == null)
            {
                role = new Role
                {
                    Name = roleName,
                    Description = $"Auto-created {roleName} role",
                    IsAdminRole = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
            }

            // Create user
            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign role
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Admin user created: {Email} with role {RoleName}", email, roleName);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create admin user: {Email}", email);
            return false;
        }
    }
}