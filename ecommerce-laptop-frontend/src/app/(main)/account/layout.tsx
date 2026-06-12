'use client';

import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import { cn } from '@/lib/utils';
import {
    MapPin,
    MessageSquare,
    ShoppingBag,
    User
} from 'lucide-react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ReactNode } from 'react';

interface AccountLayoutProps {
    children: ReactNode;
}

const accountNavigation = [
    {
        name: 'Tổng quan',
        href: '/account',
        icon: User,
    },
    {
        name: 'Đơn hàng',
        href: '/account/orders',
        icon: ShoppingBag,
    },
    {
        name: 'Địa chỉ',
        href: '/account/addresses',
        icon: MapPin,
    },
    {
        name: 'Đánh giá',
        href: '/account/reviews',
        icon: MessageSquare,
    },
];

export default function AccountLayout({ children }: AccountLayoutProps) {
    const pathname = usePathname();

    return (
        <ProtectedRoute
            redirectTo="/auth/login"
            showLoading={true}
        >
            <div className="container mx-auto px-4 py-8">
                <div className="grid gap-8 md:grid-cols-4">
                    {/* Sidebar Navigation */}
                    <div className="md:col-span-1">
                        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-sm border p-6">
                            <h2 className="text-lg font-semibold mb-4">Tài khoản của tôi</h2>
                            <nav className="space-y-2">
                                {accountNavigation.map((item) => {
                                    const Icon = item.icon;
                                    const isActive = pathname === item.href;

                                    return (
                                        <Link
                                            key={item.href}
                                            href={item.href}
                                            className={cn(
                                                'flex items-center space-x-3 px-3 py-2 rounded-md text-sm font-medium transition-colors',
                                                isActive
                                                    ? 'bg-blue-50 dark:bg-blue-900/50 text-blue-700 dark:text-blue-200'
                                                    : 'text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
                                            )}
                                        >
                                            <Icon className={cn(
                                                'h-5 w-5',
                                                isActive
                                                    ? 'text-blue-600 dark:text-blue-400'
                                                    : 'text-gray-400'
                                            )} />
                                            <span>{item.name}</span>
                                        </Link>
                                    );
                                })}
                            </nav>
                        </div>
                    </div>

                    {/* Main Content */}
                    <div className="md:col-span-3">
                        {children}
                    </div>
                </div>
            </div>
        </ProtectedRoute>
    );
}