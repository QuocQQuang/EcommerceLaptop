'use client';

import { PerformanceMonitor } from '@/utils/performanceMonitor';
import { useEffect } from 'react';

export function PerformanceProvider({ children }: { children: React.ReactNode }) {
    useEffect(() => {
        const monitor = PerformanceMonitor.getInstance();

        // Initialize Web Vitals monitoring
        monitor.initWebVitals();

        // Measure initial page load metrics
        setTimeout(() => {
            monitor.measureMemoryUsage();
            monitor.measureNetworkTiming();
        }, 1000);

        // Set up periodic memory monitoring
        const memoryInterval = setInterval(() => {
            monitor.measureMemoryUsage();
        }, 30000); // Every 30 seconds

        // Clean up
        return () => {
            clearInterval(memoryInterval);
        };
    }, []);

    return <>{children}</>;
}