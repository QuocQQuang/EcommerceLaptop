using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.DTOs.Admin;

/// <summary>
/// Extended admin user DTO with role information for management
/// </summary>
public class AdminUserManagementDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public bool IsActive { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>
/// Users list response with pagination
/// </summary>
public class UsersResponseDto
{
    public List<AdminUserManagementDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// Create admin user request DTO
/// </summary>
public class CreateUserRequestDto
{
    [Required(ErrorMessage = "Email l bt buc")]
    [EmailAddress(ErrorMessage = "Email khng hp l")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tn l bt buc")]
    [StringLength(50, ErrorMessage = "Tn khng c qu 50 k t")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "H l bt buc")]
    [StringLength(50, ErrorMessage = "H khng c qu 50 k t")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mt khu l bt buc")]
    [MinLength(8, ErrorMessage = "Mt khu phi c t nht 8 k t")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role ID l bt buc")]
    public int RoleId { get; set; }
}

/// <summary>
/// Update admin user request DTO
/// </summary>
public class UpdateUserRequestDto
{
    [EmailAddress(ErrorMessage = "Email khng hp l")]
    public string? Email { get; set; }

    [StringLength(50, ErrorMessage = "Tn khng c qu 50 k t")]
    public string? FirstName { get; set; }

    [StringLength(50, ErrorMessage = "H khng c qu 50 k t")]
    public string? LastName { get; set; }

    [MinLength(8, ErrorMessage = "Mt khu phi c t nht 8 k t")]
    public string? Password { get; set; }

    public int? RoleId { get; set; }
}

/// <summary>
/// Create role request DTO
/// </summary>
public class CreateRoleRequestDto
{
    [Required(ErrorMessage = "Tn role l bt buc")]
    [StringLength(100, ErrorMessage = "Tn role khng c qu 100 k t")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "M t khng c qu 500 k t")]
    public string Description { get; set; } = string.Empty;

    public List<int> PermissionIds { get; set; } = new();
}

/// <summary>
/// Update role request DTO
/// </summary>
public class UpdateRoleRequestDto
{
    [StringLength(100, ErrorMessage = "Tn role khng c qu 100 k t")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "M t khng c qu 500 k t")]
    public string? Description { get; set; }

    public List<int>? PermissionIds { get; set; }
}

/// <summary>
/// Role with permissions DTO
/// </summary>
public class RoleWithPermissionsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AdminPermissionDto> Permissions { get; set; } = new();
    public int UsersCount { get; set; } // Number of users with this role
}