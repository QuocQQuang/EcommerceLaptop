"use client";

import { useState, useEffect } from 'react';
import { llmService } from '@/services/llmService';
import { LlmProvider, LlmProfile } from '@/types/llm';
import {
    Plus, Server, Settings, Save, Trash2, Play, CheckCircle, XCircle, FileJson, Sliders, CloudDownload, MessageSquare, Loader2
} from 'lucide-react';
import { Slider } from '@/components/ui/slider';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';

export default function LlmConfigPage() {
    const [providers, setProviders] = useState<LlmProvider[]>([]);
    const [selectedProvider, setSelectedProvider] = useState<LlmProvider | null>(null);
    const [selectedProfile, setSelectedProfile] = useState<LlmProfile | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [testStatus, setTestStatus] = useState<'idle' | 'success' | 'date-error'>('idle');

    // Forms State
    const [isEditingProvider, setIsEditingProvider] = useState(false);
    const [isEditingProfile, setIsEditingProfile] = useState(false);
    const [isVisualMode, setIsVisualMode] = useState(true);
    const [formData, setFormData] = useState<any>({});

    const handleDeleteProvider = async (provider: LlmProvider) => {
        if (!confirm(`Bn c chc chn mun xa nh cung cp "${provider.name}" v tt c cu hnh lin quan?`)) return;
        try {
            await llmService.deleteProvider(provider.id);
            await loadProviders();
            if (selectedProvider?.id === provider.id) {
                setSelectedProvider(null);
                setSelectedProfile(null);
            }
        } catch (e) {
            console.error(e);
            alert("Xa tht bi");
        }
    };

    // Click & Play State
    const [fetchedModels, setFetchedModels] = useState<string[]>([]);
    const [isFetchingModels, setIsFetchingModels] = useState(false);
    const [testChatMsg, setTestChatMsg] = useState("Hello");
    const [testChatResult, setTestChatResult] = useState<any>(null);
    const [isTestingChat, setIsTestingChat] = useState(false);

    useEffect(() => {
        loadProviders();
    }, []);

    const loadProviders = async () => {
        setIsLoading(true);
        try {
            const data = await llmService.getAllProviders();
            setProviders(data);
            if (data.length > 0 && !selectedProvider) {
                // setSelectedProvider(data[0]); 
                // Don't auto-select to keep UI clean initially
            }
        } catch (error) {
            console.error("Failed to load providers", error);
        } finally {
            setIsLoading(false);
        }
    };

    const handleSelectProvider = (provider: LlmProvider) => {
        setSelectedProvider(provider);
        setSelectedProfile(null);
        setIsEditingProvider(false);
        setIsEditingProfile(false);
        setTestStatus('idle');
    };

    const handleSelectProfile = (profile: LlmProfile) => {
        setSelectedProfile(profile);
        setIsEditingProfile(false);
        setTestStatus('idle');
        setFormData({
            ...profile,
            configJson: profile.configJson || '{}'
        });
    };

    const handleNewProvider = () => {
        setSelectedProvider(null);
        setSelectedProfile(null);
        setIsEditingProvider(true);
        setIsEditingProfile(false);
        setFormData({
            name: '',
            type: 'openai',
            baseUrl: '',
            website: '',
            isActive: true
        });
    };

    const handleNewProfile = () => {
        if (!selectedProvider) return;
        setSelectedProfile(null);
        setIsEditingProfile(true);
        setFormData({
            providerId: selectedProvider.id,
            name: '',
            modelId: '',
            apiKey: '',
            configJson: '{}',
            isActive: true
        });
    };

    const handleSaveProvider = async () => {
        setIsSaving(true);
        try {
            if (selectedProvider && !isEditingProvider) {
                // Update specific logic if needed, but currently reused
            }

            if (isEditingProvider && !selectedProvider) {
                // Create
                const created = await llmService.createProvider(formData);
                setProviders([...providers, created]);
                setSelectedProvider(created);
                setIsEditingProvider(false);
            } else if (selectedProvider) {
                // Update
                // await llmService.updateProvider(selectedProvider.id, formData); // TODO: Implement update UI for provider
            }
        } catch (e) {
            console.error(e);
            alert('Lu tht bi');
        } finally {
            setIsSaving(false);
        }
    };

    const handleSaveProfile = async () => {
        setIsSaving(true);
        try {
            const profileData = { ...formData };
            if (!profileData.providerId && selectedProvider) profileData.providerId = selectedProvider.id;

            // Validate API Key
            if (profileData.apiKey === '') delete profileData.apiKey; // Don't send empty string if update

            if (selectedProfile) {
                await llmService.updateProfile(selectedProfile.id, profileData);
                // Reload providers to refresh profile list in state (simplest way)
                await loadProviders();
                // Find and re-select
                const updatedProvider = providers.find(p => p.id === selectedProvider?.id);
                if (updatedProvider) {
                    const updatedProfile = updatedProvider.profiles?.find(p => p.id === selectedProfile.id);
                    if (updatedProfile) setSelectedProfile(updatedProfile);
                }
            } else {
                const created = await llmService.createProfile(profileData);
                await loadProviders();
                setSelectedProfile(created);
            }
            setIsEditingProfile(false);
        } catch (e) {
            console.error(e);
            alert('Xa tht bi');
        } finally {
            setIsSaving(false);
        }
    };

    const handleTestConnection = async () => {
        if (!selectedProfile) return;
        setTestStatus('idle');
        const success = await llmService.testConnection(selectedProfile.id);
        setTestStatus(success ? 'success' : 'date-error');
    };

    const handleActivateProfile = async () => {
        if (!selectedProfile) return;
        await llmService.activateProfile(selectedProfile.id);
        alert(` kch hot: ${selectedProfile.name}`);
    };

    const handleDeleteProfile = async () => {
        if (!selectedProfile || !confirm('Bn c chc chn mun xa cu hnh ny?')) return;
        await llmService.deleteProfile(selectedProfile.id);
        setSelectedProfile(null);
        loadProviders();
    };

    const handleFetchModels = async () => {
        if (!formData.apiKey && !selectedProfile?.apiKey) {
            alert("Please enter API Key first");
            return;
        }
        setIsFetchingModels(true);
        try {
            const models = await llmService.fetchRemoteModels({
                baseUrl: formData.baseUrl || selectedProvider?.baseUrl || '',
                apiKey: formData.apiKey || selectedProfile?.apiKey || '',
                providerType: formData.type || selectedProvider?.type || 'openai'
            });
            setFetchedModels(models);
            // alert(`Fetched ${models.length} models`);
        } catch (e: any) {
            console.error(e);
            alert("Failed to fetch models: " + (e.response?.data?.message || e.message));
        } finally {
            setIsFetchingModels(false);
        }
    };

    const handleTestChat = async () => {
        setIsTestingChat(true);
        setTestChatResult(null);
        try {
            const res = await llmService.testChat({
                baseUrl: formData.baseUrl || selectedProvider?.baseUrl || selectedProfile?.provider?.baseUrl || '',
                apiKey: formData.apiKey || selectedProfile?.apiKey || '',
                modelId: formData.modelId || selectedProfile?.modelId || '',
                message: testChatMsg
            });
            setTestChatResult(res);
        } catch (e: any) {
            setTestChatResult({ success: false, error: e.message });
        } finally {
            setIsTestingChat(false);
        }
    };

    return (
        <div className="flex h-[calc(100vh-100px)] gap-6 p-6">
            {/* Left Sidebar: Providers List */}
            <div className="w-1/3 min-w-[300px] border rounded-xl bg-white dark:bg-gray-800 shadow-sm overflow-hidden flex flex-col">
                <div className="p-4 border-b flex justify-between items-center bg-gray-50 dark:bg-gray-900/50">
                    <h2 className="font-semibold text-lg flex items-center gap-2">
                        <Server size={20} /> Providers
                    </h2>
                    <button
                        onClick={handleNewProvider}
                        className="p-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
                    >
                        <Plus size={18} />
                    </button>
                </div>

                <div className="flex-1 overflow-y-auto p-2 space-y-2">
                    {isLoading ? (
                        <div className="p-4 text-center text-gray-500">ang ti...</div>
                    ) : providers.map(provider => (
                        <div key={provider.id} className="border rounded-lg overflow-hidden">
                            <div
                                onClick={() => handleSelectProvider(provider)}
                                className={`p-3 cursor-pointer flex justify-between items-center transition group ${selectedProvider?.id === provider.id
                                    ? 'bg-blue-50 dark:bg-blue-900/20 border-blue-200'
                                    : 'hover:bg-gray-50 dark:hover:bg-gray-700'
                                    }`}
                            >
                                <span className="font-medium">{provider.name}</span>
                                <div className="flex items-center gap-2">
                                    <span className="text-xs px-2 py-1 bg-gray-100 dark:bg-gray-700 rounded text-gray-600 dark:text-gray-300">
                                        {provider.type}
                                    </span>
                                    <button
                                        onClick={(e) => { e.stopPropagation(); handleDeleteProvider(provider); }}
                                        className="opacity-0 group-hover:opacity-100 p-1 text-gray-400 hover:text-red-500 transition"
                                        title="Xa nh cung cp"
                                    >
                                        <Trash2 size={14} />
                                    </button>
                                </div>
                            </div>

                            {/* Profiles List (nested) */}
                            {selectedProvider?.id === provider.id && (
                                <div className="bg-gray-50 dark:bg-gray-900/30 p-2 space-y-1">
                                    {provider.profiles && provider.profiles.map(profile => (
                                        <div
                                            key={profile.id}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                handleSelectProfile(profile);
                                            }}
                                            className={`pl-4 p-2 text-sm rounded cursor-pointer flex items-center gap-2 ${selectedProfile?.id === profile.id
                                                ? 'bg-white dark:bg-gray-800 shadow-sm text-blue-600 font-medium'
                                                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900'
                                                }`}
                                        >
                                            <Settings size={14} />
                                            {profile.name}
                                        </div>
                                    ))}
                                    <button
                                        onClick={(e) => { e.stopPropagation(); handleNewProfile(); }}
                                        className="w-full text-xs text-center py-2 text-blue-600 hover:underline"
                                    >
                                        + Thm Cu Hnh
                                    </button>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            </div>

            {/* Right Panel: Editor */}
            <div className="flex-1 bg-white dark:bg-gray-800 rounded-xl shadow-sm border p-6 overflow-y-auto">
                {!selectedProvider && !isEditingProvider ? (
                    <div className="h-full flex flex-col items-center justify-center text-gray-400">
                        <Server size={48} className="mb-4 opacity-50" />
                        <p>Chn nh cung cp hoc to mi</p>
                    </div>
                ) : (
                    <div className="space-y-6 max-w-3xl">
                        {/* Header */}
                        <div className="flex justify-between items-center border-b pb-4">
                            <div>
                                <h1 className="text-2xl font-bold">
                                    {isEditingProvider ? 'Nh Cung Cp Mi' : selectedProfile ? selectedProfile.name : selectedProvider?.name}
                                </h1>
                                <p className="text-gray-500 text-sm">
                                    {selectedProfile ? `Cu hnh cho ${selectedProvider?.name}` : 'Qun l Nh Cung Cp'}
                                </p>
                            </div>
                            {selectedProfile && !isEditingProfile && (
                                <div className="flex gap-2">
                                    <button
                                        onClick={handleTestConnection}
                                        className="px-4 py-2 border rounded-lg hover:bg-gray-50 flex items-center gap-2"
                                    >
                                        {testStatus === 'success' ? <CheckCircle className="text-green-500" size={18} /> :
                                            testStatus === 'date-error' ? <XCircle className="text-red-500" size={18} /> :
                                                <Play size={18} />}
                                        Test
                                    </button>
                                    <button
                                        onClick={handleActivateProfile}
                                        className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700"
                                    >
                                        Kch Hot
                                    </button>
                                    <button
                                        onClick={() => setIsEditingProfile(true)}
                                        className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
                                    >
                                        Sa
                                    </button>
                                </div>
                            )}
                        </div>

                        {/* Provider Form */}
                        {(isEditingProvider) && (
                            <div className="space-y-4">
                                <FormInput label="Provider Name" value={formData.name} onChange={(v) => setFormData({ ...formData, name: v })} placeholder="e.g. OpenRouter" />
                                <div>
                                    <label className="block text-sm font-medium mb-1">Type</label>
                                    <select
                                        className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
                                        value={formData.type}
                                        onChange={(e) => setFormData({ ...formData, type: e.target.value })}
                                    >
                                        <option value="openai">OpenAI Compatible</option>
                                        <option value="azure">Azure OpenAI</option>
                                        <option value="ollama">Ollama</option>
                                    </select>
                                </div>
                                <FormInput label="Base URL" value={formData.baseUrl} onChange={(v) => setFormData({ ...formData, baseUrl: v })} placeholder="https://api.openai.com/v1" />
                                <FormInput label="Website" value={formData.website} onChange={(v) => setFormData({ ...formData, website: v })} />

                                <div className="pt-4 flex justify-end gap-3">
                                    <button onClick={() => setIsEditingProvider(false)} className="px-4 py-2 text-gray-600">Hy</button>
                                    <button onClick={handleSaveProvider} className="px-4 py-2 bg-blue-600 text-white rounded-lg" disabled={isSaving}>
                                        {isSaving ? 'ang lu...' : 'To Nh Cung Cp'}
                                    </button>
                                </div>
                            </div>
                        )}

                        {/* Profile Editor */}
                        {(isEditingProfile || (selectedProfile && !isEditingProfile)) && (
                            <div className="space-y-6">
                                {!isEditingProfile ? (
                                    /* Read Only View */
                                    <div className="grid grid-cols-2 gap-6">
                                        <DetailItem label="Model ID" value={selectedProfile?.modelId} />
                                        <DetailItem label="API Key" value={selectedProfile?.apiKey ? '' : 'Not Set'} />
                                        <div className="col-span-2">
                                            <label className="block text-sm font-medium text-gray-500 mb-1">Configuration (JSON)</label>
                                            <pre className="bg-gray-50 dark:bg-gray-900 p-4 rounded-lg text-sm font-mono overflow-auto max-h-60 border">
                                                {selectedProfile?.configJson}
                                            </pre>
                                        </div>
                                        <div className="col-span-2 flex justify-end">
                                            <button onClick={handleDeleteProfile} className="text-red-500 hover:text-red-700 flex items-center gap-1 text-sm">
                                                <Trash2 size={16} /> Xa Cu Hnh
                                            </button>
                                        </div>
                                    </div>
                                ) : (
                                    /* Edit Form */
                                    <div className="space-y-4">
                                        <FormInput label="Profile Name" value={formData.name} onChange={(v) => setFormData({ ...formData, name: v })} placeholder="e.g. Production GPT-4" />

                                        {/* Model ID with Fetch */}
                                        <div>
                                            <div className="flex justify-between items-center mb-1">
                                                <label className="block text-sm font-medium">Model ID</label>
                                                <button
                                                    onClick={handleFetchModels}
                                                    className="text-xs flex items-center gap-1 text-blue-600 hover:underline disabled:opacity-50"
                                                    disabled={isFetchingModels}
                                                >
                                                    {isFetchingModels ? <Loader2 size={12} className="animate-spin" /> : <CloudDownload size={12} />}
                                                    Ti DS Model
                                                </button>
                                            </div>
                                            {fetchedModels.length > 0 ? (
                                                <div className="flex gap-2">
                                                    <select
                                                        className="flex-1 p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
                                                        value={formData.modelId}
                                                        onChange={(e) => setFormData({ ...formData, modelId: e.target.value })}
                                                    >
                                                        <option value="">Select a model...</option>
                                                        {fetchedModels.map(m => <option key={m} value={m}>{m}</option>)}
                                                    </select>
                                                    <button onClick={() => setFetchedModels([])} className="px-2 text-gray-400 hover:text-gray-600" title="Manual Input"><Settings size={16} /></button>
                                                </div>
                                            ) : (
                                                <FormInput
                                                    value={formData.modelId}
                                                    onChange={(v) => setFormData({ ...formData, modelId: v })}
                                                    placeholder="e.g. gpt-4o"
                                                />
                                            )}
                                        </div>

                                        <FormInput label="API Key" type="password" value={formData.apiKey} onChange={(v) => setFormData({ ...formData, apiKey: v })} placeholder="sk-..." />

                                        <div>
                                            <div className="flex items-center justify-between mb-4">
                                                <div className="flex items-center gap-2">
                                                    <Label>Configuration Mode</Label>
                                                    <div className="flex items-center gap-2 bg-gray-100 dark:bg-gray-800 p-1 rounded-lg">
                                                        <button
                                                            onClick={() => setIsVisualMode(true)}
                                                            className={`p-1.5 rounded-md transition ${isVisualMode ? 'bg-white shadow text-blue-600' : 'text-gray-500 hover:text-gray-700'}`}
                                                            title="Visual Editor"
                                                        >
                                                            <Sliders size={16} />
                                                        </button>
                                                        <button
                                                            onClick={() => setIsVisualMode(false)}
                                                            className={`p-1.5 rounded-md transition ${!isVisualMode ? 'bg-white shadow text-blue-600' : 'text-gray-500 hover:text-gray-700'}`}
                                                            title="Raw JSON"
                                                        >
                                                            <FileJson size={16} />
                                                        </button>
                                                    </div>
                                                </div>
                                                <span className="text-xs text-gray-500">
                                                    {isVisualMode ? 'Chnh sa trc quan' : 'Cu hnh JSON nng cao'}
                                                </span>
                                            </div>

                                            {isVisualMode ? (
                                                <VisualConfigEditor
                                                    jsonString={formData.configJson || '{}'}
                                                    onChange={(newJson) => setFormData({ ...formData, configJson: newJson })}
                                                />
                                            ) : (
                                                <div>
                                                    <textarea
                                                        className="w-full h-60 p-3 font-mono text-sm border rounded-lg dark:bg-gray-700 dark:border-gray-600 focus:ring-2 focus:ring-blue-500 outline-none"
                                                        value={formData.configJson}
                                                        onChange={(e) => setFormData({ ...formData, configJson: e.target.value })}
                                                    />
                                                    <p className="text-xs text-gray-500 mt-1">
                                                        Valid JSON required.
                                                    </p>
                                                </div>
                                            )}
                                        </div>

                                        <div className="pt-4 flex justify-end gap-3">
                                            <button
                                                onClick={() => {
                                                    setIsEditingProfile(false);
                                                    if (!selectedProfile) setSelectedProfile(null);
                                                }}
                                                className="px-4 py-2 text-gray-600"
                                            >
                                                Hy
                                            </button>
                                            <button onClick={handleSaveProfile} className="px-4 py-2 bg-blue-600 text-white rounded-lg flex items-center gap-2" disabled={isSaving}>
                                                <Save size={18} />
                                                {isSaving ? 'ang lu...' : 'Lu Cu Hnh'}
                                            </button>
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}

                        {/* Quick Chat Test Section */}
                        {(isEditingProfile || selectedProfile) && (
                            <div className="border-t pt-6 mt-6">
                                <h3 className="font-semibold flex items-center gap-2 mb-4">
                                    <MessageSquare size={18} /> Quick Chat Test
                                </h3>
                                <div className="flex gap-2 mb-2">
                                    <input
                                        className="flex-1 p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600"
                                        value={testChatMsg}
                                        onChange={(e) => setTestChatMsg(e.target.value)}
                                        placeholder="Type a message..."
                                    />
                                    <button
                                        onClick={handleTestChat}
                                        disabled={isTestingChat}
                                        className="bg-purple-600 text-white px-4 rounded-lg hover:bg-purple-700 disabled:opacity-50 flex items-center gap-2"
                                    >
                                        {isTestingChat ? <Loader2 size={16} className="animate-spin" /> : <Play size={16} />}
                                        Send
                                    </button>
                                </div>
                                {testChatResult && (
                                    <div className={`p-3 rounded-lg text-sm ${testChatResult.success ? 'bg-green-50 border border-green-200 text-green-900' : 'bg-red-50 border border-red-200 text-red-900'}`}>
                                        <div className="flex justify-between font-semibold mb-1">
                                            <span>{testChatResult.success ? 'Success' : 'Error'}</span>
                                            <span className="opacity-70">{testChatResult.latency}</span>
                                        </div>
                                        <div className="whitespace-pre-wrap">{testChatResult.message || testChatResult.error}</div>
                                    </div>
                                )}
                            </div>
                        )}

                        {/* Embedding Test Section */}
                        {isEditingProfile === false && (
                            <div className="border-t pt-6 mt-6">
                                <h3 className="font-semibold flex items-center gap-2 mb-4">
                                    <Sliders size={18} /> Embedding Latency Test
                                </h3>
                                <div className="flex items-center gap-4">
                                    <button
                                        onClick={async () => {
                                            setTestChatResult(null); // Reuse state slightly or creating new if needed, but for now simple alert/display
                                            try {
                                                const res = await llmService.testEmbedding();
                                                alert(`Success!\nLatency: ${res.latencyMs}ms\nDimensions: ${res.dimensions}\nMessage: ${res.message}`);
                                            } catch (e: any) {
                                                alert("Failed: " + e.message);
                                            }
                                        }}
                                        className="bg-orange-600 text-white px-4 py-2 rounded-lg hover:bg-orange-700 flex items-center gap-2"
                                    >
                                        <Play size={16} /> Test Embedding Speed
                                    </button>
                                    <p className="text-sm text-gray-500">
                                        Tests the <b>dedicated</b> embedding provider configured in <code>appsettings.json</code>.
                                    </p>
                                </div>
                            </div>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}

// Helpers
interface FormInputProps {
    label?: string;
    value: any;
    onChange: (val: string) => void;
    type?: string;
    placeholder?: string;
}

function FormInput({ label, value, onChange, type = "text", placeholder = "" }: FormInputProps) {
    return (
        <div>
            {label && <label className="block text-sm font-medium mb-1">{label}</label>}
            <input
                type={type}
                className="w-full p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 focus:ring-2 focus:ring-blue-500 outline-none"
                value={value || ''}
                onChange={(e) => onChange(e.target.value)}
                placeholder={placeholder}
            />
        </div>
    );
}

function DetailItem({ label, value }: any) {
    return (
        <div>
            <label className="block text-sm font-medium text-gray-500 mb-1">{label}</label>
            <div className="font-medium">{value}</div>
        </div>
    );
}

function VisualConfigEditor({ jsonString, onChange }: { jsonString: string, onChange: (val: string) => void }) {
    const [config, setConfig] = useState<any>({});
    const [error, setError] = useState<string | null>(null);

    // Default values
    const DEFAULTS = {
        temperature: 1.0,
        top_p: 1.0,
        frequency_penalty: 0,
        presence_penalty: 0,
        streaming: true,
        max_tokens: 4096
    };

    useEffect(() => {
        try {
            const parsed = JSON.parse(jsonString || '{ }');
            setConfig(parsed);
            setError(null);
        } catch (e) {
            setError("Invalid JSON - Switching to Visual Mode reset this. Please fix in Raw Mode.");
        }
    }, [jsonString]);

    const updateConfig = (key: string, value: any) => {
        let newConfig = { ...config };

        if (value === undefined || value === null) {
            delete newConfig[key];
        } else {
            newConfig[key] = value;
        }

        // Auto-cleanup sparse defaults if desired, or keep explicit if user set them.
        // For now, we respect the user's explicit choices via the UI toggles/inputs.

        setConfig(newConfig);
        onChange(JSON.stringify(newConfig, null, 2));
    };

    if (error) return <div className="text-red-500 text-sm p-4 bg-red-50 rounded border border-red-200">{error}</div>;

    return (
        <div className="space-y-6 bg-gray-50 dark:bg-gray-900/50 p-4 rounded-lg border">
            {/* Temperature */}
            <div className="space-y-3">
                <div className="flex justify-between">
                    <Label className="flex items-center gap-2">
                        Temperature
                        {config.temperature === undefined && <span className="text-xs text-gray-400 font-normal">(Default: 1.0)</span>}
                    </Label>
                    <span className="text-sm font-mono bg-white px-2 rounded border">{config.temperature ?? 1.0}</span>
                </div>
                <Slider
                    value={[config.temperature ?? 1.0]}
                    min={0}
                    max={2}
                    step={0.1}
                    onValueChange={([val]) => updateConfig('temperature', val)}
                />
                <p className="text-xs text-gray-400">Controls randomness: 1.0 is neutral/raw.</p>
                <div className="flex justify-end">
                    <button
                        onClick={() => updateConfig('temperature', undefined)}
                        className="text-xs text-red-400 hover:text-red-500 hover:underline"
                        title="Remove this parameter from JSON"
                    >
                        Unset / Remove
                    </button>
                </div>
            </div>

            {/* Top P */}
            <div className="space-y-3">
                <div className="flex justify-between">
                    <Label className="flex items-center gap-2">
                        Top P
                        {config.top_p === undefined && <span className="text-xs text-gray-400 font-normal">(Default: 1.0)</span>}
                    </Label>
                    <span className="text-sm font-mono bg-white px-2 rounded border">{config.top_p ?? 1.0}</span>
                </div>
                <Slider
                    value={[config.top_p ?? 1.0]}
                    min={0}
                    max={1}
                    step={0.05}
                    onValueChange={([val]) => updateConfig('top_p', val)}
                />
                <div className="flex justify-end mt-1">
                    <button
                        onClick={() => updateConfig('top_p', undefined)}
                        className="text-xs text-red-400 hover:text-red-500 hover:underline"
                    >
                        Unset
                    </button>
                </div>
            </div>

            {/* Frequency & Presence Penalty */}
            <div className="grid grid-cols-2 gap-6">
                <div className="space-y-3">
                    <div className="flex justify-between">
                        <Label className="text-xs">Frequency Penalty</Label>
                        <span className="text-xs font-mono">{config.frequency_penalty ?? 0}</span>
                    </div>
                    <Slider
                        value={[config.frequency_penalty ?? 0]}
                        min={-2}
                        max={2}
                        step={0.1}
                        onValueChange={([val]) => updateConfig('frequency_penalty', val)}
                    />
                </div>
                <div className="space-y-3">
                    <div className="flex justify-between">
                        <Label className="text-xs">Presence Penalty</Label>
                        <span className="text-xs font-mono">{config.presence_penalty ?? 0}</span>
                    </div>
                    <Slider
                        value={[config.presence_penalty ?? 0]}
                        min={-2}
                        max={2}
                        step={0.1}
                        onValueChange={([val]) => updateConfig('presence_penalty', val)}
                    />
                </div>
            </div>

            {/* Max Tokens */}
            <div className="space-y-3">
                <div className="flex justify-between items-center">
                    <Label className="flex items-center gap-2">
                        <input
                            type="checkbox"
                            checked={config.max_tokens !== undefined}
                            onChange={(e) => updateConfig('max_tokens', e.target.checked ? 4096 : undefined)}
                            className="rounded border-gray-300"
                        />
                        Max Tokens
                    </Label>
                    {config.max_tokens !== undefined && (
                        <span className="text-sm font-mono bg-white px-2 rounded border">{config.max_tokens}</span>
                    )}
                </div>
                {config.max_tokens !== undefined && (
                    <div className="flex gap-4 items-center">
                        <Slider
                            className="flex-1"
                            value={[config.max_tokens]}
                            min={128}
                            max={32000}
                            step={128}
                            onValueChange={([val]) => updateConfig('max_tokens', val)}
                        />
                        <input
                            type="number"
                            className="w-20 p-1 text-sm border rounded text-right"
                            value={config.max_tokens}
                            onChange={(e) => updateConfig('max_tokens', parseInt(e.target.value))}
                        />
                    </div>
                )}
                {config.max_tokens === undefined && (
                    <p className="text-xs text-gray-400">Not sent (using provider default)</p>
                )}
            </div>

            {/* Extra Params */}
            <div className="grid grid-cols-2 gap-4">
                <div>
                    <Label className="mb-2 block">Top K</Label>
                    <input
                        type="number"
                        className="w-full p-2 text-sm border rounded"
                        value={config.top_k ?? ''}
                        onChange={(e) => updateConfig('top_k', parseInt(e.target.value) || undefined)}
                        placeholder="Optional"
                    />
                </div>
                <div className="flex items-center justify-between border p-2 rounded bg-white">
                    <Label>Streaming</Label>
                    <Switch
                        checked={config.streaming === true}
                        onCheckedChange={(checked) => updateConfig('streaming', checked ? true : undefined)}
                    />
                </div>
            </div>
            <div className="pt-2 text-right">
                <button
                    onClick={() => { setConfig({}); onChange('{}'); }}
                    className="text-xs text-red-500 hover:underline"
                >
                    Clear All Config
                </button>
            </div>
        </div>
    );
}
