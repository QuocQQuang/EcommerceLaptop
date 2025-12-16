export interface EnrichedProduct {
    id: number;
    name: string;
    price: number;
    originalPrice?: number;
    thumbnailUrl: string;
    slug: string;
    inStock: boolean;
    aiReasoning?: string; // Reason why AI selected this product
}

export interface ChatMessage {
    id: string;
    role: 'user' | 'assistant' | 'system';
    content: string; // Markdown text

    // Attached rich data
    products?: EnrichedProduct[];

    // UI State
    isStreaming?: boolean;
    timestamp: Date;
}

export interface ChatRequest {
    query: string;
    contextUrl?: string; // Current page URL for context
}

export interface AuthContextType {
    user: {
        id: string;
        name: string;
        email: string;
    } | null;
    token: string | null;
}
