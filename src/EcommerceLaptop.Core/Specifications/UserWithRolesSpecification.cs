using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications;

public class UserWithRolesSpecification : BaseSpecification<User>
{
    public UserWithRolesSpecification()
    {
        AddInclude(u => u.UserRoles);
        AddInclude($"{nameof(User.UserRoles)}.{nameof(UserRole.Role)}");
    }

    public UserWithRolesSpecification(int id) : base(u => u.Id == id)
    {
        AddInclude(u => u.UserRoles);
        AddInclude($"{nameof(User.UserRoles)}.{nameof(UserRole.Role)}");
    }

    public UserWithRolesSpecification(string email) : base(u => u.Email == email)
    {
        AddInclude(u => u.UserRoles);
        AddInclude($"{nameof(User.UserRoles)}.{nameof(UserRole.Role)}");
    }
}
