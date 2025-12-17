'use client';

import { adminAuth, hasPermission, hasRole, PERMISSIONS } from '@/lib/admin-api';
import { AdminAuthRequest, AdminUser } from '@/types/admin';
import { usePathname, useRouter } from 'next/navigation';
import { createContext, ReactNode, useContext, useEffect, useState } from 'react';

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
  handleAuthError: (error: any) => void;
}

const AdminAuthContext = createContext<AdminAuthContextType | undefined>(undefined);

interface AdminAuthProviderProps {
  children: ReactNode;
}

export function AdminAuthProvider({ children }: AdminAuthProviderProps) {
  const [user, setUser] = useState<AdminUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();
  const pathname = usePathname();

  const isAuthenticated = !!user;
  const userRole = user?.roleName;
  const isSuperAdmin = userRole === 'SystemAdmin' || userRole === 'SuperAdmin' || false;

  // Initialize auth state
  useEffect(() => {
    const initAuth = async () => {
      // Avoid auth checks on the admin login page to prevent 401 loops
      if (pathname && pathname.startsWith('/admin-login')) {
        setIsLoading(false);
        return;
      }
      try {
        const storedUser = adminAuth.getCurrentUserFromStorage();
        if (storedUser) {
          // Verify token is still valid
          const currentUser = await adminAuth.getCurrentUser();
          console.log(' AdminAuth - Current user verified:', currentUser);
          console.log(' AdminAuth - User permissions:', currentUser.permissions);
          setUser(currentUser);
          return;
        }

        // No stored user  try to load from secure cookie session
        const currentUser = await adminAuth.getCurrentUser();
        if (currentUser) {
          setUser(currentUser);
        }
      } catch (error) {
        console.error('Auth initialization failed:', error);
        // Clear invalid stored data
        if (typeof window !== 'undefined') {
          localStorage.removeItem('adminToken');
          localStorage.removeItem('adminUser');
        }
      } finally {
        setIsLoading(false);
      }
    };

    initAuth();
  }, [pathname]);

  const login = async (credentials: AdminAuthRequest) => {
    setIsLoading(true);
    try {
      const response = await adminAuth.login(credentials);
      console.log(' AdminAuth - Login response user:', response.user);
      console.log(' AdminAuth - User permissions:', response.user.permissions);
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
    setIsLoading(true);
    try {
      await adminAuth.logout();
      setUser(null);
      router.push('/admin-login');
    } catch (error) {
      console.error('Logout failed:', error);
      // Always clear user state and redirect even if logout API fails
      setUser(null);
      router.push('/admin-login');
    } finally {
      setIsLoading(false);
    }
  };

  // Handle authentication errors globally
  const handleAuthError = (error: any) => {
    console.error('Authentication error:', error);

    // Check if it's an authentication failure
    if (error?.message?.includes('Authentication failed') ||
      error?.status === 401 ||
      error?.code === 'AUTH_FAILED') {
      // Clear user state and redirect to login
      setUser(null);
      router.push('/admin-login');
      return;
    }

    // For other errors, just log them
    console.error('Non-auth error:', error);
  };

  const refetchUser = async () => {
    try {
      const currentUser = await adminAuth.getCurrentUser();
      setUser(currentUser);
    } catch (error) {
      console.error('Failed to refetch user:', error);
      handleAuthError(error);
    }
  };

  const checkPermission = (permission: string): boolean => {
    if (!user) return false;
    const userPermissions = user.permissions || [];
    return hasPermission(userPermissions, permission);
  };

  const checkRole = (roles: string[]): boolean => {
    if (!user) return false;
    const userRole = user.roleName;
    return hasRole(userRole, roles);
  };

  const value: AdminAuthContextType = {
    user,
    isLoading,
    isAuthenticated,
    login,
    logout,
    hasPermission: checkPermission,
    hasRole: checkRole,
    isSuperAdmin,
    refetchUser,
    handleAuthError, // Export handleAuthError for use in other components
  };

  return <AdminAuthContext.Provider value={value}>{children}</AdminAuthContext.Provider>;
}

// Hook to use admin auth context
export function useAdminAuth() {
  const context = useContext(AdminAuthContext);
  if (context === undefined) {
    throw new Error('useAdminAuth must be used within an AdminAuthProvider');
  }
  return context;
}

// Higher-order component for protected admin routes
interface ProtectedAdminRouteProps {
  children: ReactNode;
  requiredRole?: string[];
  requiredPermission?: string;
  fallback?: ReactNode;
}

export function ProtectedAdminRoute({
  children,
  requiredRole,
  requiredPermission,
  fallback
}: ProtectedAdminRouteProps) {
  const { user, isLoading, isAuthenticated } = useAdminAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    // Skip redirect if on login page or loading to prevent loops
    if (pathname?.startsWith('/admin-login') || isLoading) return;
    if (!isAuthenticated) {
      const returnUrl = encodeURIComponent(pathname || '/admin/dashboard');
      router.push(`/admin-login?returnUrl=${returnUrl}`);
    }
  }, [isLoading, isAuthenticated, router, pathname]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return fallback || null;
  }

  // Check role requirements
  if (requiredRole && user) {
    const userRole = user.roleName;
    if (!hasRole(userRole, requiredRole)) {
      return (
        <div className="flex items-center justify-center min-h-screen">
          <div className="text-center">
            <h1 className="text-2xl font-bold text-destructive mb-2">Khng c quyn truy cp</h1>
            <p className="text-muted-foreground">Bn khng c quyn truy cp vo trang ny.</p>
          </div>
        </div>
      );
    }
  }

  // Check permission requirements
  if (requiredPermission && user) {
    const userPermissions = user.permissions || [];
    if (!hasPermission(userPermissions, requiredPermission)) {
      return (
        <div className="flex items-center justify-center min-h-screen">
          <div className="text-center">
            <h1 className="text-2xl font-bold text-destructive mb-2">Khng c quyn truy cp</h1>
            <p className="text-muted-foreground">Bn khng c quyn thc hin hnh ng ny.</p>
          </div>
        </div>
      );
    }
  }

  return <>{children}</>;
}

// Component for conditional rendering based on permissions
interface PermissionGuardProps {
  permission: string;
  children: ReactNode;
  fallback?: ReactNode;
}

export function PermissionGuard({ permission, children, fallback }: PermissionGuardProps) {
  const { user } = useAdminAuth();

  if (!user) {
    return fallback || null;
  }

  const userPermissions = user.permissions || [];
  if (!hasPermission(userPermissions, permission)) {
    return fallback || null;
  }

  return <>{children}</>;
}

// Component for conditional rendering based on roles
interface RoleGuardProps {
  roles: string[];
  children: ReactNode;
  fallback?: ReactNode;
}

export function RoleGuard({ roles, children, fallback }: RoleGuardProps) {
  const { user } = useAdminAuth();

  if (!user) {
    return fallback || null;
  }

  if (!hasRole(user.roleName, roles)) {
    return fallback || null;
  }

  return <>{children}</>;
}

// Admin menu items configuration based on roles and permissions
export const getAdminMenuItems = (user: AdminUser | null) => {
  if (!user) return [];

  const userPermissions = user.permissions || [];

  const menuItems = [
    {
      id: 'dashboard',
      label: 'Dashboard',
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
      id: 'customers',
      label: 'Qun l khch hng',
      icon: 'Users',
      href: '/admin/customers',
      requiredPermission: null,
    },
    {
      id: 'roles',
      label: 'Qun l vai tr',
      icon: 'Shield',
      href: '/admin/roles',
      requiredPermission: PERMISSIONS.ROLES_READ,
    },
    {
      id: 'products',
      label: 'Sn phm',
      icon: 'Package',
      href: '/admin/products',
      requiredPermission: PERMISSIONS.PRODUCTS_READ,
    },
    {
      id: 'orders',
      label: 'n hng',
      icon: 'ShoppingCart',
      href: '/admin/orders',
      requiredPermission: PERMISSIONS.ORDERS_READ,
    },
    {
      id: 'blog',
      label: 'Qun l blog',
      icon: 'FileText',
      href: '/admin/blog',
      requiredPermission: null,
    },
    {
      id: 'security',
      label: 'Bo mt',
      icon: 'Lock',
      href: '/admin/security',
      requiredPermission: PERMISSIONS.SECURITY_READ,
    },
    {
      id: 'settings',
      label: 'Ci t',
      icon: 'Settings',
      href: '/admin/settings',
      requiredPermission: PERMISSIONS.SETTINGS_READ,
    },
    {
      id: 'llm-config',
      label: 'AI Config',
      icon: 'Bot',
      href: '/admin/llm-config',
      requiredPermission: PERMISSIONS.SETTINGS_READ, // Using Settings permission for now
    },
  ];

  // Filter menu items based on user permissions
  return menuItems.filter(item => {
    if (!item.requiredPermission) return true;
    return hasPermission(userPermissions, item.requiredPermission);
  });
};