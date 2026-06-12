'use client';

import { Button } from '@/components/ui/button';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { ExportOptions, ExportService } from '@/services/exportService';
import {
    Ban,
    BarChart3,
    Calendar,
    Clock,
    Download,
    FileSpreadsheet,
    FileText,
    FileType,
    Loader2,
    Package,
    Shield,
    Users
} from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

interface ExportButtonsProps {
    type: 'orders' | 'products' | 'users' | 'revenue' | 'inventory' | 'security-events' | 'ip-block-rules' | 'rate-limit-rules' | 'security-report';
    options?: ExportOptions;
    startDate?: string;
    endDate?: string;
    className?: string;
}

export function ExportButtons({
    type,
    options = {},
    startDate,
    endDate,
    className
}: ExportButtonsProps) {
    const [isLoading, setIsLoading] = useState(false);

    const handleExport = async (exportType: string) => {
        setIsLoading(true);
        try {
            switch (exportType) {
                case 'orders':
                    await ExportService.downloadOrdersExcel(options);
                    toast.success('Đã xuất danh sách đơn hàng thành công');
                    break;
                case 'products':
                    await ExportService.downloadProductsExcel(options);
                    toast.success('Đã xuất danh sách sản phẩm thành công');
                    break;
                case 'users':
                    await ExportService.downloadUsersExcel(options);
                    toast.success('Đã xuất danh sách khách hàng thành công');
                    break;
                case 'revenue':
                    if (startDate && endDate) {
                        await ExportService.downloadRevenueReportExcel(startDate, endDate);
                        toast.success('Đã xuất báo cáo doanh thu thành công');
                    } else {
                        toast.error('Vui lòng chọn ngày bắt đầu và kết thúc');
                    }
                    break;
                case 'inventory':
                    await ExportService.downloadInventoryReportExcel();
                    toast.success('Đã xuất báo cáo tồn kho thành công');
                    break;
                case 'security-events':
                    await ExportService.downloadSecurityEventsExcel(options);
                    toast.success('Đã xuất danh sách event thành công');
                    break;
                case 'ip-block-rules':
                    await ExportService.downloadIPBlockRulesExcel(options);
                    toast.success('Đã xuất danh sách IP Block Rules thành công');
                    break;
                case 'rate-limit-rules':
                    await ExportService.downloadRateLimitRulesExcel(options);
                    toast.success('Đã xuất danh sách Rate Limit Rules thành công');
                    break;
                case 'security-report':
                    if (startDate && endDate) {
                        await ExportService.downloadSecurityReportExcel(startDate, endDate);
                        toast.success('Đã xuất báo cáo bảo mật tổng hợp thành công');
                    } else {
                        await ExportService.downloadSecurityReportExcel();
                        toast.success('Đã xuất báo cáo bảo mật tổng hợp thành công');
                    }
                    break;
                default:
                    toast.error('Loại xuất file không hợp lệ');
            }
        } catch (error) {
            console.error('Export error:', error);
            toast.error('Có lỗi xảy ra khi xuất file');
        } finally {
            setIsLoading(false);
        }
    };

    const getButtonContent = () => {
        const iconMap = {
            orders: <FileSpreadsheet className="h-4 w-4" />,
            products: <Package className="h-4 w-4" />,
            users: <Users className="h-4 w-4" />,
            revenue: <BarChart3 className="h-4 w-4" />,
            inventory: <Package className="h-4 w-4" />,
            'security-events': <Shield className="h-4 w-4" />,
            'ip-block-rules': <Ban className="h-4 w-4" />,
            'rate-limit-rules': <Clock className="h-4 w-4" />,
            'security-report': <Shield className="h-4 w-4" />
        };

        const textMap = {
            orders: 'Xuất đơn hàng',
            products: 'Xuất sản phẩm',
            users: 'Xuất danh sách admin',
            revenue: 'Xuất báo cáo doanh thu',
            inventory: 'Xuất báo cáo tồn kho',
            'security-events': 'Xuất event',
            'ip-block-rules': 'Tải IP Block Rules',
            'rate-limit-rules': 'Tải Rate Limit Rules',
            'security-report': 'Tải báo cáo'
        };

        return (
            <>
                {isLoading ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                    iconMap[type]
                )}
                <span className="ml-2">{textMap[type]}</span>
            </>
        );
    };

    if ((type === 'revenue' || type === 'security-report') && (!startDate || !endDate)) {
        return (
            <Button
                disabled
                variant="outline"
                className={className}
                title="Vui lòng chọn ngày bắt đầu và kết thúc"
            >
                {getButtonContent()}
            </Button>
        );
    }

    return (
        <Button
            onClick={() => handleExport(type)}
            disabled={isLoading}
            variant="outline"
            className={className}
        >
            {getButtonContent()}
        </Button>
    );
}

interface InvoiceExportButtonsProps {
    orderId: number;
    orderNumber: string;
    className?: string;
}

export function InvoiceExportButtons({ orderId, orderNumber, className }: InvoiceExportButtonsProps) {
    const [isLoading, setIsLoading] = useState(false);
    const [loadingType, setLoadingType] = useState<string | null>(null);

    const handleExport = async (type: 'pdf' | 'xml', includeDigitalSignature: boolean = true) => {
        setIsLoading(true);
        setLoadingType(type);
        try {
            if (type === 'pdf') {
                await ExportService.downloadInvoicePdf(orderId, orderNumber, includeDigitalSignature);
                toast.success('Đã xuất hóa đơn PDF thành công');
            } else {
                await ExportService.downloadInvoiceXml(orderId, orderNumber);
                toast.success('Đã xuất hóa đơn XML thành công');
            }
        } catch (error: any) {
            console.error('Export error:', error);

            // Handle specific error messages from backend
            if (error.response?.status === 400 || error.response?.status === 404) {
                const errorData = error.response.data;
                const message = errorData?.detail || errorData?.message || error.message;
                if (message) {
                    toast.error('Không thể xuất hóa đơn', {
                        description: message,
                        duration: 5000
                    });
                } else {
                    toast.error('Không thể xuất hóa đơn cho đơn hàng này');
                }
            } else {
                toast.error('Có lỗi xảy ra khi xuất hóa đơn');
            }
        } finally {
            setIsLoading(false);
            setLoadingType(null);
        }
    };

    return (
        <DropdownMenu>
            <DropdownMenuTrigger asChild>
                <Button
                    disabled={isLoading}
                    variant="outline"
                    className={className}
                >
                    {isLoading ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                    ) : (
                        <Download className="h-4 w-4" />
                    )}
                    <span className="ml-2">Xuất hóa đơn</span>
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
                <DropdownMenuItem
                    onClick={() => handleExport('pdf', false)}
                    disabled={isLoading && loadingType === 'pdf'}
                >
                    <FileText className="h-4 w-4 mr-2" />
                    Xuất PDF
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                    onClick={() => handleExport('xml')}
                    disabled={isLoading && loadingType === 'xml'}
                >
                    <FileType className="h-4 w-4 mr-2" />
                    XML
                </DropdownMenuItem>
            </DropdownMenuContent>
        </DropdownMenu>
    );
}

interface DateRangeExportProps {
    onExport: (startDate: string, endDate: string) => void;
    isLoading?: boolean;
    className?: string;
}

export function DateRangeExport({ onExport, isLoading = false, className }: DateRangeExportProps) {
    const [startDate, setStartDate] = useState('');
    const [endDate, setEndDate] = useState('');

    const handleExport = () => {
        if (!startDate || !endDate) {
            toast.error('Vui lòng chọn ngày bắt đầu và kết thúc');
            return;
        }
        if (new Date(startDate) > new Date(endDate)) {
            toast.error('Ngày bắt đầu không được lớn hơn ngày kết thúc');
            return;
        }
        onExport(startDate, endDate);
    };

    return (
        <div className={`flex items-center gap-2 ${className}`}>
            <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-md text-sm"
                placeholder="Ngày bắt đầu"
            />
            <span className="text-gray-500">đến</span>
            <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-md text-sm"
                placeholder="Ngày kết thúc"
            />
            <Button
                onClick={handleExport}
                disabled={isLoading || !startDate || !endDate}
                variant="outline"
                size="sm"
            >
                {isLoading ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                    <Calendar className="h-4 w-4" />
                )}
                <span className="ml-2">Xuất báo cáo</span>
            </Button>
        </div>
    );
}
