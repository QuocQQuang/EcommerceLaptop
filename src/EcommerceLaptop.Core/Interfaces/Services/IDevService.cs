using EcommerceLaptop.Core.DTOs.Admin;

namespace EcommerceLaptop.Core.Interfaces.Services;

public interface IDevService
{
    Task SeedPermissionsAsync();
    Task<List<DevDebugAdminDto>> GetDebugAdminInfoAsync();
}
