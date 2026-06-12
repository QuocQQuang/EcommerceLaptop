'use client';

import { LoadingButton } from '@/components/atoms/LoadingButton';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';
import { useUIStore } from '@/store/uiStore';
import {
  Calendar,
  Download,
  Filter,
  RefreshCw,
  TrendingDown,
  TrendingUp
} from 'lucide-react';
import React from 'react';

interface DashboardLayoutProps {
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
  filters?: React.ReactNode;
  showDefaultFilters?: boolean;
  children: React.ReactNode;
}

/**
 * Layout component optimized for dashboard/admin pages
 * Follows design rules for actionability, monitoring, and data visualization
 */
export function DashboardLayout({
  title,
  subtitle,
  actions,
  filters,
  showDefaultFilters = true,
  children
}: DashboardLayoutProps) {
  const { dashboardFilters, setDashboardFilters } = useUIStore();

  return (
    <div className="space-y-6 bg-gray-50 min-h-screen p-6">
      {/* Dashboard Header - Priority Information */}
      <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
          <div className="mb-4 sm:mb-0">
            <h1 className="text-2xl font-bold text-gray-900 mb-1">{title}</h1>
            {subtitle && (
              <p className="text-gray-600">{subtitle}</p>
            )}
            <div className="flex items-center text-sm text-gray-500 mt-2">
              <div className="w-2 h-2 bg-green-500 rounded-full mr-2 animate-pulse"></div>
              <span>Cập nhật lần cuối: {new Date().toLocaleString('vi-VN')}</span>
            </div>
          </div>

          {/* Action Buttons - Positioned for Quick Access */}
          {actions && (
            <div className="flex flex-wrap gap-3">
              {actions}
            </div>
          )}
        </div>
      </div>

      {/* Dashboard Filters */}
      {(filters || showDefaultFilters) && (
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
          <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
            <div className="flex items-center gap-2 text-sm font-medium text-gray-700">
              <Filter className="w-4 h-4" />
              <span>Bộ lọc dữ liệu</span>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              {/* Default Date Range Filter */}
              {showDefaultFilters && (
                <DateRangeFilter
                  value={dashboardFilters.dateRange}
                  onChange={(value) => setDashboardFilters({ dateRange: value })}
                />
              )}

              {/* Custom Filters */}
              {filters}

              {/* Quick Actions */}
              <div className="flex items-center gap-2">
                <LoadingButton
                  variant="outline"
                  size="sm"
                  loadingKey="dashboard-refresh"
                  onClick={() => window.location.reload()}
                >
                  <RefreshCw className="w-4 h-4" />
                </LoadingButton>

                <LoadingButton
                  variant="outline"
                  size="sm"
                  loadingKey="dashboard-export"
                >
                  <Download className="w-4 h-4" />
                </LoadingButton>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Dashboard Content */}
      <div className="space-y-6">
        {children}
      </div>
    </div>
  );
}

/**
 * Dashboard Widget Component - Modular design
 */
interface DashboardWidgetProps {
  title: string;
  value?: string | number;
  change?: string;
  changeType?: 'positive' | 'negative' | 'neutral';
  icon?: React.ReactNode;
  action?: {
    label: string;
    href: string;
    onClick?: () => void;
  };
  loading?: boolean;
  children?: React.ReactNode;
  className?: string;
}

export function DashboardWidget({
  title,
  value,
  change,
  changeType = 'neutral',
  icon,
  action,
  loading = false,
  children,
  className
}: DashboardWidgetProps) {
  return (
    <Card className={cn(
      "hover:shadow-lg transition-all duration-200",
      "border-l-4",
      changeType === 'positive' && "border-l-green-500",
      changeType === 'negative' && "border-l-red-500",
      changeType === 'neutral' && "border-l-blue-500",
      className
    )}>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-gray-600">
          {title}
        </CardTitle>
        {icon && (
          <div className={cn(
            "p-2 rounded-lg",
            changeType === 'positive' && "bg-green-50 text-green-600",
            changeType === 'negative' && "bg-red-50 text-red-600",
            changeType === 'neutral' && "bg-blue-50 text-blue-600"
          )}>
            {icon}
          </div>
        )}
      </CardHeader>

      <CardContent>
        {loading ? (
          <div className="space-y-2">
            <Skeleton className="h-8 w-24" />
            <Skeleton className="h-4 w-16" />
          </div>
        ) : (
          <>
            {/* Big Number Display */}
            {value && (
              <div className="text-2xl font-bold text-gray-900 mb-1">
                {typeof value === 'number'
                  ? value.toLocaleString('vi-VN')
                  : value
                }
              </div>
            )}

            {/* Change Indicator */}
            {change && (
              <p className={cn(
                "text-xs flex items-center font-medium",
                changeType === 'positive' && "text-green-600",
                changeType === 'negative' && "text-red-600",
                changeType === 'neutral' && "text-gray-600"
              )}>
                {changeType === 'positive' && <TrendingUp className="w-3 h-3 mr-1" />}
                {changeType === 'negative' && <TrendingDown className="w-3 h-3 mr-1" />}
                {change}
              </p>
            )}

            {/* Custom Content */}
            {children}

            {/* Action Link */}
            {action && (
              <div className="mt-4 pt-3 border-t border-gray-100">
                <Button
                  variant="ghost"
                  size="sm"
                  className="w-full justify-between text-blue-600 hover:text-blue-800"
                  onClick={action.onClick}
                >
                  <span>{action.label}</span>
                  <span></span>
                </Button>
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Date Range Filter Component
 */
interface DateRangeFilterProps {
  value: string;
  onChange: (value: string) => void;
}

function DateRangeFilter({ value, onChange }: DateRangeFilterProps) {
  const options = [
    { value: 'today', label: 'Hôm nay' },
    { value: 'yesterday', label: 'Hôm qua' },
    { value: 'week', label: '7 ngày qua' },
    { value: 'month', label: 'Tháng này' },
    { value: 'quarter', label: 'Quý này' },
    { value: 'year', label: 'Năm này' },
    { value: 'custom', label: 'Tùy chỉnh' }
  ];

  return (
    <div className="flex items-center gap-2">
      <Calendar className="w-4 h-4 text-gray-500" />
      <Select value={value} onValueChange={onChange}>
        <SelectTrigger className="w-32">
          <SelectValue placeholder="Chọn khoảng thời gian" />
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}

/**
 * Dashboard Grid Layout Component
 */
interface DashboardGridProps {
  children: React.ReactNode;
  columns?: 1 | 2 | 3 | 4;
  className?: string;
}

export function DashboardGrid({
  children,
  columns = 4,
  className
}: DashboardGridProps) {
  return (
    <div className={cn(
      "grid gap-6",
      columns === 1 && "grid-cols-1",
      columns === 2 && "grid-cols-1 md:grid-cols-2",
      columns === 3 && "grid-cols-1 md:grid-cols-2 lg:grid-cols-3",
      columns === 4 && "grid-cols-1 md:grid-cols-2 lg:grid-cols-4",
      className
    )}>
      {children}
    </div>
  );
}
