// Performance monitoring utility for Core Web Vitals
export class PerformanceMonitor {
    private static instance: PerformanceMonitor;
    private metrics: Record<string, number> = {};

    static getInstance(): PerformanceMonitor {
        if (!PerformanceMonitor.instance) {
            PerformanceMonitor.instance = new PerformanceMonitor();
        }
        return PerformanceMonitor.instance;
    }

    // Measure Core Web Vitals
    initWebVitals() {
        if (typeof window !== 'undefined') {
            // Largest Contentful Paint (LCP)
            this.measureLCP();

            // First Input Delay (FID) / Interaction to Next Paint (INP)
            this.measureFID();

            // Cumulative Layout Shift (CLS)
            this.measureCLS();

            // First Contentful Paint (FCP)
            this.measureFCP();

            // Time to First Byte (TTFB)
            this.measureTTFB();
        }
    }

    private measureLCP() {
        if ('PerformanceObserver' in window) {
            const observer = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                const lastEntry = entries[entries.length - 1];
                this.metrics.lcp = lastEntry.startTime;
                this.reportMetric('LCP', lastEntry.startTime);
            });

            observer.observe({ entryTypes: ['largest-contentful-paint'] });
        }
    }

    private measureFID() {
        if ('PerformanceObserver' in window) {
            const observer = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                entries.forEach((entry) => {
                    const fidEntry = entry as any; // First Input Delay entry
                    this.metrics.fid = fidEntry.processingStart - fidEntry.startTime;
                    this.reportMetric('FID', fidEntry.processingStart - fidEntry.startTime);
                });
            });

            observer.observe({ entryTypes: ['first-input'] });
        }
    }

    private measureCLS() {
        if ('PerformanceObserver' in window) {
            let clsValue = 0;
            const observer = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                entries.forEach((entry: any) => {
                    if (!entry.hadRecentInput) {
                        clsValue += entry.value;
                        this.metrics.cls = clsValue;
                        this.reportMetric('CLS', clsValue);
                    }
                });
            });

            observer.observe({ entryTypes: ['layout-shift'] });
        }
    }

    private measureFCP() {
        if ('PerformanceObserver' in window) {
            const observer = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                entries.forEach((entry) => {
                    if (entry.name === 'first-contentful-paint') {
                        this.metrics.fcp = entry.startTime;
                        this.reportMetric('FCP', entry.startTime);
                    }
                });
            });

            observer.observe({ entryTypes: ['paint'] });
        }
    }

    private measureTTFB() {
        if ('performance' in window && 'timing' in performance) {
            const timing = performance.timing as any;
            const ttfb = timing.responseStart - timing.navigationStart;
            this.metrics.ttfb = ttfb;
            this.reportMetric('TTFB', ttfb);
        }
    }

    // Custom performance measurements
    startTiming(label: string) {
        if (typeof window !== 'undefined' && 'performance' in window) {
            performance.mark(`${label}-start`);
        }
    }

    endTiming(label: string) {
        if (typeof window !== 'undefined' && 'performance' in window) {
            performance.mark(`${label}-end`);
            performance.measure(label, `${label}-start`, `${label}-end`);

            const measures = performance.getEntriesByName(label);
            if (measures.length > 0) {
                const duration = measures[measures.length - 1].duration;
                this.metrics[label] = duration;
                this.reportMetric(label, duration);
            }
        }
    }

    // Memory usage monitoring
    measureMemoryUsage() {
        if (typeof window !== 'undefined' && 'performance' in window && 'memory' in (performance as any)) {
            const memory = (performance as any).memory;
            this.metrics.memoryUsed = memory.usedJSHeapSize / 1024 / 1024; // MB
            this.metrics.memoryTotal = memory.totalJSHeapSize / 1024 / 1024; // MB
            this.reportMetric('Memory Used (MB)', this.metrics.memoryUsed);
        }
    }

    // Network performance
    measureNetworkTiming() {
        if (typeof window !== 'undefined' && 'performance' in window) {
            const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming;

            if (navigation) {
                this.metrics.dnsLookup = navigation.domainLookupEnd - navigation.domainLookupStart;
                this.metrics.tcpConnection = navigation.connectEnd - navigation.connectStart;
                this.metrics.serverResponse = navigation.responseEnd - navigation.requestStart;
                this.metrics.domContentLoaded = navigation.domContentLoadedEventEnd - navigation.fetchStart;
                this.metrics.pageLoad = navigation.loadEventEnd - navigation.fetchStart;

                this.reportMetric('DNS Lookup', this.metrics.dnsLookup);
                this.reportMetric('TCP Connection', this.metrics.tcpConnection);
                this.reportMetric('Server Response', this.metrics.serverResponse);
                this.reportMetric('DOM Content Loaded', this.metrics.domContentLoaded);
                this.reportMetric('Page Load', this.metrics.pageLoad);
            }
        }
    }

    private reportMetric(name: string, value: number) {
        // In development, log to console
        if (process.env.NODE_ENV === 'development') {
            console.log(` Performance Metric - ${name}: ${Math.round(value * 100) / 100}ms`);
        }

        // In production, send to analytics service
        if (process.env.NODE_ENV === 'production') {
            this.sendToAnalytics(name, value);
        }
    }

    private sendToAnalytics(name: string, value: number) {
        // Send to your analytics service (Google Analytics, etc.)
        if (typeof window !== 'undefined' && 'gtag' in window) {
            (window as any).gtag('event', name, {
                custom_parameter_1: value,
                event_category: 'Performance',
            });
        }
    }

    // Get all collected metrics
    getMetrics() {
        return { ...this.metrics };
    }

    // Reset metrics
    reset() {
        this.metrics = {};
    }

    // Performance optimization recommendations
    getRecommendations() {
        const recommendations: string[] = [];

        if (this.metrics.lcp > 2500) {
            recommendations.push('LCP is slow. Consider optimizing images and critical resources.');
        }

        if (this.metrics.fid > 100) {
            recommendations.push('FID is slow. Consider reducing JavaScript execution time.');
        }

        if (this.metrics.cls > 0.1) {
            recommendations.push('CLS is poor. Consider setting dimensions for images and ads.');
        }

        if (this.metrics.fcp > 1800) {
            recommendations.push('FCP is slow. Consider optimizing critical rendering path.');
        }

        if (this.metrics.ttfb > 600) {
            recommendations.push('TTFB is slow. Consider optimizing server response time.');
        }

        return recommendations;
    }
}

// Hook for using performance monitoring in React components
export function usePerformanceMonitor() {
    const monitor = PerformanceMonitor.getInstance();

    const measureComponentRender = (componentName: string) => {
        return {
            start: () => monitor.startTiming(`${componentName}-render`),
            end: () => monitor.endTiming(`${componentName}-render`),
        };
    };

    return {
        monitor,
        measureComponentRender,
        getMetrics: () => monitor.getMetrics(),
        getRecommendations: () => monitor.getRecommendations(),
    };
}