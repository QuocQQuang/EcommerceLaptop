import logger from '@/lib/logger';
import { CustomerManagementDto, CustomerSearchParameters, CustomerStatisticsDto, PagedResult } from '../types/customer.types';
import { AdminApiService } from './AdminApiService';

export class CustomerManagementService {
  private apiService: AdminApiService;

  constructor(apiService: AdminApiService) {
    this.apiService = apiService;
  }

  async getCustomers(parameters: CustomerSearchParameters): Promise<PagedResult<CustomerManagementDto>> {
    logger.info('Requesting customers list', { parameters });
    try {
      const result = await this.apiService.getCustomers(parameters);
      logger.info('Received customers list', { status: 'success', items: result.items?.length, totalCount: result.totalCount });
      logger.debug('Customers payload sample', { sample: result.items?.slice(0, 3) });
      return result;
    } catch (error: any) {
      logger.error('Error fetching customers list', {
        message: error?.message,
        status: error?.response?.status,
        responseData: error?.response?.data,
        parameters
      });
      throw error;
    }
  }

  async getCustomerStatistics(): Promise<CustomerStatisticsDto> {
    logger.info('Requesting customer statistics');
    try {
      const stats = await this.apiService.getCustomerStatistics();
      logger.info('Received customer statistics', { status: 'success', statistics: stats });
      return stats;
    } catch (error: any) {
      logger.error('Error fetching customer statistics', {
        message: error?.message,
        status: error?.response?.status,
        responseData: error?.response?.data
      });
      throw error;
    }
  }

  async hasPermission(permission: string): Promise<boolean> {
    logger.debug('Checking permission', { permission });
    try {
      const ok = await this.apiService.checkPermission(permission);
      logger.debug('Permission check result', { permission, ok });
      return ok;
    } catch (error: any) {
      logger.error('Permission check failed', { permission, message: error?.message, status: error?.response?.status });
      // On error assume false
      return false;
    }
  }

  async getCustomerDetail(customerId: number): Promise<CustomerManagementDto> {
    logger.info('Requesting customer detail', { customerId });
    try {
      const result = await this.apiService.getCustomerById(customerId);
      logger.info('Received customer detail', { status: 'success', customerId });
      return result;
    } catch (error: any) {
      logger.error('Error fetching customer detail', {
        message: error?.message,
        status: error?.response?.status,
        responseData: error?.response?.data,
        customerId
      });
      throw error;
    }
  }

  async updateCustomer(customerId: number, updateData: any): Promise<any> {
    logger.info('Updating customer', { customerId, updateData });
    try {
      const result = await this.apiService.updateCustomer(customerId, updateData);
      logger.info('Customer updated successfully', { status: 'success', customerId });
      return result;
    } catch (error: any) {
      logger.error('Error updating customer', {
        message: error?.message,
        status: error?.response?.status,
        responseData: error?.response?.data,
        customerId
      });
      throw error;
    }
  }

  async deactivateCustomer(customerId: number, reason: string): Promise<any> {
    logger.info('Deactivating customer', { customerId, reason });
    try {
      const result = await this.apiService.deactivateCustomer(customerId, reason);
      logger.info('Customer deactivated successfully', { status: 'success', customerId });
      return result;
    } catch (error: any) {
      logger.error('Error deactivating customer', {
        message: error?.message,
        status: error?.response?.status,
        responseData: error?.response?.data,
        customerId
      });
      throw error;
    }
  }

  async reactivateCustomer(customerId: number, reason: string): Promise<any> {
    logger.info('Reactivating customer', { customerId, reason });
    try {
      const result = await this.apiService.reactivateCustomer(customerId, reason);
      logger.info('Customer reactivated successfully', { status: 'success', customerId });
      return result;
    } catch (error: any) {
      logger.error('Error reactivating customer', {
        message: error?.message,
        status: error?.response?.status,
        responseData: error?.response?.data,
        customerId
      });
      throw error;
    }
  }

  async getCustomerOrders(customerId: number): Promise<any> {
    logger.info('Requesting customer orders', { customerId });
    try {
      const result = await this.apiService.getCustomerOrders(customerId);
      logger.info('Received customer orders', { status: 'success', customerId });
      return result;
    } catch (error: any) {
      logger.error('Error fetching customer orders', {
        message: error?.message,
        status: error?.response?.status,
        responseData: error?.response?.data,
        customerId
      });
      throw error;
    }
  }

  async exportCustomers(exportParams: any): Promise<Blob> {
    logger.info('Exporting customers', { exportParams });
    try {
      const result = await this.apiService.exportCustomers(exportParams);
      logger.info('Customers exported successfully', { status: 'success' });
      return result;
    } catch (error: any) {
      logger.error('Error exporting customers', {
        message: error?.message,
        status: error?.response?.status,
        responseData: error?.response?.data,
        exportParams
      });
      throw error;
    }
  }

  async bulkUpdateCustomers(bulkData: any): Promise<any> {
    logger.info('Bulk updating customers', { bulkData });
    try {
      const result = await this.apiService.bulkUpdateCustomers(bulkData);
      logger.info('Bulk update completed successfully', { status: 'success' });
      return result;
    } catch (error: any) {
      logger.error('Error in bulk update', {
        message: error?.message,
        status: error?.response?.status,
        responseData: error?.response?.data,
        bulkData
      });
      throw error;
    }
  }
}
