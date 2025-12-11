namespace EcommerceLaptop.Core.DTOs.Admin;

public class DevDebugAdminDto
{
    public object AdminUser { get; set; } = null!;
    public List<DevDebugRoleDto> Roles { get; set; } = new();
}

public class DevDebugRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAdminRole { get; set; }
    public int PermissionCount { get; set; }
    public List<DevDebugPermissionDto> Permissions { get; set; } = new();
}

public class DevDebugPermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
