using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications;

public class AdminUserSpecification : BaseSpecification<User>
{
    public AdminUserSpecification(string? searchTerm = null) 
        : base(u => u.UserRoles.Any(ur => ur.Role.IsAdminRole))
    {
        AddInclude(u => u.UserRoles);
        AddInclude($"{nameof(User.UserRoles)}.{nameof(UserRole.Role)}");

        if (!string.IsNullOrEmpty(searchTerm))
        {
            // Note: Criteria is already set in base constructor.
            // Complex criteria combining Admin filter AND search term need careful handling in SpecificationEvaluator 
            // OR we define the predicate entirely here.
            // Since BaseSpecification takes a single predicate, we should construct the full predicate here.
            
            // Re-defining criteria cannot be done easily after base().
            // So we'll use a static helper or complex logic in constructor values.
            // Actually, let's just use the predicate in the constructor call.
        }
    }

    public AdminUserSpecification(string? searchTerm, int page, int pageSize) 
        : base(u => u.UserRoles.Any(ur => ur.Role.IsAdminRole) && 
                    (string.IsNullOrEmpty(searchTerm) || 
                     u.FirstName.Contains(searchTerm) || 
                     u.LastName.Contains(searchTerm) || 
                     u.Email.Contains(searchTerm)))
    {
        AddInclude(u => u.UserRoles);
        AddInclude($"{nameof(User.UserRoles)}.{nameof(UserRole.Role)}");
        AddOrderBy(u => u.FirstName);
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}
