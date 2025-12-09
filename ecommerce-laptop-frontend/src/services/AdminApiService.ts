import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { CustomerManagementDto, CustomerSearchParameters, CustomerStatisticsDto, PagedResult } from '../types/customer.types';
// Import tokenManager from existing admin-api system for consistency
import { logApiError, logApiRequest, logApiResponse } from '@/lib/logger';
import { tokenManager } from '../lib/admin-api';

export interface AdminApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  error?: string;
}

export class AdminApiService {
  private api: AxiosInstance;

  constructor() {
    this.api = axios.create({
      baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5129/api',
      timeout: 10000,
    });

    // Request interceptor for authentication - use integrated token management
    this.api.interceptors.request.use(
      async (config) => {
        // Try to get token from main tokenManager first (consistent with admin-api.ts)
        let token = tokenManager.getAccessToken();

        // If no token in tokenManager, try to fetch from HTTP-only cookie via local API
        if (!token && typeof window !== 'undefined') {
          try {
            const resp = await fetch('/api/admin/token', { credentials: 'include' });
            if (resp.ok) {
              const data = await resp.json();
              if (data?.success && data?.token) {
                token = data.token;
                // Store in main tokenManager for consistency
                tokenManager.setAccessToken(token);
              }
            }
          } catch (err) {
            console.warn('Could not fetch token from cookie:', err);
          }
        }

        if (token) {
          // Log token size for debugging HTTP 431 error
          if (process.env.NODE_ENV === 'development') {
            console.warn(` Token size: ${token.length} characters`);
            const authHeaderSize = (`Bearer ${token}`).length;
            console.warn(` Authorization header size: ${authHeaderSize} bytes`);

            // Warning for very large tokens
            if (authHeaderSize > 8000) {
              console.error(' Token too large! This may cause HTTP 431 errors');
            }
          }
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor for error handling
    this.api.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          // Clear token from main tokenManager and redirect to login
          tokenManager.clearTokens();
          window.location.href = '/admin-login';
        }
        return Promise.reject(error);
      }
    );
  }

  // Permission checking method
  async checkPermission(permission?: string): Promise<boolean> {
    try {
      const permissionToCheck = permission || 'customers:read';
      const url = `/auth/check-permission?permission=${permissionToCheck}`;
      logApiRequest('get', url, null, null);
      const response = await this.api.get(url);
      logApiResponse('get', url, response.status, response.data);
      return response.data?.data?.hasPermission || false;
    } catch (error) {
      logApiError('get', `/auth/check-permission`, error);
      return false;
    }
  }

  // Customer management methods
  async getCustomers(params: CustomerSearchParameters): Promise<PagedResult<CustomerManagementDto>> {
    const url = '/admin/customers';
    logApiRequest('get', url, null, params);
    try {
      const start = Date.now();
      const response = await this.api.get(url, { params });
      const duration = Date.now() - start;
      logApiResponse('get', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('get', url, error);
      throw error;
    }
  }

  async getCustomerById(id: number): Promise<CustomerManagementDto> {
    const url = `/admin/customers/${id}`;
    logApiRequest('get', url);
    try {
      const start = Date.now();
      const response = await this.api.get(url);
      const duration = Date.now() - start;
      logApiResponse('get', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('get', url, error);
      throw error;
    }
  }

  async getCustomerStatistics(): Promise<CustomerStatisticsDto> {
    const url = '/admin/customers/statistics';
    logApiRequest('get', url);
    try {
      const start = Date.now();
      const response = await this.api.get(url);
      const duration = Date.now() - start;
      logApiResponse('get', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('get', url, error);
      throw error;
    }
  }

  // Customer management actions
  async updateCustomer(customerId: number, updateData: any): Promise<any> {
    const url = `/admin/customers/${customerId}`;
    logApiRequest('put', url, updateData);
    try {
      const start = Date.now();
      const response = await this.api.put(url, updateData);
      const duration = Date.now() - start;
      logApiResponse('put', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('put', url, error);
      throw error;
    }
  }

  async deactivateCustomer(customerId: number, reason: string): Promise<any> {
    const url = `/admin/customers/${customerId}/deactivate`;
    const data = { reason };
    logApiRequest('post', url, data);
    try {
      const start = Date.now();
      const response = await this.api.post(url, data);
      const duration = Date.now() - start;
      logApiResponse('post', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('post', url, error);
      throw error;
    }
  }

  async reactivateCustomer(customerId: number, reason: string): Promise<any> {
    const url = `/admin/customers/${customerId}/reactivate`;
    const data = { reason };
    logApiRequest('post', url, data);
    try {
      const start = Date.now();
      const response = await this.api.post(url, data);
      const duration = Date.now() - start;
      logApiResponse('post', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('post', url, error);
      throw error;
    }
  }

  async getCustomerOrders(customerId: number): Promise<any> {
    const url = `/admin/customers/${customerId}/orders`;
    logApiRequest('get', url);
    try {
      const start = Date.now();
      const response = await this.api.get(url);
      const duration = Date.now() - start;
      logApiResponse('get', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('get', url, error);
      throw error;
    }
  }

  async getCustomerActivityLogs(customerId: number, parameters: any = {}): Promise<any> {
    const url = `/admin/customers/${customerId}/activity-logs`;
    logApiRequest('get', url, null, parameters);
    try {
      const start = Date.now();
      const response = await this.api.get(url, { params: parameters });
      const duration = Date.now() - start;
      logApiResponse('get', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('get', url, error);
      throw error;
    }
  }

  async sendCustomerNotification(customerId: number, notification: any): Promise<any> {
    const url = `/admin/customers/${customerId}/notifications`;
    logApiRequest('post', url, notification);
    try {
      const start = Date.now();
      const response = await this.api.post(url, notification);
      const duration = Date.now() - start;
      logApiResponse('post', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('post', url, error);
      throw error;
    }
  }

  async exportCustomers(exportRequest: any): Promise<Blob> {
    const url = '/admin/customers/export';
    logApiRequest('post', url, exportRequest);
    try {
      const start = Date.now();
      const response = await this.api.post(url, exportRequest, {
        responseType: 'blob'
      });
      const duration = Date.now() - start;
      logApiResponse('post', url, response.status, 'Binary data', duration);
      return response.data;
    } catch (error: any) {
      logApiError('post', url, error);
      throw error;
    }
  }

  async bulkUpdateCustomers(bulkUpdateData: any): Promise<any> {
    const url = '/admin/customers/bulk-update';
    logApiRequest('post', url, bulkUpdateData);
    try {
      const start = Date.now();
      const response = await this.api.post(url, bulkUpdateData);
      const duration = Date.now() - start;
      logApiResponse('post', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('post', url, error);
      throw error;
    }
  }

  async searchCustomers(searchRequest: any): Promise<PagedResult<CustomerManagementDto>> {
    const url = '/admin/customers/search';
    logApiRequest('post', url, searchRequest);
    try {
      const start = Date.now();
      const response = await this.api.post(url, searchRequest);
      const duration = Date.now() - start;
      logApiResponse('post', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('post', url, error);
      throw error;
    }
  }

  async getCustomerSummary(customerId: number): Promise<any> {
    const url = `/admin/customers/${customerId}/summary`;
    logApiRequest('get', url);
    try {
      const start = Date.now();
      const response = await this.api.get(url);
      const duration = Date.now() - start;
      logApiResponse('get', url, response.status, response.data, duration);
      return response.data;
    } catch (error: any) {
      logApiError('get', url, error);
      throw error;
    }
  }

  // Generic API methods
  async get<T>(url?: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.api.get(url || '/admin/customers', config);
  }

  async post<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.api.post(url, data, config);
  }

  async put<T>(url: string, data?: any, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.api.put(url, data, config);
  }

  async delete<T>(url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T>> {
    return this.api.delete(url, config);
  }

  // Public method to initialize token (for use by authentication systems)
  public initializeToken(token: string | null): void {
    tokenManager.setAccessToken(token);
  }

  // Check if authenticated using main tokenManager
  public isAuthenticated(): boolean {
    return tokenManager.isAuthenticated();
  }
}
