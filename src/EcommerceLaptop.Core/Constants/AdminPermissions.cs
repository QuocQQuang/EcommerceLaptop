namespace EcommerceLaptop.Core.Constants;

/// <summary>
/// Admin permission constants
/// </summary>
public static class AdminPermissions
{
    // Dashboard permissions
    public const string DashboardRead = "dashboard:read";
    
    // User management permissions
    public const string UsersRead = "users:read";
    public const string UsersWrite = "users:write";
    public const string UsersDelete = "users:delete";
    public const string UsersManage = "users:manage";
    
    // Role management permissions
    public const string RolesRead = "roles:read";
    public const string RolesWrite = "roles:write";
    public const string RolesDelete = "roles:delete";
    public const string RolesManage = "roles:manage";
    
    // Product management permissions
    public const string ProductsRead = "products:read";
    public const string ProductsWrite = "products:write";
    public const string ProductsDelete = "products:delete";
    public const string ProductsManage = "products:manage";
    
    // Order management permissions
    public const string OrdersRead = "orders:read";
    public const string OrdersWrite = "orders:write";
    public const string OrdersDelete = "orders:delete";
    public const string OrdersManage = "orders:manage";
    
    // Promotion management permissions
    public const string PromotionsRead = "promotions:read";
    public const string PromotionsWrite = "promotions:write";
    public const string PromotionsDelete = "promotions:delete";
    public const string PromotionsManage = "promotions:manage";
    
    // Settings permissions
    public const string SettingsRead = "settings:read";
    public const string SettingsWrite = "settings:write";
    public const string SettingsManage = "settings:manage";
    
    // Logs and audit permissions
    public const string LogsRead = "logs:read";
    public const string LogsManage = "logs:manage";
    
    // Security permissions
    public const string SecurityRead = "security:read";
    public const string SecurityWrite = "security:write";
    public const string SecurityManage = "security:manage";
}