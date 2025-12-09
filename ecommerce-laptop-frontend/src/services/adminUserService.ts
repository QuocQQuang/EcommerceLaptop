// Admin User Management Service - Complete CRUD operations for admin users

import { adminApiClient } from '@/lib/adminApi';
import {
  AdminUser,
  AdminUserListParams,
  AdminUserListResponse,
  CreateAdminUserRequest,
  UpdateAdminUserRequest,
  ApiResponse
} from '@/types/admin';

/**
 * Admin User Management Service Class
 * 
 * Provides comprehensive user management functionality:
 * - User listing with pagination, search, and filtering
 * - CRUD operations (Create, Read, Update, Delete)
 * - Role and permission management
 * - User activation/deactivation
 * - Bulk operations and user analytics
 * 
 * All operations include proper authorization checks and audit logging.
 */
export class AdminUserService {
  private readonly client = adminApiClient;

  /**
   * Get paginated list of admin users
   * 
   * Features:
   * - Server-side pagination with configurable page size
   * - Search by email, first name, or last name
   * - Role-based filtering
   * - Active/inactive status filtering
   * - Sort by multiple fields
   * 
   * @param params Query parameters for filtering and pagination
   * @returns Promise<AdminUserListResponse> Paginated user list with metadata
   */
  async getUsers(params: AdminUserListParams = {}): Promise<AdminUserListResponse> {
    const response = await this.client.get<ApiResponse<AdminUserListResponse>>(
      '/admin/users',
      { params }
    );
    
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to fetch admin users');
    }
    
    return response.data.data;
  }

  /**
   * Get single admin user by ID
   * 
   * Retrieves complete user profile including:
   * - Basic information (name, email, status)
   * - Role and permission details
   * - Account activity timestamps
   * - Security settings and preferences
   * 
   * @param userId Unique user identifier
   * @returns Promise<AdminUser> Complete user profile
   */
  async getUserById(userId: number): Promise<AdminUser> {
    const response = await this.client.get<ApiResponse<AdminUser>>(
      `/admin/users/${userId}`
    );
    
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to fetch user details');
    }
    
    return response.data.data;
  }

  /**
   * Create new admin user
   * 
   * Creates a new admin user with:
   * - Email validation and uniqueness check
   * - Password strength validation
   * - Role assignment and permission verification
   * - Account activation options
   * - Welcome email notification (optional)
   * 
   * @param userData New user information
   * @returns Promise<AdminUser> Created user profile
   */
  async createUser(userData: CreateAdminUserRequest): Promise<AdminUser> {
    const response = await this.client.post<ApiResponse<AdminUser>>(
      '/admin/users',
      userData
    );
    
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to create admin user');
    }
    
    return response.data.data;
  }

  /**
   * Update existing admin user
   * 
   * Updates user information with validation:
   * - Email uniqueness validation (if changed)
   * - Role change authorization checks
   * - Status change permissions
   * - Audit trail generation
   * - Change notifications
   * 
   * @param userId User identifier to update
   * @param userData Updated user information
   * @returns Promise<AdminUser> Updated user profile
   */
  async updateUser(userId: number, userData: UpdateAdminUserRequest): Promise<AdminUser> {
    const response = await this.client.put<ApiResponse<AdminUser>>(
      `/admin/users/${userId}`,
      userData
    );
    
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to update admin user');
    }
    
    return response.data.data;
  }

  /**
   * Delete admin user (soft delete)
   * 
   * Performs soft deletion with:
   * - Authorization validation (cannot delete self)
   * - Dependency checks (active sessions, assignments)
   * - Data anonymization options
   * - Audit trail preservation
   * - Restore capability within grace period
   * 
   * @param userId User identifier to delete
   * @returns Promise<boolean> Deletion success status
   */
  async deleteUser(userId: number): Promise<boolean> {
    const response = await this.client.delete<ApiResponse<boolean>>(
      `/admin/users/${userId}`
    );
    
    if (!response.data.success) {
      throw new Error(response.data.message || 'Failed to delete admin user');
    }
    
    return response.data.success;
  }

  /**
   * Activate/Deactivate user account
   * 
   * Changes user active status with:
   * - Session termination (for deactivation)
   * - Permission validation
   * - Notification triggers
   * - Audit logging
   * - Cascade effects handling
   * 
   * @param userId User identifier
   * @param isActive New activation status
   * @returns Promise<AdminUser> Updated user profile
   */
  async toggleUserStatus(userId: number, isActive: boolean): Promise<AdminUser> {
    const response = await this.client.patch<ApiResponse<AdminUser>>(
      `/admin/users/${userId}/status`,
      { isActive }
    );
    
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to update user status');
    }
    
    return response.data.data;
  }

  /**
   * Reset user password
   * 
   * Initiates password reset process:
   * - Generates secure reset token
   * - Sends reset email notification
   * - Logs security event
   * - Sets temporary password expiration
   * - Requires password change on next login
   * 
   * @param userId User identifier
   * @returns Promise<boolean> Reset initiation success
   */
  async resetUserPassword(userId: number): Promise<boolean> {
    const response = await this.client.post<ApiResponse<boolean>>(
      `/admin/users/${userId}/reset-password`
    );
    
    if (!response.data.success) {
      throw new Error(response.data.message || 'Failed to reset user password');
    }
    
    return response.data.success;
  }

  /**
   * Get user activity summary
   * 
   * Retrieves user activity analytics:
   * - Login history and patterns
   * - Recent actions and operations
   * - Permission usage statistics
   * - Security events and alerts
   * - Performance metrics
   * 
   * @param userId User identifier
   * @param days Number of days to analyze (default: 30)
   * @returns Promise<UserActivity> Activity summary
   */
  async getUserActivity(userId: number, days: number = 30): Promise<{
    loginCount: number;
    lastLogin: string;
    actionsPerformed: number;
    securityEvents: number;
    mostUsedFeatures: string[];
  }> {
    const response = await this.client.get<ApiResponse<{
      loginCount: number;
      lastLogin: string;
      actionsPerformed: number;
      securityEvents: number;
      mostUsedFeatures: string[];
    }>>(`/admin/users/${userId}/activity`, {
      params: { days }
    });
    
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to fetch user activity');
    }
    
    return response.data.data;
  }

  /**
   * Bulk user operations
   * 
   * Performs operations on multiple users:
   * - Bulk activation/deactivation
   * - Role assignments
   * - Permission updates
   * - Export user data
   * - Batch notifications
   * 
   * @param userIds Array of user identifiers
   * @param operation Operation to perform
   * @param data Operation-specific data
   * @returns Promise<BulkOperationResult> Operation results
   */
  async bulkOperation(userIds: number[], operation: 'activate' | 'deactivate' | 'delete' | 'updateRole', data?: any): Promise<{
    success: number;
    failed: number;
    errors: string[];
  }> {
    const response = await this.client.post<ApiResponse<{
      success: number;
      failed: number;
      errors: string[];
    }>>('/admin/users/bulk', {
      userIds,
      operation,
      data
    });
    
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to perform bulk operation');
    }
    
    return response.data.data;
  }

  /**
   * Search users with advanced filters
   * 
   * Advanced search functionality:
   * - Full-text search across multiple fields
   * - Date range filtering (created, updated, last login)
   * - Role and permission filtering
   * - Status and activity filtering
   * - Custom field searching
   * 
   * @param searchTerm Search query
   * @param filters Advanced filter options
   * @returns Promise<AdminUser[]> Matching users
   */
  async searchUsers(searchTerm: string, filters: {
    roleId?: number;
    isActive?: boolean;
    createdFrom?: string;
    createdTo?: string;
    lastLoginFrom?: string;
    lastLoginTo?: string;
  } = {}): Promise<AdminUser[]> {
    const response = await this.client.get<ApiResponse<AdminUser[]>>(
      '/admin/users/search',
      {
        params: {
          q: searchTerm,
          ...filters
        }
      }
    );
    
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Failed to search users');
    }
    
    return response.data.data;
  }
}

/**
 * Singleton instance for consistent usage across the application
 * 
 * Usage examples:
 * ```typescript
 * import { adminUserService } from '@/services/adminUserService';
 * 
 * // Get paginated users with search
 * const users = await adminUserService.getUsers({
 *   page: 1,
 *   limit: 20,
 *   search: 'john@example.com'
 * });
 * 
 * // Create new admin user
 * const newUser = await adminUserService.createUser({
 *   firstName: 'John',
 *   lastName: 'Doe', 
 *   email: 'john.doe@company.com',
 *   password: 'SecurePassword123!',
 *   roleId: 2,
 *   isActive: true
 * });
 * 
 * // Update user role
 * const updatedUser = await adminUserService.updateUser(userId, {
 *   roleId: 3,
 *   isActive: true
 * });
 * 
 * // Deactivate user
 * const deactivatedUser = await adminUserService.toggleUserStatus(userId, false);
 * ```
 */
export const adminUserService = new AdminUserService();