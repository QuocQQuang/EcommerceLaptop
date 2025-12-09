'use client';

import {
  Avatar,
  AvatarFallback,
  AvatarImage
} from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { CurrencySelector } from '@/components/ui/currency-selector';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import { useAdminAuth } from '@/contexts/AdminAuthContext';
import {
  LogOut,
  Menu,
  Moon,
  Search,
  Settings as SettingsIcon,
  Sun
} from 'lucide-react';
import { useTheme } from 'next-themes';
import Link from 'next/link';
import React, { useState } from 'react';

interface HeaderProps {
  onMenuToggle?: () => void;
  sidebarCollapsed?: boolean;
}

export function Header({ onMenuToggle, sidebarCollapsed }: HeaderProps) {
  const { user, logout, isSuperAdmin } = useAdminAuth();
  const { theme, setTheme } = useTheme();
  const [searchQuery, setSearchQuery] = useState('');

  const initials = user?.fullName
    ? user.fullName.split(' ').slice(-2).map((n: string) => n[0]).join('').toUpperCase()
    : 'AD';

  const getRoleColor = (roleName: string) => {
    const colors = {
      SystemAdmin: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
      ProductAdmin: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
      SalesAdmin: 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200',
      ContentAdmin: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200',
      CustomerSupportAdmin: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200',
      FinanceAdmin: 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200',
      InventoryAdmin: 'bg-teal-100 text-teal-800 dark:bg-teal-900 dark:text-teal-200',
      MarketingAdmin: 'bg-pink-100 text-pink-800 dark:bg-pink-900 dark:text-pink-200',
    };
    return colors[roleName as keyof typeof colors] || 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200';
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    // TODO: Implement global search functionality
    console.log('Search query:', searchQuery);
  };

  const toggleTheme = () => {
    setTheme(theme === 'dark' ? 'light' : 'dark');
  };

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-background backdrop-blur">
      <div className="flex h-16 items-center px-4">

        {/* Mobile menu toggle */}
        <div className="flex items-center space-x-4">
          {onMenuToggle && (
            <Button
              variant="ghost"
              size="icon"
              onClick={onMenuToggle}
              className="h-9 w-9"
            >
              <Menu className="h-4 w-4" />
              <span className="sr-only">Toggle menu</span>
            </Button>
          )}

          {/* Page title / Breadcrumb */}
          <div className="hidden md:block">
            <h1 className="text-xl font-semibold">Admin Dashboard</h1>
          </div>
        </div>

        {/* Main content area */}
        <div className="flex flex-1 items-center justify-between space-x-4 md:justify-end">
          {/* Search bar */}
          <div className="w-full flex-1 md:w-auto md:flex-grow-0">
            <form onSubmit={handleSearch} className="relative">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                type="search"
                placeholder="Tm kim..."
                className="w-full max-w-sm pl-8"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
              />
            </form>
          </div>

          {/* Right side actions */}
          <div className="flex items-center space-x-2">
            {/* Currency Selector */}
            <CurrencySelector
              variant="simple"
              showRefreshButton={true}
              className="hidden sm:flex"
            />

            {/* Theme toggle */}
            <Button
              variant="ghost"
              size="icon"
              onClick={toggleTheme}
              className="h-9 w-9"
            >
              <Sun className="h-4 w-4 rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
              <Moon className="absolute h-4 w-4 rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
              <span className="sr-only">Toggle theme</span>
            </Button>

            {/* Settings */}
            {isSuperAdmin && (
              <Link href="/admin/settings">
                <Button variant="ghost" size="icon" className="h-9 w-9">
                  <SettingsIcon className="h-4 w-4" />
                  <span className="sr-only">Settings</span>
                </Button>
              </Link>
            )}

            {/* User menu */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="relative h-9 w-9 rounded-full">
                  <Avatar className="h-8 w-8">
                    <AvatarImage src="" alt={user?.fullName} />
                    <AvatarFallback className="text-sm">
                      {initials}
                    </AvatarFallback>
                  </Avatar>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="w-56" align="end" forceMount>
                <DropdownMenuLabel className="font-normal">
                  <div className="flex flex-col space-y-1">
                    <p className="text-sm font-medium leading-none">
                      {user?.fullName || 'Admin User'}
                    </p>
                    <p className="text-xs leading-none text-muted-foreground">
                      {user?.email || 'admin@example.com'}
                    </p>
                    <div className="flex flex-wrap gap-1 mt-2">
                      <Badge
                        variant="secondary"
                        className="text-xs bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200"
                      >
                        Admin
                      </Badge>
                    </div>
                  </div>
                </DropdownMenuLabel>

                <DropdownMenuSeparator />

                <DropdownMenuItem
                  onClick={logout}
                  className="text-destructive focus:text-destructive"
                >
                  <LogOut className="h-4 w-4 mr-2" />
                  ng xut
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      </div>
    </header>
  );
}