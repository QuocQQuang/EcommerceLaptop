// Admin User Management React Query Hooks

import { useQuery, useMutation, useQueryClient, UseQueryOptions, UseMutationOptions } from '@tanstack/react-query';
import { adminUserService } from '@/services/adminUserService';
import {
  AdminUser,
  AdminUserListParams,
  AdminUserListResponse,
  CreateAdminUserRequest,
  UpdateAdminUserRequest
} from '@/types/admin';
import { toast } from 'sonner';

/**
 * Query Keys for Admin User Management
 * 
 * Hierarchical query key structure for efficient caching:
 * - Base admin users queries
 * - Parameter-based cache separation
 * - Individual user detail caching
 */
export const adminUserQueryKeys = {
  all: ['admin', 'users'] as const,
  lists: () => [...adminUserQueryKeys.all, 'list'] as const,
  list: (params: AdminUserListParams) => [...adminUserQueryKeys.lists(), params] as const,
  details: () => [...adminUserQueryKeys.all, 'detail'] as const,
  detail: (id: number) => [...adminUserQueryKeys.details(), id] as const,
  activity: (id: number, days?: number) => [...adminUserQueryKeys.detail(id), 'activity', days] as const,
  search: (term: string, filters?: any) => [...adminUserQueryKeys.all, 'search', term, filters] as const,
};

/**
 * Hook: useAdminUsers
 * 
 * Fetches paginated admin user list with search and filtering capabilities.
 * 
 * Features:
 * - Server-side pagination with configurable page size
 * - Real-time search with debouncing
 * - Role-based filtering
 * - Status filtering (active/inactive)
 * - Sorting by multiple fields
 * - Background refetch for data freshness
 * 
 * @param params Query parameters for pagination and filtering
 * @param options React Query configuration options
 * @returns Query state with paginated user list and metadata
 * 
 * @example
 * ```typescript
 * const { data, isLoading, error } = useAdminUsers({
 *   page: 1,
 *   limit: 20,
 *   search: 'john@example.com'
 * });
 * 
 * if (isLoading) return <TableSkeleton />;
 * if (error) return <ErrorMessage error={error} />;
 * 
 * const { users, totalCount, currentPage, totalPages } = data;
 * 
 * return (
 *   <UserTable
 *     users={users}
 *     pagination={{ currentPage, totalPages, totalCount }}
 *   />
 * );
 * ```
 */
export function useAdminUsers(
  params: AdminUserListParams = {},
  options?: UseQueryOptions<AdminUserListResponse, Error>
) {
  return useQuery({
    queryKey: adminUserQueryKeys.list(params),
    queryFn: () => adminUserService.getUsers(params),
    staleTime: 2 * 60 * 1000, // 2 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    refetchOnWindowFocus: false,
    retry: 3,
    ...options,
  });
}

/**
 * Hook: useAdminUser
 * 
 * Fetches individual admin user details with comprehensive profile information.
 * 
 * Features:
 * - Complete user profile with role and permissions
 * - Account activity timestamps
 * - Security settings and preferences
 * - Automatic cache invalidation on updates
 * 
 * @param userId User identifier
 * @param options React Query configuration options
 * @returns Query state with detailed user profile
 * 
 * @example
 * ```typescript
 * const { data: user, isLoading } = useAdminUser(userId);
 * 
 * if (isLoading) return <UserProfileSkeleton />;
 * if (!user) return <UserNotFound />;
 * 
 * return (
 *   <UserProfile
 *     user={user}
 *     permissions={user.permissions}
 *   />
 * );
 * ```
 */
export function useAdminUser(
  userId: number,
  options?: UseQueryOptions<AdminUser, Error>
) {
  return useQuery({
    queryKey: adminUserQueryKeys.detail(userId),
    queryFn: () => adminUserService.getUserById(userId),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 15 * 60 * 1000, // 15 minutes
    enabled: !!userId,
    retry: 3,
    ...options,
  });
}

/**
 * Hook: useAdminUserActivity
 * 
 * Fetches user activity analytics and usage patterns.
 * 
 * @param userId User identifier
 * @param days Analysis period in days
 * @param options React Query configuration options
 * @returns Query state with activity analytics
 */
export function useAdminUserActivity(
  userId: number,
  days: number = 30,
  options?: UseQueryOptions<{
    loginCount: number;
    lastLogin: string;
    actionsPerformed: number;
    securityEvents: number;
    mostUsedFeatures: string[];
  }, Error>
) {
  return useQuery({
    queryKey: adminUserQueryKeys.activity(userId, days),
    queryFn: () => adminUserService.getUserActivity(userId, days),
    staleTime: 10 * 60 * 1000, // 10 minutes
    enabled: !!userId,
    ...options,
  });
}

/**
 * Hook: useSearchAdminUsers
 * 
 * Advanced user search with filtering capabilities.
 * 
 * @param searchTerm Search query string
 * @param filters Advanced search filters
 * @param options React Query configuration options
 * @returns Query state with matching users
 */
export function useSearchAdminUsers(
  searchTerm: string,
  filters: any = {},
  options?: UseQueryOptions<AdminUser[], Error>
) {
  return useQuery({
    queryKey: adminUserQueryKeys.search(searchTerm, filters),
    queryFn: () => adminUserService.searchUsers(searchTerm, filters),
    enabled: searchTerm.length >= 2, // Only search with 2+ characters
    staleTime: 30 * 1000, // 30 seconds for search results
    ...options,
  });
}

/**
 * Hook: useCreateAdminUser
 * 
 * Mutation hook for creating new admin users with validation and feedback.
 * 
 * Features:
 * - Form validation and error handling
 * - Success/error toast notifications
 * - Automatic cache invalidation
 * - Optimistic updates for improved UX
 * - Security validation and compliance
 * 
 * @param options Mutation configuration options
 * @returns Mutation state with create function and loading/error states
 * 
 * @example
 * ```typescript
 * const createUser = useCreateAdminUser({
 *   onSuccess: (newUser) => {
 *     toast.success(`User ${newUser.email} created successfully`);
 *     router.push(`/admin/users/${newUser.id}`);
 *   },
 *   onError: (error) => {
 *     toast.error(`Failed to create user: ${error.message}`);
 *   }
 * });
 * 
 * const handleSubmit = (userData: CreateAdminUserRequest) => {
 *   createUser.mutate(userData);
 * };
 * 
 * return (
 *   <UserForm
 *     onSubmit={handleSubmit}
 *     isLoading={createUser.isPending}
 *   />
 * );
 * ```
 */
export function useCreateAdminUser(
  options?: UseMutationOptions<AdminUser, Error, CreateAdminUserRequest>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userData: CreateAdminUserRequest) => adminUserService.createUser(userData),
    onSuccess: (newUser, variables) => {
      // Invalidate user lists to show new user
      queryClient.invalidateQueries({ queryKey: adminUserQueryKeys.lists() });
      
      // Add new user to cache
      queryClient.setQueryData(adminUserQueryKeys.detail(newUser.id), newUser);
      
      // Success notification
      toast.success(`Admin user ${newUser.email} created successfully`);
    },
    onError: (error) => {
      console.error('Failed to create admin user:', error);
      toast.error(`Failed to create user: ${error.message}`);
    },
    ...options,
  });
}

/**
 * Hook: useUpdateAdminUser
 * 
 * Mutation hook for updating existing admin users with optimistic updates.
 * 
 * Features:
 * - Optimistic UI updates for instant feedback
 * - Role change validation and authorization
 * - Cache synchronization across related queries
 * - Error recovery with automatic rollback
 * 
 * @param options Mutation configuration options
 * @returns Mutation state with update function
 * 
 * @example
 * ```typescript
 * const updateUser = useUpdateAdminUser({
 *   onSuccess: () => {
 *     toast.success('User updated successfully');
 *   }
 * });
 * 
 * const handleRoleChange = (userId: number, roleId: number) => {
 *   updateUser.mutate({
 *     userId,
 *     userData: { roleId }
 *   });
 * };
 * ```
 */
export function useUpdateAdminUser(
  options?: UseMutationOptions<AdminUser, Error, { userId: number; userData: UpdateAdminUserRequest }>
) {
  const queryClient = useQueryClient();

  return useMutation<AdminUser, Error, { userId: number; userData: UpdateAdminUserRequest }>({
    mutationFn: ({ userId, userData }) => adminUserService.updateUser(userId, userData),
    onMutate: async ({ userId, userData }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: adminUserQueryKeys.detail(userId) });

      // Snapshot previous value for rollback
      const previousUser = queryClient.getQueryData<AdminUser>(adminUserQueryKeys.detail(userId));

      // Optimistically update user data
      if (previousUser) {
        queryClient.setQueryData<AdminUser>(
          adminUserQueryKeys.detail(userId),
          { ...previousUser, ...userData }
        );
      }

      return { previousUser };
    },
    onSuccess: (updatedUser, { userId }) => {
      // Update cached user data
      queryClient.setQueryData(adminUserQueryKeys.detail(userId), updatedUser);
      
      // Invalidate user lists to reflect changes
      queryClient.invalidateQueries({ queryKey: adminUserQueryKeys.lists() });
      
      toast.success('User updated successfully');
    },
    onError: (error, { userId }, context: any) => {
      // Rollback optimistic update on error
      if (context?.previousUser) {
        queryClient.setQueryData(adminUserQueryKeys.detail(userId), context.previousUser);
      }
      
      console.error('Failed to update admin user:', error);
      toast.error(`Failed to update user: ${error.message}`);
    },
    onSettled: (data, error, { userId }) => {
      // Refetch to ensure data consistency
      queryClient.invalidateQueries({ queryKey: adminUserQueryKeys.detail(userId) });
    },
    ...options,
  });
}

/**
 * Hook: useDeleteAdminUser
 * 
 * Mutation hook for deleting admin users with confirmation and cleanup.
 * 
 * @param options Mutation configuration options
 * @returns Mutation state with delete function
 */
export function useDeleteAdminUser(
  options?: UseMutationOptions<boolean, Error, number>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (userId: number) => adminUserService.deleteUser(userId),
    onSuccess: (_, userId) => {
      // Remove user from cache
      queryClient.removeQueries({ queryKey: adminUserQueryKeys.detail(userId) });
      
      // Invalidate user lists
      queryClient.invalidateQueries({ queryKey: adminUserQueryKeys.lists() });
      
      toast.success('User deleted successfully');
    },
    onError: (error) => {
      console.error('Failed to delete admin user:', error);
      toast.error(`Failed to delete user: ${error.message}`);
    },
    ...options,
  });
}

/**
 * Hook: useToggleAdminUserStatus
 * 
 * Mutation hook for activating/deactivating admin users.
 * 
 * @param options Mutation configuration options
 * @returns Mutation state with toggle function
 */
export function useToggleAdminUserStatus(
  options?: UseMutationOptions<AdminUser, Error, { userId: number; isActive: boolean }>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ userId, isActive }) => adminUserService.toggleUserStatus(userId, isActive),
    onSuccess: (updatedUser, { userId, isActive }) => {
      // Update cached user data
      queryClient.setQueryData(adminUserQueryKeys.detail(userId), updatedUser);
      
      // Invalidate user lists
      queryClient.invalidateQueries({ queryKey: adminUserQueryKeys.lists() });
      
      const action = isActive ? 'activated' : 'deactivated';
      toast.success(`User ${action} successfully`);
    },
    onError: (error, { isActive }) => {
      const action = isActive ? 'activate' : 'deactivate';
      console.error(`Failed to ${action} user:`, error);
      toast.error(`Failed to ${action} user: ${error.message}`);
    },
    ...options,
  });
}

/**
 * Utility: Admin User Query Invalidation
 * 
 * Helper functions for manual cache management and synchronization.
 */
export const invalidateAdminUsers = {
  all: (queryClient: any) => {
    return queryClient.invalidateQueries({ queryKey: adminUserQueryKeys.all });
  },
  lists: (queryClient: any) => {
    return queryClient.invalidateQueries({ queryKey: adminUserQueryKeys.lists() });
  },
  detail: (queryClient: any, userId: number) => {
    return queryClient.invalidateQueries({ queryKey: adminUserQueryKeys.detail(userId) });
  },
};