import { tokenManager } from '@/lib/admin-api';
import axios from 'axios';

export interface ExportOptions {
    page?: number;
    pageSize?: number;
    search?: string;
    status?: string;
    startDate?: string;
    endDate?: string;
    categoryId?: number;
    brandId?: number;
    isActive?: boolean;
    vipLevel?: number;
    // Security events specific options
    eventType?: string;
    severity?: string;
    type?: string; // For IP block rules
}

// Create admin-authenticated axios client
const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5129/api';

const adminClient = axios.create({
    baseURL: API_BASE,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Add admin token to requests
adminClient.interceptors.request.use(async (config) => {
    // Get admin token from secure token manager
    const adminToken = tokenManager.getAccessToken();
    if (adminToken) {
        config.headers.Authorization = `Bearer ${adminToken}`;
    }
    return config;
});

// Response interceptor for error handling
adminClient.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 401) {
            // Token expired or invalid, redirect to admin login
            if (typeof window !== 'undefined') {
                window.location.href = '/admin-login';
            }
        }
        return Promise.reject(error);
    }
);

export class ExportService {
    /**
     * Xuất hóa đơn PDF với chữ ký số (Admin only)
     */
    static async exportInvoicePdf(orderId: number, includeDigitalSignature: boolean = true): Promise<Blob> {
        const response = await adminClient.get(`/export/invoice/pdf/${orderId}`, {
            params: { includeDigitalSignature },
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất hóa đơn XML (Admin only)
     */
    static async exportInvoiceXml(orderId: number): Promise<Blob> {
        const response = await adminClient.get(`/export/invoice/xml/${orderId}`, {
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất hóa đơn PDF cho khách hàng (không cần quyền admin)
     */
    static async exportCustomerInvoicePdf(orderId: number, includeDigitalSignature: boolean = false): Promise<Blob> {
        const response = await adminClient.get(`/export/customer/invoice/pdf/${orderId}`, {
            params: { includeDigitalSignature },
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất hóa đơn XML cho khách hàng (không cần quyền admin)
     */
    static async exportCustomerInvoiceXml(orderId: number): Promise<Blob> {
        const response = await adminClient.get(`/export/customer/invoice/xml/${orderId}`, {
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất danh sách đơn hàng ra Excel
     */
    static async exportOrdersToExcel(options: ExportOptions = {}): Promise<Blob> {
        const response = await adminClient.get('/export/orders/excel', {
            params: options,
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất danh sách sản phẩm ra Excel
     */
    static async exportProductsToExcel(options: ExportOptions = {}): Promise<Blob> {
        const response = await adminClient.get('/export/products/excel', {
            params: options,
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất báo cáo doanh thu ra Excel
     */
    static async exportRevenueReportToExcel(startDate: string, endDate: string): Promise<Blob> {
        const response = await adminClient.get('/export/revenue/excel', {
            params: { startDate, endDate },
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất báo cáo tồn kho ra Excel
     */
    static async exportInventoryReportToExcel(): Promise<Blob> {
        const response = await adminClient.get('/export/inventory/excel', {
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất danh sách khách hàng ra Excel
     */
    static async exportUsersToExcel(options: ExportOptions = {}): Promise<Blob> {
        const response = await adminClient.get('/export/users/excel', {
            params: options,
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Tải file xuống
     */
    static downloadFile(blob: Blob, filename: string): void {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    }

    /**
     * Xuất hóa đơn PDF và tải xuống (Admin only)
     */
    static async downloadInvoicePdf(orderId: number, orderNumber: string, includeDigitalSignature: boolean = true): Promise<void> {
        try {
            const blob = await this.exportInvoicePdf(orderId, includeDigitalSignature);
            const filename = `HoaDon_${orderNumber}_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.pdf`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading invoice PDF:', error);
            throw error;
        }
    }

    /**
     * Xuất hóa đơn XML và tải xuống (Admin only)
     */
    static async downloadInvoiceXml(orderId: number, orderNumber: string): Promise<void> {
        try {
            const blob = await this.exportInvoiceXml(orderId);
            const filename = `HoaDon_${orderNumber}_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xml`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading invoice XML:', error);
            throw error;
        }
    }

    /**
     * Xuất hóa đơn PDF cho khách hàng và tải xuống
     */
    static async downloadCustomerInvoicePdf(orderId: number, orderNumber: string, includeDigitalSignature: boolean = false): Promise<void> {
        try {
            const blob = await this.exportCustomerInvoicePdf(orderId, includeDigitalSignature);
            const filename = `HoaDon_${orderNumber}_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.pdf`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading customer invoice PDF:', error);
            throw error;
        }
    }

    /**
     * Xuất hóa đơn XML cho khách hàng và tải xuống
     */
    static async downloadCustomerInvoiceXml(orderId: number, orderNumber: string): Promise<void> {
        try {
            const blob = await this.exportCustomerInvoiceXml(orderId);
            const filename = `HoaDon_${orderNumber}_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xml`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading customer invoice XML:', error);
            throw error;
        }
    }

    /**
     * Xuất danh sách đơn hàng Excel và tải xuống
     */
    static async downloadOrdersExcel(options: ExportOptions = {}): Promise<void> {
        try {
            const blob = await this.exportOrdersToExcel(options);
            const filename = `DanhSachDonHang_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading orders Excel:', error);
            throw error;
        }
    }

    /**
     * Xuất danh sách sản phẩm Excel và tải xuống
     */
    static async downloadProductsExcel(options: ExportOptions = {}): Promise<void> {
        try {
            const blob = await this.exportProductsToExcel(options);
            const filename = `DanhSachSanPham_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading products Excel:', error);
            throw error;
        }
    }

    /**
     * Xuất báo cáo doanh thu Excel và tải xuống
     */
    static async downloadRevenueReportExcel(startDate: string, endDate: string): Promise<void> {
        try {
            const blob = await this.exportRevenueReportToExcel(startDate, endDate);
            const filename = `BaoCaoDoanhThu_${startDate}_${endDate}.xlsx`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading revenue report Excel:', error);
            throw error;
        }
    }

    /**
     * Xuất báo cáo tồn kho Excel và tải xuống
     */
    static async downloadInventoryReportExcel(): Promise<void> {
        try {
            const blob = await this.exportInventoryReportToExcel();
            const filename = `BaoCaoTonKho_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading inventory report Excel:', error);
            throw error;
        }
    }

    /**
     * Xuất danh sách khách hàng Excel và tải xuống
     */
    static async downloadUsersExcel(options: ExportOptions = {}): Promise<void> {
        try {
            const blob = await this.exportUsersToExcel(options);
            const filename = `DanhSachKhachHang_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading users Excel:', error);
            throw error;
        }
    }

    /**
     * Xuất danh sách sự kiện bảo mật ra Excel
     */
    static async exportSecurityEventsToExcel(options: ExportOptions = {}): Promise<Blob> {
        const response = await adminClient.get('/export/security-events/excel', {
            params: options,
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất danh sách IP Block Rules ra Excel
     */
    static async exportIPBlockRulesToExcel(options: ExportOptions = {}): Promise<Blob> {
        const response = await adminClient.get('/export/ip-block-rules/excel', {
            params: options,
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất danh sách Rate Limit Rules ra Excel
     */
    static async exportRateLimitRulesToExcel(options: ExportOptions = {}): Promise<Blob> {
        const response = await adminClient.get('/export/rate-limit-rules/excel', {
            params: options,
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất báo cáo bảo mật tổng hợp ra Excel
     */
    static async exportSecurityReportToExcel(startDate?: string, endDate?: string): Promise<Blob> {
        const response = await adminClient.get('/export/security-report/excel', {
            params: { startDate, endDate },
            responseType: 'blob'
        });
        return response.data;
    }

    /**
     * Xuất danh sách sự kiện bảo mật Excel và tải xuống
     */
    static async downloadSecurityEventsExcel(options: ExportOptions = {}): Promise<void> {
        try {
            const blob = await this.exportSecurityEventsToExcel(options);
            const filename = `DanhSachSuKienBaoMat_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading security events Excel:', error);
            throw error;
        }
    }

    /**
     * Xuất danh sách IP Block Rules Excel và tải xuống
     */
    static async downloadIPBlockRulesExcel(options: ExportOptions = {}): Promise<void> {
        try {
            const blob = await this.exportIPBlockRulesToExcel(options);
            const filename = `DanhSachIPBlockRules_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading IP block rules Excel:', error);
            throw error;
        }
    }

    /**
     * Xuất danh sách Rate Limit Rules Excel và tải xuống
     */
    static async downloadRateLimitRulesExcel(options: ExportOptions = {}): Promise<void> {
        try {
            const blob = await this.exportRateLimitRulesToExcel(options);
            const filename = `DanhSachRateLimitRules_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading rate limit rules Excel:', error);
            throw error;
        }
    }

    /**
     * Xuất báo cáo bảo mật tổng hợp Excel và tải xuống
     */
    static async downloadSecurityReportExcel(startDate?: string, endDate?: string): Promise<void> {
        try {
            const blob = await this.exportSecurityReportToExcel(startDate, endDate);
            const filename = `BaoCaoBaoMat_${startDate || 'all'}_${endDate || 'all'}_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
            this.downloadFile(blob, filename);
        } catch (error) {
            console.error('Error downloading security report Excel:', error);
            throw error;
        }
    }
}
