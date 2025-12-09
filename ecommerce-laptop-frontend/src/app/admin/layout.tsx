'use client';

import React, { useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AdminAuthProvider, ProtectedAdminRoute } from '@/contexts/AdminAuthContext';
import { Sidebar } from '@/components/admin/Sidebar';
import { Header } from '@/components/admin/Header';
import { Toaster } from 'sonner';

// Create a client for React Query
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: 3,
      refetchOnWindowFocus: true,
    },
  },
});

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  const handleSidebarToggle = () => {
    setSidebarCollapsed(!sidebarCollapsed);
  };

  return (
    <QueryClientProvider client={queryClient}>
      <AdminAuthProvider>
        <ProtectedAdminRoute>
          <div className="flex h-screen bg-background">
            {/* Sidebar */}
            <Sidebar 
              collapsed={sidebarCollapsed} 
              onToggle={handleSidebarToggle}
            />
            
            {/* Main content area */}
            <div className="flex flex-col flex-1 overflow-hidden">
              {/* Header */}
              <Header 
                onMenuToggle={handleSidebarToggle}
                sidebarCollapsed={sidebarCollapsed}
              />
              
              {/* Main content */}
              <main className="flex-1 overflow-auto">
                <div className="container mx-auto px-6 py-8">
                  {children}
                </div>
              </main>
            </div>
          </div>
          
          {/* Toast notifications */}
          <Toaster 
            position="top-right" 
            expand={true} 
            richColors 
            closeButton
          />
        </ProtectedAdminRoute>
      </AdminAuthProvider>
    </QueryClientProvider>
  );
}
