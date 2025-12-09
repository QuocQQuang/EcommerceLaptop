'use client';

import { secureAdminAuth } from '@/lib/secure-admin-api';
import { AdminAuthRequest, AdminUser } from '@/types/admin';
import { usePathname, useRouter } from 'next/navigation';
import { createContext, ReactNode, useContext, useEffect, useState } from 'react';

// Permission constants - matches backend permission system
export const PERMISSIONS = {
    // Dashboard
    DASHBOARD_READ: 'dashboard:read',

    // User Management  
    USERS_READ: 'users:read',
    USERS_WRITE: 'users:write',
    USERS_DELETE: 'users:delete',
    USERS_MANAGE: 'users:manage',

    // Role Management  
    ROLES_READ: 'roles:read',
    ROLES_WRITE: 'roles:write',
    ROLES_DELETE: 'roles:delete',
    ROLES_MANAGE: 'roles:manage',

    // Product Management
    PRODUCTS_READ: 'products:read',
    PRODUCTS_WRITE: 'products:write',
    PRODUCTS_DELETE: 'products:delete',
    PRODUCTS_MANAGE: 'products:manage',

    // Order Management
    ORDERS_READ: 'orders:read',
    ORDERS_WRITE: 'orders:write',
    ORDERS_DELETE: 'orders:delete',
    ORDERS_MANAGE: 'orders:manage',

    // Promotion Management
    PROMOTIONS_READ: 'promotions:read',
    PROMOTIONS_WRITE: 'promotions:write',
    PROMOTIONS_DELETE: 'promotions:delete',
    PROMOTIONS_MANAGE: 'promotions:manage',

    // Permission Management
    PERMISSIONS_READ: 'permissions:read',
    PERMISSIONS_WRITE: 'permissions:write',
    PERMISSIONS_MANAGE: 'permissions:manage',

    // Settings Management
    SETTINGS_READ: 'settings:read',
    SETTINGS_WRITE: 'settings:write',
    SETTINGS_MANAGE: 'settings:manage',

    // Security Management
    SECURITY_READ: 'security:read',
    SECURITY_WRITE: 'security:write',
    SECURITY_MANAGE: 'security:manage',

    // Audit & Logs
    LOGS_READ: 'logs:read',
    LOGS_MANAGE: 'logs:manage',
} as const;

// Helper functions for permissions and roles
export function hasPermission(user: AdminUser | null, permission: string): boolean {
    if (!user || !user.roleName) return false;

    // SuperAdmin and SystemAdmin have all permissions
    if (user.roleName === 'SuperAdmin' || user.roleName === 'SystemAdmin') {
        return true;
    }

    // Check if user has the specific permission
    return user.permissions?.includes(permission) || false;
}

export function hasRole(user: AdminUser | null, roles: string[]): boolean {
    if (!user || !user.roleName) return false;
    return roles.includes(user.roleName);
}

interface AdminAuthContextType {
    user: AdminUser | null;
    isLoading: boolean;
    isAuthenticated: boolean;
    login: (credentials: AdminAuthRequest) => Promise<void>;
    logout: () => Promise<void>;
    hasPermission: (permission: string) => boolean;
    hasRole: (roles: string[]) => boolean;
    isSuperAdmin: boolean;
    refetchUser: () => Promise<void>;
}

const AdminAuthContext = createContext<AdminAuthContextType | undefined>(undefined);

interface AdminAuthProviderProps {
    children: ReactNode;
}

export function AdminAuthProvider({ children }: AdminAuthProviderProps) {
    const [user, setUser] = useState<AdminUser | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const router = useRouter();

    const isAuthenticated = !!user;
    const isSuperAdmin = user?.roleName === 'SystemAdmin' || user?.roleName === 'SuperAdmin' || false;

    // Initialize auth state from secure cookie session
    useEffect(() => {
        const initAuth = async () => {
            try {
                const currentUser = await secureAdminAuth.getCurrentUser();
                setUser(currentUser);
            } catch (error) {
                console.error('Auth initialization failed:', error);
                // User is not authenticated, which is fine for public routes
                setUser(null);
            } finally {
                setIsLoading(false);
            }
        };

        initAuth();
    }, []);

    const login = async (credentials: AdminAuthRequest) => {
        try {
            setIsLoading(true);
            const response = await secureAdminAuth.login(credentials);
            setUser(response.user);

            // Redirect to returnUrl or dashboard
            const returnUrl = new URLSearchParams(window.location.search).get('returnUrl') || '/admin/dashboard';
            router.push(decodeURIComponent(returnUrl));
        } catch (error) {
            console.error('Login failed:', error);
            throw error;
        } finally {
            setIsLoading(false);
        }
    };

    const logout = async () => {
        try {
            setIsLoading(true);
            await secureAdminAuth.logout();
            setUser(null);

            // Redirect will be handled by secureAdminAuth.logout()
        } catch (error) {
            console.error('Logout failed:', error);
            // Still clear local state
            setUser(null);
            router.push('/admin-login');
        } finally {
            setIsLoading(false);
        }
    };

    const refetchUser = async () => {
        try {
            const currentUser = await secureAdminAuth.getCurrentUser();
            setUser(currentUser);
        } catch (error) {
            console.error('Failed to refetch user:', error);
            setUser(null);
        }
    };

    // Route protection effect
    const pathname = usePathname();

    useEffect(() => {
        // Skip if loading or on login page to prevent loops
        if (isLoading || pathname?.startsWith('/admin-login')) return;
        if (pathname && pathname.startsWith('/admin') && !isAuthenticated) {
            router.push('/admin-login');
        } else if (pathname === '/admin-login' && isAuthenticated) {
            // Redirect to returnUrl or dashboard
            const returnUrl = new URLSearchParams(window.location.search).get('returnUrl') || '/admin/dashboard';
            router.push(decodeURIComponent(returnUrl));
        }
    }, [isAuthenticated, isLoading, pathname, router]);

    const value: AdminAuthContextType = {
        user,
        isLoading,
        isAuthenticated,
        login,
        logout,
        hasPermission: (permission: string) => hasPermission(user, permission),
        hasRole: (roles: string[]) => hasRole(user, roles),
        isSuperAdmin,
        refetchUser,
    };

    return (
        <AdminAuthContext.Provider value={value}>
            {children}
        </AdminAuthContext.Provider>
    );
}

export function useAdminAuth(): AdminAuthContextType {
    const context = useContext(AdminAuthContext);
    if (context === undefined) {
        throw new Error('useAdminAuth must be used within an AdminAuthProvider');
    }
    return context;
}

// Admin menu items configuration based on roles and permissions
export const getAdminMenuItems = (user: AdminUser | null) => {
    if (!user) return [];

    const menuItems = [
        {
            id: 'dashboard',
            label: 'Bng iu khin',
            icon: 'LayoutDashboard',
            href: '/admin/dashboard',
            requiredPermission: null,
        },
        {
            id: 'users',
            label: 'Qun l Admin',
            icon: 'Users',
            href: '/admin/users',
            requiredPermission: PERMISSIONS.USERS_READ,
        },
        {
            id: 'roles',
            label: 'Qun l vai tr',
            icon: 'Shield',
            href: '/admin/roles',
            requiredPermission: PERMISSIONS.ROLES_READ,
        },
        {
            id: 'security',
            label: 'Bo mt h thng',
            icon: 'Lock',
            href: '/admin/security',
            requiredPermission: PERMISSIONS.SECURITY_READ,
        },
        {
            id: 'products',
            label: 'Qun l sn phm',
            icon: 'Package',
            href: '/admin/products',
            requiredPermission: PERMISSIONS.PRODUCTS_READ,
        },
        {
            id: 'orders',
            label: 'Qun l n hng',
            icon: 'ShoppingCart',
            href: '/admin/orders',
            requiredPermission: PERMISSIONS.ORDERS_READ,
        },

        {
            id: 'settings',
            label: 'Ci t h thng',
            icon: 'Settings',
            href: '/admin/settings',
            requiredPermission: PERMISSIONS.SETTINGS_READ,
        },
    ];

    // Filter menu items based on user permissions
    return menuItems.filter(item => {
        if (!item.requiredPermission) return true;
        return hasPermission(user, item.requiredPermission);
    });
};

// Permission Guard component
interface PermissionGuardProps {
    permission: string;
    children: ReactNode;
    fallback?: ReactNode;
}

export function PermissionGuard({ permission, children, fallback }: PermissionGuardProps) {
    const { user } = useAdminAuth();

    if (!hasPermission(user, permission)) {
        return fallback || null;
    }

    return <>{children}</>;
}

// Protected Admin Route component
interface ProtectedAdminRouteProps {
    children: ReactNode;
    requiredPermissions?: string[];
    requiredRoles?: string[];
    fallback?: ReactNode;
}

export function ProtectedAdminRoute({
    children,
    requiredPermissions = [],
    requiredRoles = [],
    fallback
}: ProtectedAdminRouteProps) {
    const { user, isLoading } = useAdminAuth();

    if (isLoading) {
        return <div>Loading...</div>;
    }

    if (!user) {
        return fallback || <div>Access Denied</div>;
    }

    // Check required permissions
    const hasRequiredPermissions = requiredPermissions.length === 0 ||
        requiredPermissions.every(permission => hasPermission(user, permission));

    // Check required roles  
    const hasRequiredRoles = requiredRoles.length === 0 ||
        hasRole(user, requiredRoles);

    if (!hasRequiredPermissions || !hasRequiredRoles) {
        return fallback || <div>Access Denied</div>;
    }

    return <>{children}</>;
}

export default AdminAuthContext;