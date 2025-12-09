export * from './api';
// export * from './order';  // Commenting out to avoid duplicate exports - types are in api.ts

// UI/UX Types
export interface NavItem {
    title: string;
    href: string;
    description?: string;
    items?: NavItem[];
}

export interface BreadcrumbItem {
    title: string;
    href?: string;
    isCurrentPage?: boolean;
}

export interface FilterOption {
    label: string;
    value: string;
    count?: number;
}

export interface PriceRange {
    min: number;
    max: number;
}

// Component Props Types
export interface BaseComponentProps {
    className?: string;
    children?: React.ReactNode;
}

// Form Types
export interface FormField {
    name: string;
    label: string;
    type: 'text' | 'email' | 'password' | 'tel' | 'number' | 'textarea' | 'select' | 'checkbox';
    placeholder?: string;
    required?: boolean;
    options?: { label: string; value: string }[];
    validation?: {
        min?: number;
        max?: number;
        pattern?: string;
        message?: string;
    };
}

// Notification Types
export interface Toast {
    id: string;
    title: string;
    description?: string;
    type: 'success' | 'error' | 'warning' | 'info';
    duration?: number;
}

// Local Storage Types
export interface CartStorageItem {
    productId: number;
    quantity: number;
    addedAt: string;
}

export interface WishlistStorageItem {
    productId: number;
    addedAt: string;
}

// SEO Types
export interface MetaData {
    title: string;
    description: string;
    keywords?: string[];
    image?: string;
    url?: string;
    type?: 'website' | 'article' | 'product';
}