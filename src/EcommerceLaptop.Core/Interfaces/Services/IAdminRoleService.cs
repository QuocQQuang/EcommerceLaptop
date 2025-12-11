using EcommerceLaptop.Core.DTOs.Admin;

namespace EcommerceLaptop.Core.Interfaces.Services;

public interface IAdminRoleService
{
    Task<List<RoleWithPermissionsDto>> GetAllRolesAsync();
    Task<RoleWithPermissionsDto?> GetRoleByIdAsync(int id);
    Task<RoleWithPermissionsDto> CreateRoleAsync(CreateRoleRequestDto request, string adminId);
    Task<RoleWithPermissionsDto> UpdateRoleAsync(int id, UpdateRoleRequestDto request, string adminId);
    Task DeleteRoleAsync(int id, string adminId);
    Task<RoleWithPermissionsDto> AssignPermissionsAsync(int roleId, List<int> permissionIds, string adminId);
}
