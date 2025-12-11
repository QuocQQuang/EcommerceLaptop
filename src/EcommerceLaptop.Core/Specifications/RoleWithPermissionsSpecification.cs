using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications;

public class RoleWithPermissionsSpecification : BaseSpecification<Role>
{
    public RoleWithPermissionsSpecification()
    {
        AddInclude(r => r.RolePermissions);
        AddInclude($"{nameof(Role.RolePermissions)}.{nameof(RolePermission.Permission)}");
        AddInclude(r => r.UserRoles);
        AddInclude($"{nameof(Role.UserRoles)}.{nameof(UserRole.User)}");
        // Or properly typed then-include if I extended the evaluator to support ThenInclude,
        // but for now String includes for ThenInclude or simple Includes are standard in this pattern implementation style
        // unless I enhance SpecificationEvaluator.
        // Let's check Evaluator again. It blindly adds Includes.
        // Standard EF Core doesn't support nested Include via simple expression list easily without ThenInclude.
        // Using string include is safer for "RolePermissions.Permission".
    }

    public RoleWithPermissionsSpecification(int id) : base(r => r.Id == id)
    {
        AddInclude(r => r.RolePermissions);
        AddInclude($"{nameof(Role.RolePermissions)}.{nameof(RolePermission.Permission)}");
        AddInclude(r => r.UserRoles);
        AddInclude($"{nameof(Role.UserRoles)}.{nameof(UserRole.User)}");
    }
}
