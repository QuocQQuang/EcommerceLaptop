export interface LlmProvider {
    id: number;
    name: string;
    type: string; // 'openai', 'azure', 'ollama', 'custom'
    baseUrl?: string;
    website?: string;
    description?: string;
    isActive: boolean;
    createdAt?: string;
    updatedAt?: string;
    profiles?: LlmProfile[];
}

export interface LlmProfile {
    id: number;
    providerId: number;
    name: string;
    modelId: string;
    /** Server never returns this. True if a key is stored server-side. */
    hasApiKey: boolean;
    configJson: string; // JSON string for flexible config
    isActive: boolean;
    provider?: LlmProvider;
    createdAt?: string;
    updatedAt?: string;
}

/** Form shape used only when creating/editing a profile (client-side only) */
export interface LlmProfileFormData {
    providerId: number;
    name: string;
    modelId: string;
    /** Only sent to server when the user explicitly types a new key. */
    apiKey?: string;
    configJson: string;
    isActive: boolean;
}

export interface LlmConfigForm {
    temperature: number;
    max_tokens: number;
    streaming: boolean;
    headers: Record<string, string>;
    [key: string]: any;
}
