import { ExportOptions, ExportService } from '@/services/exportService';
import { useCallback, useState } from 'react';
import { toast } from 'sonner';

export function useExport() {
    const [isLoading, setIsLoading] = useState(false);
    const [loadingType, setLoadingType] = useState<string | null>(null);

    const exportWithLoading = useCallback(async (
        exportFn: () => Promise<void>,
        successMessage: string,
        errorMessage: string = 'C li xy ra khi xut file',
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
            ' xut danh sch n hng thnh cng',
            'C li xy ra khi xut danh sch n hng',
            'orders'
        );
    }, [exportWithLoading]);

    const exportProducts = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadProductsExcel(options),
            ' xut danh sch sn phm thnh cng',
            'C li xy ra khi xut danh sch sn phm',
            'products'
        );
    }, [exportWithLoading]);

    const exportUsers = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadUsersExcel(options),
            ' xut danh sch khch hng thnh cng',
            'C li xy ra khi xut danh sch khch hng',
            'users'
        );
    }, [exportWithLoading]);

    const exportRevenueReport = useCallback(async (startDate: string, endDate: string) => {
        if (!startDate || !endDate) {
            toast.error('Vui lng chn ngy bt u v kt thc');
            return;
        }

        if (new Date(startDate) > new Date(endDate)) {
            toast.error('Ngy bt u khng c ln hn ngy kt thc');
            return;
        }

        await exportWithLoading(
            () => ExportService.downloadRevenueReportExcel(startDate, endDate),
            ' xut bo co doanh thu thnh cng',
            'C li xy ra khi xut bo co doanh thu',
            'revenue'
        );
    }, [exportWithLoading]);

    const exportInventoryReport = useCallback(async () => {
        await exportWithLoading(
            () => ExportService.downloadInventoryReportExcel(),
            ' xut bo co tn kho thnh cng',
            'C li xy ra khi xut bo co tn kho',
            'inventory'
        );
    }, [exportWithLoading]);

    const exportInvoicePdf = useCallback(async (orderId: number, orderNumber: string, includeDigitalSignature: boolean = true) => {
        await exportWithLoading(
            () => ExportService.downloadInvoicePdf(orderId, orderNumber, includeDigitalSignature),
            ' xut ha n PDF thnh cng',
            'C li xy ra khi xut ha n PDF',
            'pdf'
        );
    }, [exportWithLoading]);

    const exportInvoiceXml = useCallback(async (orderId: number, orderNumber: string) => {
        await exportWithLoading(
            () => ExportService.downloadInvoiceXml(orderId, orderNumber),
            ' xut ha n XML thnh cng',
            'C li xy ra khi xut ha n XML',
            'xml'
        );
    }, [exportWithLoading]);

    const exportSecurityEvents = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadSecurityEventsExcel(options),
            ' xut danh sch s kin bo mt thnh cng',
            'C li xy ra khi xut danh sch s kin bo mt',
            'security-events'
        );
    }, [exportWithLoading]);

    const exportIPBlockRules = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadIPBlockRulesExcel(options),
            ' xut danh sch IP Block Rules thnh cng',
            'C li xy ra khi xut danh sch IP Block Rules',
            'ip-block-rules'
        );
    }, [exportWithLoading]);

    const exportRateLimitRules = useCallback(async (options: ExportOptions = {}) => {
        await exportWithLoading(
            () => ExportService.downloadRateLimitRulesExcel(options),
            ' xut danh sch Rate Limit Rules thnh cng',
            'C li xy ra khi xut danh sch Rate Limit Rules',
            'rate-limit-rules'
        );
    }, [exportWithLoading]);

    const exportSecurityReport = useCallback(async (startDate?: string, endDate?: string) => {
        await exportWithLoading(
            () => ExportService.downloadSecurityReportExcel(startDate, endDate),
            ' xut bo co bo mt tng hp thnh cng',
            'C li xy ra khi xut bo co bo mt tng hp',
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
