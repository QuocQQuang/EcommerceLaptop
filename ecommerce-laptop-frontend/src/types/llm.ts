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
    apiKey?: string; // Only for setting, backend won't return full key
    configJson: string; // JSON string for flexible config
    isActive: boolean;
    provider?: LlmProvider;
    createdAt?: string;
    updatedAt?: string;
}

export interface LlmConfigForm {
    temperature: number;
    max_tokens: number;
    streaming: boolean;
    headers: Record<string, string>;
    [key: string]: any;
}
