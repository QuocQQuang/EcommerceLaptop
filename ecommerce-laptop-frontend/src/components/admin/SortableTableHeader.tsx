'use client';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react';

export type SortDirection = 'asc' | 'desc' | null;

export interface SortConfig {
    field: string;
    direction: SortDirection;
}

interface SortableTableHeaderProps {
    field: string;
    children: React.ReactNode;
    sortConfig: SortConfig | null;
    onSort: (field: string) => void;
    className?: string;
}

export function SortableTableHeader({
    field,
    children,
    sortConfig,
    onSort,
    className
}: SortableTableHeaderProps) {
    const isActive = sortConfig?.field === field;
    const direction = isActive ? sortConfig.direction : null;

    const getSortIcon = () => {
        if (!isActive) {
            return <ArrowUpDown className="h-4 w-4 opacity-50" />;
        }
        if (direction === 'asc') {
            return <ArrowUp className="h-4 w-4" />;
        }
        if (direction === 'desc') {
            return <ArrowDown className="h-4 w-4" />;
        }
        return <ArrowUpDown className="h-4 w-4" />;
    };

    return (
        <Button
            variant="ghost"
            onClick={() => onSort(field)}
            className={cn(
                "h-auto p-0 font-semibold hover:bg-transparent",
                isActive && "text-primary",
                className
            )}
        >
            <div className="flex items-center space-x-1">
                <span>{children}</span>
                {getSortIcon()}
            </div>
        </Button>
    );
}
