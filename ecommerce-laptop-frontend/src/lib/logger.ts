// Enhanced frontend logger for both server and client side
const isDev = process.env.NODE_ENV === 'development';
const logLevel = process.env.NEXT_PUBLIC_LOG_LEVEL || 'info';

// Colors for console logging
const colors = {
    error: '\x1b[31m',   // Red
    warn: '\x1b[33m',    // Yellow
    info: '\x1b[36m',    // Cyan
    debug: '\x1b[35m',   // Magenta
    reset: '\x1b[0m'     // Reset
};

class FrontendLogger {
    private isClient = typeof window !== 'undefined';
    
    private formatMessage(level: string, message: string, meta?: any): string {
        const timestamp = new Date().toISOString();
        const color = colors[level as keyof typeof colors] || colors.reset;
        const metaStr = meta ? ` | ${JSON.stringify(meta)}` : '';
        
        if (this.isClient) {
            return `${color}[${timestamp}] [${level.toUpperCase()}] ${message}${metaStr}${colors.reset}`;
        }
        return `[${timestamp}] [${level.toUpperCase()}] ${message}${metaStr}`;
    }

    info(message: string, meta?: any) {
        if (isDev || logLevel === 'debug') {
            console.log(this.formatMessage('info', message, meta));
        }
        
        // Store in localStorage for debugging on client side
        if (this.isClient) {
            this.storeLog('info', message, meta);
        }
    }

    error(message: string, meta?: any) {
        console.error(this.formatMessage('error', message, meta));
        
        if (this.isClient) {
            this.storeLog('error', message, meta);
        }
    }

    warn(message: string, meta?: any) {
        if (isDev || logLevel === 'debug') {
            console.warn(this.formatMessage('warn', message, meta));
        }
        
        if (this.isClient) {
            this.storeLog('warn', message, meta);
        }
    }

    debug(message: string, meta?: any) {
        if (logLevel === 'debug') {
            console.debug(this.formatMessage('debug', message, meta));
        }
        
        if (this.isClient) {
            this.storeLog('debug', message, meta);
        }
    }

    // Store logs in localStorage for debugging
    private storeLog(level: string, message: string, meta?: any) {
        try {
            const logs = this.getLogs();
            const logEntry = {
                timestamp: new Date().toISOString(),
                level,
                message,
                meta,
                url: window.location.href,
                userAgent: navigator.userAgent
            };
            
            logs.push(logEntry);
            
            // Keep only last 100 logs
            if (logs.length > 100) {
                logs.splice(0, logs.length - 100);
            }
            
            localStorage.setItem('frontend_logs', JSON.stringify(logs));
        } catch (error) {
            console.error('Failed to store log:', error);
        }
    }

    // Get stored logs
    getLogs() {
        try {
            const logs = localStorage.getItem('frontend_logs');
            return logs ? JSON.parse(logs) : [];
        } catch {
            return [];
        }
    }

    // Clear stored logs
    clearLogs() {
        localStorage.removeItem('frontend_logs');
    }

    // Network request logging
    logApiRequest(method: string, url: string, data?: any, headers?: any) {
        // Reduce initialization noise - only log non-init requests
        const isInitRequest = url.includes('/init') || url.includes('/health') || url.includes('/status');
        if (isInitRequest && logLevel !== 'debug') return;
        
        this.info(` API REQUEST: ${method.toUpperCase()} ${url}`, {
            method,
            url,
            data: data ? (typeof data === 'string' ? data : JSON.stringify(data)) : undefined,
            headers,
            timestamp: Date.now()
        });
    }

    logApiResponse(method: string, url: string, status: number, data?: any, duration?: number) {
        // Reduce initialization noise - only log non-init responses or errors
        const isInitRequest = url.includes('/init') || url.includes('/health') || url.includes('/status');
        const isError = status >= 400;
        
        if (isInitRequest && !isError && logLevel !== 'debug') return;
        
        const level = status >= 400 ? 'error' : 'info';
        const emoji = status >= 400 ? '' : '';
        
        this[level](`${emoji} API RESPONSE: ${method.toUpperCase()} ${url} - ${status}`, {
            method,
            url,
            status,
            data: data ? (typeof data === 'string' ? data : JSON.stringify(data)) : undefined,
            duration: duration ? `${duration}ms` : undefined,
            timestamp: Date.now()
        });
    }

    logApiError(method: string, url: string, error: any) {
        this.error(` API ERROR: ${method.toUpperCase()} ${url}`, {
            method,
            url,
            error: error.message || error,
            status: error.response?.status,
            statusText: error.response?.statusText,
            responseData: error.response?.data,
            stack: error.stack,
            timestamp: Date.now()
        });
    }
}

const logger = new FrontendLogger();

export default logger;

// Helper functions for common log types
export const logInfo = (message: string, meta?: any) => logger.info(message, meta);
export const logError = (message: string, meta?: any) => logger.error(message, meta);
export const logWarn = (message: string, meta?: any) => logger.warn(message, meta);
export const logDebug = (message: string, meta?: any) => logger.debug(message, meta);

// Network logging helpers
export const logApiRequest = (method: string, url: string, data?: any, headers?: any) => 
    logger.logApiRequest(method, url, data, headers);

export const logApiResponse = (method: string, url: string, status: number, data?: any, duration?: number) => 
    logger.logApiResponse(method, url, status, data, duration);

export const logApiError = (method: string, url: string, error: any) => 
    logger.logApiError(method, url, error);