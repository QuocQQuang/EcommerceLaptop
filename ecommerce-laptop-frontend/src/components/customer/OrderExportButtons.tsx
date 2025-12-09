'use client';

import { Button } from '@/components/ui/button';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { ExportService } from '@/services/exportService';
import {
    Download,
    FileText,
    FileType,
    Loader2,
} from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

interface OrderExportButtonsProps {
    orderId: number;
    orderNumber: string;
    className?: string;
}

export function OrderExportButtons({ orderId, orderNumber, className }: OrderExportButtonsProps) {
    const [isLoading, setIsLoading] = useState(false);
    const [loadingType, setLoadingType] = useState<string | null>(null);

    const handleExport = async (type: 'pdf' | 'xml', includeDigitalSignature: boolean = false) => {
        setIsLoading(true);
        setLoadingType(type);
        try {
            if (type === 'pdf') {
                await ExportService.downloadCustomerInvoicePdf(orderId, orderNumber, includeDigitalSignature);
                toast.success(' xut ha n PDF thnh cng');
            } else {
                await ExportService.downloadCustomerInvoiceXml(orderId, orderNumber);
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
