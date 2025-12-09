// Dynamic Permission Provider for Admin Frontend
// Replaces hardcoded PERMISSIONS constants with API-driven dynamic loading

import React, { createContext, ReactNode, useContext, useEffect, useState } from 'react';
import { adminApi } from '../lib/admin-api';

// =====================================================
// Permission Types & Interfaces
// =====================================================

export interface AdminPermission {
    id: number;
    name: string;
    description: string;
    module?: string;
    action?: string;
}

export interface RolePermissionMapping {
    [roleName: string]: AdminPermission[];
}

export interface PermissionContextType {
    // Permission Data
    permissions: AdminPermission[];
    myPermissions: AdminPermission[];
    rolePermissions: RolePermissionMapping;

    // Loading States
    loading: boolean;
    error: string | null;

    // Permission Checking
    hasPermission: (permission: string) => boolean;
    hasAnyPermission: (permissions: string[]) => boolean;
    hasAllPermissions: (permissions: string[]) => boolean;

    // Refresh Methods
    refreshPermissions: () => Promise<void>;
    syncPermissions: () => Promise<void>;

    // Permission Schema
    schema: {
        modules: string[];
        actions: string[];
    } | null;
}

// =====================================================
// Permission Context
// =====================================================

const PermissionContext = createContext<PermissionContextType | null>(null);

export const usePermissions = (): PermissionContextType => {
    const context = useContext(PermissionContext);
    if (!context) {
        throw new Error('usePermissions must be used within a PermissionProvider');
    }
    return context;
};

// =====================================================
// Permission Provider Component
// =====================================================

interface PermissionProviderProps {
    children: ReactNode;
}

export const PermissionProvider: React.FC<PermissionProviderProps> = ({ children }) => {
    const [permissions, setPermissions] = useState<AdminPermission[]>([]);
    const [myPermissions, setMyPermissions] = useState<AdminPermission[]>([]);
    const [rolePermissions, setRolePermissions] = useState<RolePermissionMapping>({});
    const [schema, setSchema] = useState<{ modules: string[]; actions: string[]; } | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // =====================================================
    // Permission Loading Functions
    // =====================================================

    const loadAllPermissions = async (): Promise<AdminPermission[]> => {
        try {
            const response = await adminApi.get('/permissions/all');

            // Backend returns ApiResponse<T> wrapper: { success: true, data: [...], message: "..." }
            // Extract the actual permissions array from the response.data property
            let permsArray: AdminPermission[] = [];

            if (Array.isArray(response)) {
                permsArray = response;
            } else if (response?.success && Array.isArray(response.data)) {
                permsArray = response.data;
            } else if (response?.data && Array.isArray(response.data)) {
                permsArray = response.data;
            } else {
                console.warn(' Unexpected response format for all permissions:', response);
                permsArray = [];
            }

            setPermissions(permsArray);
            return permsArray;
        } catch (err) {
            console.error(' Failed to load all permissions:', err);
            // Return empty array instead of throwing to prevent undefined
            setPermissions([]);
            return [];
        }
    };

    const loadMyPermissions = async (): Promise<AdminPermission[]> => {
        try {
            console.log(' Loading my permissions...');
            const response = await adminApi.get('/permissions/my-permissions');
            console.log(' My permissions raw response:', response);

            // Backend returns ApiResponse<T> wrapper: { success: true, data: [...], message: "..." }
            // Extract the actual permissions array from the response.data property
            let permsArray: AdminPermission[] = [];

            if (Array.isArray(response)) {
                permsArray = response;
            } else if (response?.success && Array.isArray(response.data)) {
                permsArray = response.data;
            } else if (response?.data && Array.isArray(response.data)) {
                permsArray = response.data;
            } else {
                console.warn(' Unexpected response format for my permissions:', response);
                permsArray = [];
            }

            console.log(' My permissions processed:', permsArray);
            console.log(' Permission names:', permsArray.map(p => p.name));
            setMyPermissions(permsArray);
            return permsArray;
        } catch (err) {
            console.error(' Failed to load my permissions:', err);
            // Return empty array instead of throwing to prevent undefined
            setMyPermissions([]);
            return [];
        }
    };

    const loadRolePermissions = async (): Promise<void> => {
        try {
            const response = await adminApi.get('/permissions/role-mappings');

            // Extract data from ApiResponse wrapper
            const roleMappings = response?.data || response || {};

            setRolePermissions(roleMappings);
        } catch (err) {
            console.error(' Failed to load role permissions:', err);
            throw err;
        }
    };

    const loadPermissionSchema = async (): Promise<void> => {
        try {
            const response = await adminApi.get('/permissions/schema');

            // Extract data from ApiResponse wrapper
            const schema = response?.data || response || { modules: [], actions: [] };

            setSchema(schema);
        } catch (err) {
            console.error(' Failed to load permission schema:', err);
            // Schema loading is optional, don't throw
        }
    };

    // =====================================================
    // Main Refresh Function
    // =====================================================

    const refreshPermissions = async (): Promise<void> => {
        setLoading(true);
        setError(null);

        try {
            // Call all API endpoints in parallel
            const [allPermsResult, myPermsResult] = await Promise.all([
                loadAllPermissions(),
                loadMyPermissions(),
                loadRolePermissions(),
                loadPermissionSchema(),
            ]);

            // Permissions loaded successfully

        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Failed to load permissions';
            setError(errorMessage);
            console.error(' PERMISSION LOADING FAILED:', errorMessage);

            // Log more details about the error
            console.error(' Error details:', err);
        } finally {
            setLoading(false);
        }
    };

    // =====================================================
    // Permission Sync Function
    // =====================================================

    const syncPermissions = async (): Promise<void> => {
        try {
            await adminApi.post('/permissions/sync');
            await refreshPermissions();
        } catch (err) {
            console.error(' PERMISSION SYNC FAILED:', err);
            throw err;
        }
    };

    // =====================================================
    // Permission Checking Functions
    // =====================================================

    const hasPermission = (permission: string): boolean => {
        console.log(` hasPermission called for: "${permission}"`);
        console.log(` Current state - loading: ${loading}, myPermissions count: ${myPermissions?.length || 0}`);

        if (!myPermissions || myPermissions.length === 0) {
            console.warn(' No permissions loaded yet, denying access to:', permission);
            return false;
        }

        console.log(` Checking permission: "${permission}"`);
        console.log(` Available permissions: [${myPermissions.map(p => p.name).join(', ')}]`);

        // Check for exact permission match
        const hasExact = myPermissions.some(p => p.name === permission);
        if (hasExact) {
            return true;
        }

        // Check for wildcard permissions
        const hasWildcard = myPermissions.some(p => {
            if (p.name === '*') return true; // Super admin wildcard
            if (p.name.endsWith(':*')) {
                const permissionModule = p.name.split(':')[0];
                return permission.startsWith(permissionModule + ':');
            }
            return false;
        });

        const result = hasExact || hasWildcard;
        console.log(`${result ? '' : ''} Permission "${permission}": ${result ? 'GRANTED' : 'DENIED'}`);
        return result;
    };

    const hasAnyPermission = (permissions: string[]): boolean => {
        return permissions.some(permission => hasPermission(permission));
    };

    const hasAllPermissions = (permissions: string[]): boolean => {
        return permissions.every(permission => hasPermission(permission));
    };

    // =====================================================
    // Initial Load Effect
    // =====================================================

    useEffect(() => {
        refreshPermissions();
    }, []);

    // =====================================================
    // Context Value
    // =====================================================

    const contextValue: PermissionContextType = {
        permissions,
        myPermissions,
        rolePermissions,
        loading,
        error,
        hasPermission,
        hasAnyPermission,
        hasAllPermissions,
        refreshPermissions,
        syncPermissions,
        schema,
    };

    return (
        <PermissionContext.Provider value={contextValue}>
            {children}
        </PermissionContext.Provider>
    );
};

// =====================================================
// Permission Hooks & Utilities
// =====================================================

// Hook for checking specific permission
export const useHasPermission = (permission: string): boolean => {
    const { hasPermission } = usePermissions();
    return hasPermission(permission);
};

// Hook for checking any of multiple permissions
export const useHasAnyPermission = (permissions: string[]): boolean => {
    const { hasAnyPermission } = usePermissions();
    return hasAnyPermission(permissions);
};

// Hook for checking all permissions
export const useHasAllPermissions = (permissions: string[]): boolean => {
    const { hasAllPermissions } = usePermissions();
    return hasAllPermissions(permissions);
};

// Permission Guard Component
interface PermissionGuardProps {
    permission?: string;
    permissions?: string[];
    requireAll?: boolean;
    children: ReactNode;
    fallback?: ReactNode;
}

export const PermissionGuard: React.FC<PermissionGuardProps> = ({
    permission,
    permissions = [],
    requireAll = false,
    children,
    fallback = null,
}) => {
    const { hasPermission, hasAnyPermission, hasAllPermissions } = usePermissions();

    let hasAccess = false;

    if (permission) {
        hasAccess = hasPermission(permission);
    } else if (permissions.length > 0) {
        hasAccess = requireAll ? hasAllPermissions(permissions) : hasAnyPermission(permissions);
    } else {
        hasAccess = true; // No permission requirements
    }

    return <>{hasAccess ? children : fallback}</>;
};

export default PermissionProvider;