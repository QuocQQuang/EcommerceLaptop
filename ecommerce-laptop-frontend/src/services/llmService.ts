import { api } from '@/lib/admin-api';
import { LlmProvider, LlmProfile } from '@/types/llm';

export const llmService = {
    // Providers
    async getAllProviders(): Promise<LlmProvider[]> {
        const { data } = await api.get('/llm/providers');
        return data;
    },

    async getProviderById(id: number): Promise<LlmProvider> {
        const { data } = await api.get(`/llm/providers/${id}`);
        return data;
    },

    async createProvider(provider: Partial<LlmProvider>): Promise<LlmProvider> {
        const { data } = await api.post('/llm/providers', provider);
        return data;
    },

    async updateProvider(id: number, provider: Partial<LlmProvider>): Promise<void> {
        await api.put(`/llm/providers/${id}`, provider);
    },

    async deleteProvider(id: number): Promise<void> {
        await api.delete(`/llm/providers/${id}`);
    },

    // Profiles
    async getProfilesByProvider(providerId: number): Promise<LlmProfile[]> {
        const { data } = await api.get(`/llm/providers/${providerId}/profiles`);
        return data;
    },

    async getProfileById(id: number): Promise<LlmProfile> {
        const { data } = await api.get(`/llm/profiles/${id}`);
        return data;
    },

    async createProfile(profile: Partial<LlmProfile>): Promise<LlmProfile> {
        const { data } = await api.post('/llm/profiles', profile);
        return data;
    },

    async updateProfile(id: number, profile: Partial<LlmProfile>): Promise<void> {
        await api.put(`/llm/profiles/${id}`, profile);
    },

    async deleteProfile(id: number): Promise<void> {
        await api.delete(`/llm/profiles/${id}`);
    },

    // Actions
    async testConnection(profileId: number): Promise<boolean> {
        try {
            await api.post(`/llm/profiles/${profileId}/test`);
            return true;
        } catch (error) {
            return false;
        }
    },

    async activateProfile(profileId: number): Promise<void> {
        await api.post(`/llm/profiles/${profileId}/activate`);
    },

    async getActiveProfile(): Promise<{ id: number; name: string; providerId: number } | null> {
        try {
            const { data } = await api.get('/llm/profiles/active');
            return data;
        } catch (error) {
            return null;
        }
    },

    // Rewriting Profile
    async getActiveRewritingProfileId(): Promise<number | null> {
        try {
            const { data } = await api.get('/llm/rewriting-profile');
            return data.profileId;
        } catch (error) {
            return null;
        }
    },

    async setActiveRewritingProfile(profileId: number | null): Promise<void> {
        await api.post('/llm/rewriting-profile', { profileId });
    },

    // System Settings (Quick Access)
    async getSystemSetting<T>(key: string): Promise<T | null> {
        try {
            const { data } = await api.get(`/systemsettings/${key}`);
            return data ? JSON.parse(data.settingValue) : null;
            // Note: Use simple value if not JSON, but currently assuming typed usage or manual parsing
        } catch (error) {
            // Fallback to raw string if JSON parse fails or just return string
            try {
                const { data } = await api.get(`/systemsettings/${key}`);
                return (data?.settingValue as unknown as T) ?? null;
            } catch { return null; }
        }
    },

    async updateSystemSetting(key: string, value: string, category: string = 'RAG'): Promise<void> {
        await api.put(`/systemsettings/${key}`, { value, category });
    },

    // Click & Play Utilities
    async fetchRemoteModels(req: { baseUrl: string, apiKey: string, providerType: string }): Promise<string[]> {
        const { data } = await api.post('/llm/providers/models', req);
        return data;
    },

    async testChat(req: { profileId?: number, baseUrl?: string, apiKey?: string, modelId?: string, message: string }): Promise<any> {
        const { data } = await api.post('/llm/test-chat', req);
        return data;
    },

    async testEmbedding(): Promise<{ success: boolean, latencyMs: number, dimensions: number, message: string }> {
        const { data } = await api.post('/llm/test-embedding');
        return data;
    },

    async reindexVectorDb(): Promise<any> {
        const { data } = await api.post('/search/vector/reindex');
        return data;
    }
};
