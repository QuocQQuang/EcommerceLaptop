import { ExportOptions, ExportService } from '@/services/exportService';
import { useCallback, useState } from 'react';
import { toast } from 'sonner';

export function useExport() {
    const [isLoading, setIsLoading] = useState(false);
    const [loadingType, setLoadingType] = useState<string | null>(null);

    const exportWithLoading = useCallback(async (
        exportFn: () => Promise<void>,
        successMessage: string,
        errorMessage: string = 'Có lỗi xảy ra khi xuất file',
        type?: string
    ) => {
        setIsLoading(true);
        if (type) setLoadingType(type);

        try {
            await exportFn();
            toast.success(successMessage);
        } catch (error) {
            console.error('Export error:', error);
            toast.error(errorMessage);
        } finally {
            setIsLoading(false);
            if (type) setLoadingType(null);
        }
    }, []);

    const exportOrders = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadOrdersExcel(options),
            'Đã xuất danh sách đơn hàng thành công',
            'Có lỗi xảy ra khi xuất danh sách đơn hàng',
            'orders'
        );
    }, [exportWithLoading]);

    const exportProducts = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadProductsExcel(options),
            'Đã xuất danh sách sản phẩm thành công',
            'Có lỗi xảy ra khi xuất danh sách sản phẩm',
            'products'
        );
    }, [exportWithLoading]);

    const exportUsers = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadUsersExcel(options),
            'Đã xuất danh sách khách hàng thành công',
            'Có lỗi xảy ra khi xuất danh sách khách hàng',
            'users'
        );
    }, [exportWithLoading]);

    const exportRevenueReport = useCallback(async (startDate: string, endDate: string) => {
        if (!startDate || !endDate) {
            toast.error('Vui lòng chọn ngày bắt đầu và kết thúc');
            return;
        }

        if (new Date(startDate) > new Date(endDate)) {
            toast.error('Ngày bắt đầu không được lớn hơn ngày kết thúc');
            return;
        }

        await exportWithLoading(
            () => ExportService.downloadRevenueReportExcel(startDate, endDate),
            'Đã xuất báo cáo doanh thu thành công',
            'Có lỗi xảy ra khi xuất báo cáo doanh thu',
            'revenue'
        );
    }, [exportWithLoading]);

    const exportInventoryReport = useCallback(async () => {
        await exportWithLoading(
            () => ExportService.downloadInventoryReportExcel(),
            'Đã xuất báo cáo tồn kho thành công',
            'Có lỗi xảy ra khi xuất báo cáo tồn kho',
            'inventory'
        );
    }, [exportWithLoading]);

    const exportInvoicePdf = useCallback(async (orderId: number, orderNumber: string, includeDigitalSignature: boolean = true) => {
        await exportWithLoading(
            () => ExportService.downloadInvoicePdf(orderId, orderNumber, includeDigitalSignature),
            'Đã xuất hóa đơn PDF thành công',
            'Có lỗi xảy ra khi xuất hóa đơn PDF',
            'pdf'
        );
    }, [exportWithLoading]);

    const exportInvoiceXml = useCallback(async (orderId: number, orderNumber: string) => {
        await exportWithLoading(
            () => ExportService.downloadInvoiceXml(orderId, orderNumber),
            'Đã xuất hóa đơn XML thành công',
            'Có lỗi xảy ra khi xuất hóa đơn XML',
            'xml'
        );
    }, [exportWithLoading]);

    const exportSecurityEvents = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadSecurityEventsExcel(options),
            'Đã xuất danh sách event thành công',
            'Có lỗi xảy ra khi xuất danh sách event',
            'security-events'
        );
    }, [exportWithLoading]);

    const exportIPBlockRules = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadIPBlockRulesExcel(options),
            'Đã xuất danh sách IP Block Rules thành công',
            'Có lỗi xảy ra khi xuất danh sách IP Block Rules',
            'ip-block-rules'
        );
    }, [exportWithLoading]);

    const exportRateLimitRules = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadRateLimitRulesExcel(options),
            'Đã xuất danh sách Rate Limit Rules thành công',
            'Có lỗi xảy ra khi xuất danh sách Rate Limit Rules',
            'rate-limit-rules'
        );
    }, [exportWithLoading]);

    const exportSecurityReport = useCallback(async (startDate?: string, endDate?: string) => {
        await exportWithLoading(
            () => ExportService.downloadSecurityReportExcel(startDate, endDate),
            'Đã xuất báo cáo bảo mật tổng hợp thành công',
            'Có lỗi xảy ra khi xuất báo cáo bảo mật tổng hợp',
            'security-report'
        );
    }, [exportWithLoading]);

    return {
        isLoading,
        loadingType,
        exportOrders,
        exportProducts,
        exportUsers,
        exportRevenueReport,
        exportInventoryReport,
        exportInvoicePdf,
        exportInvoiceXml,
        exportSecurityEvents,
        exportIPBlockRules,
        exportRateLimitRules,
        exportSecurityReport,
    };
}
