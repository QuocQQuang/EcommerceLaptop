import { Toast } from '@/types';
import { create } from 'zustand';

interface UIState {
    // Sidebar states
    isMobileMenuOpen: boolean;
    isCartSidebarOpen: boolean;
    isFilterSidebarOpen: boolean;

    // Modal states
    isSearchModalOpen: boolean;
    isQuickViewModalOpen: boolean;
    quickViewProduct: any | null;

    // Enhanced Loading states
    isLoading: boolean;
    loadingMessage: string;
    loadingStates: Record<string, boolean>;

    // Static page specific states
    staticPageLoading: boolean;
    readingProgress: number;

    // Dashboard specific states
    dashboardWidgetLoading: Record<string, boolean>;
    dashboardFilters: {
        dateRange: string;
        refreshInterval: number;
    };

    // Toast notifications
    toasts: Toast[];

    // Theme
    theme: 'light' | 'dark' | 'system';

    // Language
    language: 'vi' | 'en';

    // Actions
    setMobileMenuOpen: (isOpen: boolean) => void;
    setCartSidebarOpen: (isOpen: boolean) => void;
    setFilterSidebarOpen: (isOpen: boolean) => void;
    setSearchModalOpen: (isOpen: boolean) => void;
    setQuickViewModal: (isOpen: boolean, product?: any) => void;

    // Enhanced loading actions
    setLoading: (keyOrBoolean: string | boolean, loading?: boolean, message?: string) => void;
    setStaticPageLoading: (loading: boolean) => void;
    setDashboardWidgetLoading: (widgetId: string, loading: boolean) => void;
    setReadingProgress: (progress: number) => void;
    setDashboardFilters: (filters: Partial<UIState['dashboardFilters']>) => void;

    addToast: (toast: Omit<Toast, 'id'>) => void;
    removeToast: (id: string) => void;
    setTheme: (theme: 'light' | 'dark' | 'system') => void;
    setLanguage: (language: 'vi' | 'en') => void;
}

export const useUIStore = create<UIState>((set, get) => ({
    // Initial states
    isMobileMenuOpen: false,
    isCartSidebarOpen: false,
    isFilterSidebarOpen: false,
    isSearchModalOpen: false,
    isQuickViewModalOpen: false,
    quickViewProduct: null,
    isLoading: false,
    loadingMessage: '',
    loadingStates: {},
    staticPageLoading: false,
    readingProgress: 0,
    dashboardWidgetLoading: {},
    dashboardFilters: {
        dateRange: 'today',
        refreshInterval: 30000, // 30 seconds
    },
    toasts: [],
    theme: 'light',
    language: 'vi',

    // Actions
    setMobileMenuOpen: (isOpen: boolean) => {
        set({ isMobileMenuOpen: isOpen });
    },

    setCartSidebarOpen: (isOpen: boolean) => {
        set({ isCartSidebarOpen: isOpen });
    },

    setFilterSidebarOpen: (isOpen: boolean) => {
        set({ isFilterSidebarOpen: isOpen });
    },

    setSearchModalOpen: (isOpen: boolean) => {
        set({ isSearchModalOpen: isOpen });
    },

    setQuickViewModal: (isOpen: boolean, product?: any) => {
        set({
            isQuickViewModalOpen: isOpen,
            quickViewProduct: product || null
        });
    },

    // Enhanced loading setter - supports both old and new API
    setLoading: (keyOrBoolean: string | boolean, loading?: boolean, message: string = '') => {
        if (typeof keyOrBoolean === 'boolean') {
            // Old API: setLoading(true, 'message')
            const loadingMessage = typeof loading === 'string' ? loading : message;
            set({ isLoading: keyOrBoolean, loadingMessage });
        } else {
            // New API: setLoading('key', true)
            set(state => ({
                loadingStates: {
                    ...state.loadingStates,
                    [keyOrBoolean]: loading || false
                }
            }));
        }
    },

    setStaticPageLoading: (loading: boolean) => {
        set({ staticPageLoading: loading });
    },

    setDashboardWidgetLoading: (widgetId: string, loading: boolean) => {
        set(state => ({
            dashboardWidgetLoading: {
                ...state.dashboardWidgetLoading,
                [widgetId]: loading
            }
        }));
    },

    setReadingProgress: (progress: number) => {
        set({ readingProgress: Math.max(0, Math.min(100, progress)) });
    },

    setDashboardFilters: (filters: Partial<UIState['dashboardFilters']>) => {
        set(state => ({
            dashboardFilters: {
                ...state.dashboardFilters,
                ...filters
            }
        }));
    },

    addToast: (toast: Omit<Toast, 'id'>) => {
        const id = Math.random().toString(36).substring(2, 9);
        const newToast: Toast = {
            ...toast,
            id,
            duration: toast.duration || 5000,
        };

        set(state => ({
            toasts: [...state.toasts, newToast]
        }));

        // Auto remove toast after duration
        setTimeout(() => {
            get().removeToast(id);
        }, newToast.duration);
    },

    removeToast: (id: string) => {
        set(state => ({
            toasts: state.toasts.filter(toast => toast.id !== id)
        }));
    },

    setTheme: (theme: 'light' | 'dark' | 'system') => {
        set({ theme });
    },

    setLanguage: (language: 'vi' | 'en') => {
        set({ language });
    },
}));