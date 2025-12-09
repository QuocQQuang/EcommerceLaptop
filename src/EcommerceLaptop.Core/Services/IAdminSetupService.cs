namespace EcommerceLaptop.Core.Services;

public interface IAdminSetupService
{
    Task<bool> EnsureDefaultAdminExistsAsync();
    Task<bool> CreateAdminUserAsync(string email, string password, string firstName, string lastName, string roleName = "SystemAdmin");
}