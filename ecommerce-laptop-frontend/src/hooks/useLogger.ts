import logger from '@/lib/logger';

export function useLogger(context: string) {
    return {
        debug: (message: string, data?: any) => logger.debug(`[${context}] ${message}`, data),
        info: (message: string, data?: any) => logger.info(`[${context}] ${message}`, data),
        warn: (message: string, data?: any) => logger.warn(`[${context}] ${message}`, data),
        error: (message: string, data?: any) => logger.error(`[${context}] ${message}`, data),
    };
}