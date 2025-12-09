/**
 * Utilities for admin proxy routes
 * Handles common error scenarios and response formatting
 */

export interface ProxyResponse<T = any> {
  success: boolean;
  data?: T;
  error?: string;
  status?: number;
}

/**
 * Handles backend API errors and formats response appropriately
 */
export function handleBackendError(error: any): ProxyResponse {
  console.error('Backend API error:', error);
  
  if (error.response?.status === 401) {
    return {
      success: false,
      error: 'Unauthorized - session expired',
      status: 401
    };
  }
  
  if (error.response?.status === 403) {
    return {
      success: false,
      error: 'Forbidden - insufficient permissions',
      status: 403
    };
  }
  
  return {
    success: false,
    error: error.message || 'Internal server error',
    status: error.response?.status || 500
  };
}

/**
 * Extracts and validates admin session cookie
 */
export function extractAdminSession(cookieHeader: string | null | undefined): string | null {
  if (!cookieHeader) return null;
  
  const cookies = cookieHeader.split(';').map(cookie => cookie.trim());
  const adminSessionCookie = cookies.find(cookie => cookie.startsWith('admin-session='));
  
  if (!adminSessionCookie) return null;
  
  const token = adminSessionCookie.split('=')[1];
  return token || null;
}