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
                    toast.success(' xut danh sch n hng thnh cng');
                    break;
                case 'products':
                    await ExportService.downloadProductsExcel(options);
                    toast.success(' xut danh sch sn phm thnh cng');
                    break;
                case 'users':
                    await ExportService.downloadUsersExcel(options);
                    toast.success(' xut danh sch khch hng thnh cng');
                    break;
                case 'revenue':
                    if (startDate && endDate) {
                        await ExportService.downloadRevenueReportExcel(startDate, endDate);
                        toast.success(' xut bo co doanh thu thnh cng');
                    } else {
                        toast.error('Vui lng chn ngy bt u v kt thc');
                    }
                    break;
                case 'inventory':
                    await ExportService.downloadInventoryReportExcel();
                    toast.success(' xut bo co tn kho thnh cng');
                    break;
                case 'security-events':
                    await ExportService.downloadSecurityEventsExcel(options);
                    toast.success(' xut danh sch s kin bo mt thnh cng');
                    break;
                case 'ip-block-rules':
                    await ExportService.downloadIPBlockRulesExcel(options);
                    toast.success(' xut danh sch IP Block Rules thnh cng');
                    break;
                case 'rate-limit-rules':
                    await ExportService.downloadRateLimitRulesExcel(options);
                    toast.success(' xut danh sch Rate Limit Rules thnh cng');
                    break;
                case 'security-report':
                    if (startDate && endDate) {
                        await ExportService.downloadSecurityReportExcel(startDate, endDate);
                        toast.success(' xut bo co bo mt tng hp thnh cng');
                    } else {
                        await ExportService.downloadSecurityReportExcel();
                        toast.success(' xut bo co bo mt tng hp thnh cng');
                    }
                    break;
                default:
                    toast.error('Loi xut file khng hp l');
            }
        } catch (error) {
            console.error('Export error:', error);
            toast.error('C li xy ra khi xut file');
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
            orders: 'Xut n hng',
            products: 'Xut sn phm',
            users: 'Xut danh sch admin',
            revenue: 'Xut bo co doanh thu',
            inventory: 'Xut bo co tn kho',
            'security-events': 'Xut s kin bo mt',
            'ip-block-rules': 'Xut IP Block Rules',
            'rate-limit-rules': 'Xut Rate Limit Rules',
            'security-report': 'Xut bo co bo mt'
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
                title="Vui lng chn ngy bt u v kt thc"
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
                toast.success(' xut ha n PDF thnh cng');
            } else {
                await ExportService.downloadInvoiceXml(orderId, orderNumber);
                toast.success(' xut ha n XML thnh cng');
            }
        } catch (error: any) {
            console.error('Export error:', error);

            // Handle specific error messages from backend
            if (error.response?.status === 400) {
                const errorData = error.response.data;
                if (errorData?.message) {
                    toast.error('Khng th xut ha n', {
                        description: errorData.message,
                        duration: 5000
                    });
                } else {
                    toast.error('Khng th xut ha n cho n hng ny');
                }
            } else {
                toast.error('C li xy ra khi xut ha n');
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
                    <span className="ml-2">Xut ha n</span>
                </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
                <DropdownMenuItem
                    onClick={() => handleExport('pdf', false)}
                    disabled={isLoading && loadingType === 'pdf'}
                >
                    <FileText className="h-4 w-4 mr-2" />
                    Xut PDF
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
            toast.error('Vui lng chn ngy bt u v kt thc');
            return;
        }
        if (new Date(startDate) > new Date(endDate)) {
            toast.error('Ngy bt u khng c ln hn ngy kt thc');
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
                placeholder="Ngy bt u"
            />
            <span className="text-gray-500">n</span>
            <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-md text-sm"
                placeholder="Ngy kt thc"
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
                <span className="ml-2">Xut bo co</span>
            </Button>
        </div>
    );
}
