using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Interfaces.Services;

public interface IAdminUserService
{
    Task<PagedResult<User>> GetAdminUsersAsync(int page, int pageSize, string? searchTerm = null);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAdminUserAsync(User user, string password, int roleId, string adminId);
    Task<User> UpdateAdminUserAsync(User user, string? password, int? roleId, string adminId);
    Task<bool> DeleteUserAsync(int id, string adminId);
    Task<User> ToggleUserStatusAsync(int id, string adminId);
}
