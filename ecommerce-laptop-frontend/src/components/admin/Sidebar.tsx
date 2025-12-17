'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { getAdminMenuItems, useAdminAuth } from '@/contexts/AdminAuthContext';
import { cn } from '@/lib/utils';
import {
  BarChart3,
  ChevronLeft,
  ChevronRight,
  FileText,
  Gift,
  LayoutDashboard,
  Lock,
  Package,
  Settings,
  Shield,
  ShoppingCart,
  Users,
  Bot
} from 'lucide-react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

const iconMap = {
  LayoutDashboard,
  Users,
  Shield,
  Lock,
  Package,
  ShoppingCart,
  Gift,
  Settings,
  FileText,
  BarChart3,
  Bot
};

interface SidebarProps {
  collapsed?: boolean;
  onToggle?: () => void;
}

export function Sidebar({ collapsed = false, onToggle }: SidebarProps) {
  const pathname = usePathname();
  const { user, isSuperAdmin } = useAdminAuth();

  const menuItems = getAdminMenuItems(user);

  const isActive = (href: string) => {
    return pathname === href || pathname?.startsWith(href + '/');
  };

  const getRoleColor = (roleName: string) => {
    const colors = {
      SystemAdmin: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
      ProductAdmin: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
      SalesAdmin: 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200',
      PromotionManager: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200',
      Customer: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
    };
    return colors[roleName as keyof typeof colors] || 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200';
  };

  return (
    <aside
      className={cn(
        "relative bg-card border-r border-border h-screen transition-all duration-300 ease-in-out",
        collapsed ? "w-16" : "w-64"
      )}
    >
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-border">
        {!collapsed && (
          <div className="flex items-center space-x-2">
            <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center">
              <LayoutDashboard className="h-5 w-5 text-primary-foreground" />
            </div>
            <div>
              <h1 className="text-lg font-semibold">Admin Panel</h1>
              <p className="text-xs text-muted-foreground">Qun tr h thng</p>
            </div>
          </div>
        )}
        {onToggle && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onToggle}
            className="h-8 w-8 p-0"
          >
            {collapsed ? (
              <ChevronRight className="h-4 w-4" />
            ) : (
              <ChevronLeft className="h-4 w-4" />
            )}
          </Button>
        )}
      </div>

      {/* User Info */}
      {user && (
        <div className={cn(
          "p-4 border-b border-border",
          collapsed && "px-2"
        )}>
          {!collapsed ? (
            <div className="space-y-2">
              <div className="flex items-center space-x-3">
                <div className="w-10 h-10 bg-primary rounded-full flex items-center justify-center">
                  <span className="text-primary-foreground font-medium">
                    {user.firstName?.[0]}{user.lastName?.[0]}
                  </span>
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">
                    {user.fullName}
                  </p>
                  <p className="text-xs text-muted-foreground truncate">
                    {user.email}
                  </p>
                </div>
              </div>
              <Badge className={cn("text-xs", getRoleColor(user.roleName))}>
                {user.roleName}
                {isSuperAdmin && " (Super Admin)"}
              </Badge>
            </div>
          ) : (
            <div className="flex justify-center">
              <div className="w-8 h-8 bg-primary rounded-full flex items-center justify-center">
                <span className="text-primary-foreground text-xs font-medium">
                  {user.firstName?.[0]}{user.lastName?.[0]}
                </span>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Navigation Menu */}
      <nav className="flex-1 p-2">
        <ul className="space-y-1">
          {menuItems.map((item) => {
            const IconComponent = iconMap[item.icon as keyof typeof iconMap];
            const active = isActive(item.href);

            return (
              <li key={item.id}>
                <Link
                  href={item.href}
                  className={cn(
                    "flex items-center space-x-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors",
                    active
                      ? "bg-primary text-primary-foreground"
                      : "text-muted-foreground hover:text-foreground hover:bg-accent",
                    collapsed && "justify-center px-2"
                  )}
                  title={collapsed ? item.label : undefined}
                >
                  {IconComponent && (
                    <IconComponent className={cn(
                      "h-5 w-5 flex-shrink-0",
                      collapsed ? "h-6 w-6" : ""
                    )} />
                  )}
                  {!collapsed && (
                    <span className="truncate">{item.label}</span>
                  )}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>

      {/* Footer */}
      <div className={cn(
        "p-4 border-t border-border",
        collapsed && "px-2"
      )}>
        {!collapsed ? (
          <div className="text-center">
            <p className="text-xs text-muted-foreground">
              EcommerceLaptop Admin v1.0
            </p>
            <p className="text-xs text-muted-foreground">
               2024 All rights reserved
            </p>
          </div>
        ) : (
          <div className="flex justify-center">
            <div className="w-2 h-2 bg-primary rounded-full"></div>
          </div>
        )}
      </div>
    </aside>
  );
}