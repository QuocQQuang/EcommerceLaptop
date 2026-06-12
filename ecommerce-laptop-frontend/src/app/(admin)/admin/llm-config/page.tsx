"use client";

import { useState, useEffect } from 'react';
import { llmService } from '@/services/llmService';
import { LlmProvider, LlmProfile, LlmProfileFormData } from '@/types/llm';
import {
    Plus, Server, Settings, Save, Trash2, Play, CheckCircle, XCircle, FileJson, Sliders, CloudDownload, MessageSquare, Loader2,
    Cpu,
    Globe,
    Zap
} from 'lucide-react';
import { Slider } from '@/components/ui/slider';
import { Switch } from '@/components/ui/switch';
import { Label } from '@/components/ui/label';
import { toast } from 'sonner';
import { Card, CardContent, CardHeader, CardTitle, CardDescription, CardFooter } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Separator } from '@/components/ui/separator';

export default function LlmConfigPage() {
    const [providers, setProviders] = useState<LlmProvider[]>([]);
    const [selectedProvider, setSelectedProvider] = useState<LlmProvider | null>(null);
    const [selectedProfile, setSelectedProfile] = useState<LlmProfile | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [testStatus, setTestStatus] = useState<'idle' | 'success' | 'date-error'>('idle');
    const [activeProfileId, setActiveProfileId] = useState<number | null>(null);

    // Forms State
    const [isEditingProvider, setIsEditingProvider] = useState(false);
    const [isEditingProfile, setIsEditingProfile] = useState(false);
    const [isVisualMode, setIsVisualMode] = useState(true);
    const [formData, setFormData] = useState<LlmProfileFormData | any>({});

    // Click & Play State
    const [fetchedModels, setFetchedModels] = useState<string[]>([]);
    const [isFetchingModels, setIsFetchingModels] = useState(false);
    const [testChatMsg, setTestChatMsg] = useState("Hello");
    const [testChatResult, setTestChatResult] = useState<any>(null);
    const [isTestingChat, setIsTestingChat] = useState(false);

    // Global RAG State
    const [globalEnableRewriting, setGlobalEnableRewriting] = useState(true);
    const [globalRewritingProfileId, setGlobalRewritingProfileId] = useState<number | null>(null);
    const [globalCarouselLimit, setGlobalCarouselLimit] = useState(5);



    useEffect(() => {
        loadProviders();
    }, []);

    const loadProviders = async () => {
        setIsLoading(true);
        try {
            const data = await llmService.getAllProviders();
            setProviders(data);

            // Fetch Active Profile
            const active = await llmService.getActiveProfile();
            if (active) setActiveProfileId(active.id);

            // Fetch Global RAG Settings
            const rewritingEnabled = await llmService.getSystemSetting<boolean>('EnableQueryRewriting');
            setGlobalEnableRewriting(rewritingEnabled ?? true);

            const rewritingProfileId = await llmService.getActiveRewritingProfileId();
            setGlobalRewritingProfileId(rewritingProfileId);

            const carouselLimit = await llmService.getSystemSetting<number>('ProductCarouselLimit');
            setGlobalCarouselLimit(carouselLimit ?? 5);

            // Maintain selection if possible, else clear
            // if (data.length > 0 && !selectedProvider) { ... }
        } catch (error) {
            console.error("Failed to load providers", error);
            toast.error("Không thể tải danh sách nhà cung cấp");
        } finally {
            setIsLoading(false);
        }
    };

    const handleDeleteProvider = async (provider: LlmProvider) => {
        if (!confirm(`Bạn có chắc chắn muốn xóa nhà cung cấp "${provider.name}" và tất cả cấu hình liên quan?`)) return;
        try {
            await llmService.deleteProvider(provider.id);
            await loadProviders();
            if (selectedProvider?.id === provider.id) {
                setSelectedProvider(null);
                setSelectedProfile(null);
            }
            toast.success("đã xóa nhà cung cấp");
        } catch (e) {
            console.error(e);
            toast.error("Xóa thất bại");
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
            providerId: profile.providerId,
            name: profile.name,
            modelId: profile.modelId,
            apiKey: '',           // Never pre-populate from server response
            configJson: profile.configJson || '{}',
            isActive: profile.isActive,
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
        if (!formData.name) {
            toast.error("Tên nhà cung cấp là bắt buộc");
            return;
        }
        setIsSaving(true);
        try {
            if (isEditingProvider && !selectedProvider) {
                // Create
                const created = await llmService.createProvider(formData);
                setProviders([...providers, created]);
                setSelectedProvider(created);
                setIsEditingProvider(false);
                toast.success("đã tạo nhà cung cấp mới");
            } else if (selectedProvider) {
                // Update implementation would go here
            }
        } catch (e) {
            console.error(e);
            toast.error('Lưu thất bại');
        } finally {
            setIsSaving(false);
        }
    };

    const handleSaveProfile = async () => {
        if (!formData.name || !formData.modelId) {
            toast.error("Tên cấu hình và Model ID là bắt buộc");
            return;
        }
        setIsSaving(true);
        try {
            const profileData = { ...formData };
            if (!profileData.providerId && selectedProvider) profileData.providerId = selectedProvider.id;

            // Only include apiKey if user actually typed a new one
            if (!profileData.apiKey) delete profileData.apiKey;

            if (selectedProfile) {
                await llmService.updateProfile(selectedProfile.id, profileData);
                await loadProviders();
                // Re-select logic
                const updatedProvider = providers.find(p => p.id === selectedProvider?.id);
                if (updatedProvider) {
                    const updatedProfile = updatedProvider.profiles?.find(p => p.id === selectedProfile.id);
                    if (updatedProfile) setSelectedProfile(updatedProfile);
                }
                toast.success("đã cập nhật cấu hình");
            } else {
                const created = await llmService.createProfile(profileData);
                await loadProviders();
                setSelectedProfile(created);
                toast.success("Đã tạo cấu hình mới");
            }
            setIsEditingProfile(false);
        } catch (e) {
            console.error(e);
            toast.error('Lưu thất bại');
        } finally {
            setIsSaving(false);
        }
    };

    const handleTestConnection = async () => {
        if (!selectedProfile) return;
        setTestStatus('idle');
        const success = await llmService.testConnection(selectedProfile.id);
        setTestStatus(success ? 'success' : 'date-error');
        if (success) toast.success("Kết nối thành công!");
        else toast.error("Kết nối thất bại. Kiểm tra API Key và URL.");
    };

    const handleActivateProfile = async () => {
        if (!selectedProfile) return;
        try {
            await llmService.activateProfile(selectedProfile.id);
            setActiveProfileId(selectedProfile.id);
            toast.success(` kích hoạt: ${selectedProfile.name}`);
        } catch (e: any) {
            console.error(e);
            toast.error("Kích hoạt thất bại");
        }
    };

    const handleDeleteProfile = async () => {
        if (!selectedProfile || !confirm('Bạn có chắc chắn muốn xóa cấu hình này?')) return;
        try {
            await llmService.deleteProfile(selectedProfile.id);
            setSelectedProfile(null);
            await loadProviders();
            toast.success(" xóa cấu hình");
        } catch (e) {
            console.error(e);
            toast.error("Xóa thất bại");
        }
    };

    const handleFetchModels = async () => {
        if (!formData.apiKey) {
            toast.error("Vui lòng nhập API Key để tải danh sách models");
            return;
        }
        setIsFetchingModels(true);
        try {
            const models = await llmService.fetchRemoteModels({
                baseUrl: formData.baseUrl || selectedProvider?.baseUrl || '',
                apiKey: formData.apiKey,
                providerType: formData.type || selectedProvider?.type || 'openai'
            });
            setFetchedModels(models);
            toast.success(`✓ Tìm thấy ${models.length} models`);
        } catch (e: any) {
            console.error(e);
            toast.error("Lỗi khi tải danh sách models: " + (e.response?.data?.message || e.message));
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
                apiKey: formData.apiKey || '',   // empty = backend uses stored key by profile ID
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

    const handleSaveGlobalConfig = async () => {
        setIsSaving(true);
        try {
            await llmService.updateSystemSetting('EnableQueryRewriting', String(globalEnableRewriting));
            await llmService.setActiveRewritingProfile(globalRewritingProfileId);
            await llmService.updateSystemSetting('ProductCarouselLimit', String(globalCarouselLimit));
            toast.success("Đã lưu cấu hình RAG Global");
        } catch (e) {
            console.error(e);
            toast.error("Lưu thất bại");
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="container mx-auto py-6 space-y-6">
            <div className="flex flex-col gap-2">
                <h1 className="text-3xl font-bold tracking-tight">Cấu hình LLM (AI)</h1>
                <p className="text-muted-foreground">
                    Quản lý các nhà cung cấp AI, cấu hình profile và kiểm tra kết nối.
                </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-12 gap-6 items-start">

                {/* --- Left Sidebar: Providers List --- */}
                <Card className="md:col-span-4 shadow-md">
                    <CardHeader className="pb-3 border-b bg-muted/20">
                        <div className="flex justify-between items-center">
                            <CardTitle className="text-lg flex items-center gap-2">
                                <Server className="h-5 w-5" /> Nh Cung Cp
                            </CardTitle>
                            <Button size="sm" onClick={handleNewProvider}>
                                <Plus className="h-4 w-4 mr-1" /> Thêm
                            </Button>
                        </div>
                    </CardHeader>
                    <CardContent className="p-0">
                        {isLoading ? (
                            <div className="p-8 text-center text-muted-foreground">
                                <Loader2 className="h-6 w-6 animate-spin mx-auto mb-2" />
                                Đang tải...
                            </div>
                        ) : (
                            <div className="divide-y max-h-[600px] overflow-y-auto">
                                {providers.map(provider => (
                                    <div key={provider.id} className="group">
                                        <div
                                            onClick={() => handleSelectProvider(provider)}
                                            className={`p-4 cursor-pointer transition-colors hover:bg-muted/50 ${selectedProvider?.id === provider.id ? 'bg-muted/80' : ''}`}
                                        >
                                            <div className="flex justify-between items-start mb-2">
                                                <div className="font-semibold">{provider.name}</div>
                                                <Badge variant="outline" className="uppercase text-[10px]">{provider.type}</Badge>
                                            </div>

                                            {/* Nested Profiles */}
                                            {selectedProvider?.id === provider.id && (
                                                <div className="mt-3 space-y-2 pl-2 border-l-2 border-primary/20">
                                                    {provider.profiles && provider.profiles.map(profile => (
                                                        <div
                                                            key={profile.id}
                                                            onClick={(e) => { e.stopPropagation(); handleSelectProfile(profile); }}
                                                            className={`text-sm p-2 rounded-md cursor-pointer flex items-center justify-between transition-colors ${selectedProfile?.id === profile.id
                                                                ? 'bg-primary/10 text-primary font-medium'
                                                                : 'hover:bg-muted text-muted-foreground'}`}
                                                        >
                                                            <div className="flex items-center gap-2">
                                                                <Settings className="h-3 w-3" />
                                                                {profile.name}
                                                            </div>
                                                            {activeProfileId === profile.id && (
                                                                <Badge variant="default" className="bg-green-600 hover:bg-green-700 text-[10px] h-5">ACTIVE</Badge>
                                                            )}
                                                        </div>
                                                    ))}
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        className="w-full justify-start text-xs h-8"
                                                        onClick={(e) => { e.stopPropagation(); handleNewProfile(); }}
                                                    >
                                                        <Plus className="h-3 w-3 mr-2" /> Thêm Profile
                                                    </Button>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                ))}
                                {providers.length === 0 && (
                                    <div className="p-8 text-center text-muted-foreground text-sm">
                                        Chưa có nhà cung cấp nào.
                                    </div>
                                )}
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* --- Right Panel: Main Content --- */}
                <div className="md:col-span-8 space-y-6">
                    {!selectedProvider && !isEditingProvider ? (
                        <Card className="min-h-[400px] flex flex-col items-center justify-center text-center p-8 border-dashed">
                            <div className="bg-muted/50 p-6 rounded-full mb-4">
                                <Cpu className="h-10 w-10 text-muted-foreground" />
                            </div>
                            <h3 className="text-xl font-semibold mb-2">Chưa chọn cấu hình</h3>
                            <p className="text-muted-foreground max-w-sm">
Vui lòng chọn một nhà cung cấp từ danh sách bên trái hoặc tạo mới để bắt đầu cấu hình.
                            </p>
                        </Card>
                    ) : (
                        <>
                            <Card className="shadow-md">
                                <CardHeader className="border-b bg-muted/10 pb-4">
                                    <div className="flex justify-between items-start">
                                        <div>
                                            <CardTitle>{isEditingProvider ? 'Tạo Nhà Cung Cấp Mới' : (selectedProfile ? `Cấu hình: ${selectedProfile.name}` : providerDisplayName(selectedProvider))}</CardTitle>
                                            <CardDescription>
                                                {selectedProfile ? `ID: ${selectedProfile.modelId}  Provider: ${selectedProvider?.name}` : 'Thông tin chung nhà cung cấp'}
                                            </CardDescription>
                                        </div>
                                        <div className="flex gap-2">
                                            {selectedProfile && !isEditingProfile && (
                                                <>
                                                    <Button variant="outline" size="sm" onClick={handleTestConnection}>
                                                        {testStatus === 'success' ? <CheckCircle className="h-4 w-4 text-green-500 mr-2" /> :
                                                            testStatus === 'date-error' ? <XCircle className="h-4 w-4 text-red-500 mr-2" /> :
                                                                <Zap className="h-4 w-4 mr-2" />}
                                                        Test
                                                    </Button>
                                                    <Button size="sm" className="bg-green-600 hover:bg-green-700" onClick={handleActivateProfile}>
                                                        <CheckCircle className="h-4 w-4 mr-2" /> Kích hoạt
                                                    </Button>
                                                    <Button variant="secondary" size="sm" onClick={() => setIsEditingProfile(true)}>
                                                        Sa
                                                    </Button>
                                                </>
                                            )}
                                            {selectedProvider && !isEditingProvider && !selectedProfile && (
                                                <Button variant="destructive" size="sm" onClick={() => handleDeleteProvider(selectedProvider)}>
                                                    <Trash2 className="h-4 w-4 mr-2" /> Xóa Provider
                                                </Button>
                                            )}
                                        </div>
                                    </div>
                                </CardHeader>
                                <CardContent className="p-6">

                                    {/* --- Provider Edit Form --- */}
                                    {isEditingProvider && (
                                        <div className="space-y-4 max-w-xl">
                                            <div className="grid gap-2">
                                                <Label htmlFor="provider-name">Tên Nhà Cung Cấp</Label>
                                                <Input id="provider-name" value={formData.name} onChange={(e) => setFormData({ ...formData, name: e.target.value })} placeholder="VD: OpenRouter, OpenAI..." />
                                            </div>
                                            <div className="grid gap-2">
                                                <Label htmlFor="provider-type">Loại (Type)</Label>
                                                <Select value={formData.type} onValueChange={(v) => setFormData({ ...formData, type: v })}>
                                                    <SelectTrigger>
                                                        <SelectValue placeholder="Chọn loại" />
                                                    </SelectTrigger>
                                                    <SelectContent>
                                                        <SelectItem value="openai">OpenAI Compatible</SelectItem>
                                                        <SelectItem value="azure">Azure OpenAI</SelectItem>
                                                        <SelectItem value="ollama">Ollama</SelectItem>
                                                    </SelectContent>
                                                </Select>
                                            </div>
                                            <div className="grid gap-2">
                                                <Label htmlFor="base-url">Base URL</Label>
                                                <Input id="base-url" value={formData.baseUrl} onChange={(e) => setFormData({ ...formData, baseUrl: e.target.value })} placeholder="https://api.openai.com/v1" />
                                            </div>
                                            <div className="grid gap-2">
                                                <Label htmlFor="website">Website Documentation</Label>
                                                <Input id="website" value={formData.website} onChange={(e) => setFormData({ ...formData, website: e.target.value })} placeholder="https://..." />
                                            </div>
                                        </div>
                                    )}

                                    {/* --- Profile Edit Form --- */}
                                    {(isEditingProfile) && (
                                        <div className="space-y-6">
                                            <div className="grid md:grid-cols-2 gap-4">
                                                <div className="grid gap-2">
                                                    <Label>Tên Profile Display</Label>
                                                    <Input value={formData.name} onChange={(e) => setFormData({ ...formData, name: e.target.value })} placeholder="VD: GPT-4 Production" />
                                                </div>
                                                <div className="grid gap-2">
                                                    <Label className="flex justify-between items-center">
                                                        <span>Model ID</span>
                                                        <Button variant="link" size="sm" className="h-auto p-0 text-xs" onClick={handleFetchModels} disabled={isFetchingModels}>
                                                            {isFetchingModels ? <Loader2 className="h-3 w-3 animate-spin mr-1" /> : <CloudDownload className="h-3 w-3 mr-1" />}
                                                            Ly DS t Server
                                                        </Button>
                                                    </Label>
                                                    <div className="flex gap-2">
                                                        {fetchedModels.length > 0 ? (
                                                            <Select value={formData.modelId} onValueChange={(v) => setFormData({ ...formData, modelId: v })}>
                                                                <SelectTrigger className="flex-1">
                                                                    <SelectValue placeholder="Chọn model" />
                                                                </SelectTrigger>
                                                                <SelectContent>
                                                                    {fetchedModels.map(m => <SelectItem key={m} value={m}>{m}</SelectItem>)}
                                                                </SelectContent>
                                                            </Select>
                                                        ) : (
                                                            <Input value={formData.modelId} onChange={(e) => setFormData({ ...formData, modelId: e.target.value })} placeholder="VD: gpt-3.5-turbo" />
                                                        )}
                                                    </div>
                                                </div>
                                            </div>

                                            <div className="grid gap-2">
                                                <Label>API Key</Label>
                                                <Input
                                                    type="password"
                                                    value={formData.apiKey}
                                                    onChange={(e) => setFormData({ ...formData, apiKey: e.target.value })}
                                                    placeholder={selectedProfile?.hasApiKey
                                                        ? '••••••••• (bỏ trống để giữ key hiện tại)'
                                                        : 'sk-... (nhập API Key)'}
                                                />
                                                <p className="text-[10px] text-muted-foreground">
                                                    {selectedProfile?.hasApiKey
                                                        ? '✓ Đã có API Key. Chỉ nhập nếu muốn thay đổi.'
                                                        : 'Nhập API Key để xác thực với nhà cung cấp.'}
                                                </p>
                                            </div>

                                            <Separator />

                                            <div className="space-y-4">
                                                <div className="flex items-center justify-between">
                                                    <Label>Cấu hình nng cao (JSON)</Label>
                                                    <div className="flex items-center bg-muted p-1 rounded-md">
                                                        <Button variant={isVisualMode ? 'secondary' : 'ghost'} size="sm" className="h-7 text-xs" onClick={() => setIsVisualMode(true)}>Visual</Button>
                                                        <Button variant={!isVisualMode ? 'secondary' : 'ghost'} size="sm" className="h-7 text-xs" onClick={() => setIsVisualMode(false)}>JSON</Button>
                                                    </div>
                                                </div>

                                                {isVisualMode ? (
                                                    <VisualConfigEditor
                                                        jsonString={formData.configJson || '{}'}
                                                        onChange={(newJson) => setFormData({ ...formData, configJson: newJson })}
                                                    />
                                                ) : (
                                                    <textarea
                                                        className="w-full min-h-[200px] p-3 font-mono text-sm border rounded-md bg-muted/50"
                                                        value={formData.configJson}
                                                        onChange={(e) => setFormData({ ...formData, configJson: e.target.value })}
                                                    />
                                                )}
                                            </div>
                                        </div>
                                    )}

                                    {/* --- Detail Read View --- */}
                                    {selectedProfile && !isEditingProfile && (
                                        <div className="grid md:grid-cols-2 gap-8">
                                            <div className="space-y-4">
                                                <div className="grid gap-1">
                                                    <Label className="text-muted-foreground">Model ID</Label>
                                                    <div className="font-mono bg-muted/30 px-3 py-1 rounded inline-block">{selectedProfile.modelId}</div>
                                                </div>
                                                <div className="grid gap-1">
                                                    <Label className="text-muted-foreground">API Key</Label>
                                                    <div className="font-mono text-muted-foreground"></div>
                                                </div>
                                                <div className="grid gap-1">
                                                    <Label className="text-muted-foreground">JSON Config</Label>
                                                    <pre className="text-xs bg-muted p-3 rounded-lg overflow-x-auto border max-h-[200px]">{selectedProfile.configJson}</pre>
                                                </div>

                                                <RagConfigSummary configJson={selectedProfile.configJson} />
                                            </div>
                                            <div className="space-y-6 border-l pl-6">
                                                <div className="space-y-2">
                                                    <h4 className="font-medium flex items-center gap-2"><MessageSquare className="h-4 w-4" /> Quick Chat Test</h4>
                                                    <div className="flex gap-2">
                                                        <Input
                                                            placeholder="Type a message..."
                                                            value={testChatMsg}
                                                            onChange={(e) => setTestChatMsg(e.target.value)}
                                                        />
                                                        <Button onClick={handleTestChat} disabled={isTestingChat}>
                                                            {isTestingChat ? <Loader2 className="h-4 w-4 animate-spin" /> : <Play className="h-4 w-4" />}
                                                        </Button>
                                                    </div>
                                                    {testChatResult && (
                                                        <div className={`text-sm p-3 rounded-md border ${testChatResult.success ? 'bg-green-500/10 border-green-200 text-green-700' : 'bg-red-500/10 border-red-200 text-red-700'}`}>
                                                            <div className="font-semibold text-xs mb-1 flex justify-between">
                                                                <span>{testChatResult.success ? 'RESPONSE' : 'ERROR'}</span>
                                                                <span>{testChatResult.latency}</span>
                                                            </div>
                                                            <div>{testChatResult.message || testChatResult.error}</div>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    )}
                                </CardContent>
                                {(isEditingProvider || isEditingProfile) && (
                                    <CardFooter className="flex justify-end gap-2 bg-muted/10 py-4">
                                        <Button variant="ghost" onClick={() => { setIsEditingProvider(false); setIsEditingProfile(false); }}>Hủy bỏ</Button>
                                        <Button onClick={isEditingProvider ? handleSaveProvider : handleSaveProfile} disabled={isSaving}>
                                            {isSaving ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Save className="h-4 w-4 mr-2" />}
                                            {isEditingProvider ? 'Tạo Provider' : 'Lưu thay đổi'}
                                        </Button>
                                    </CardFooter>
                                )}
                            </Card>

                            {/* --- System Actions (Only when not editing) --- */}
                            {!isEditingProfile && !isEditingProvider && (
                                <Card>
                                    <CardHeader className="pb-3">
                                        <CardTitle className="text-lg flex items-center gap-2"><Settings className="h-5 w-5" /> System Utilities</CardTitle>
                                    </CardHeader>
                                    <CardContent className="flex flex-wrap gap-4">
                                        <Button variant="outline" onClick={async () => {
                                            try {
                                                const res = await llmService.testEmbedding();
                                                toast.success(`Embedding Test Success! Latency: ${res.latencyMs}ms`);
                                            } catch (e: any) {
                                                toast.error("Failed: " + e.message);
                                            }
                                        }}>
                                            <Sliders className="h-4 w-4 mr-2" /> Test Embedding
                                        </Button>

                                        <Button variant="outline" onClick={async () => {
                                            if (!confirm('Hành động này sẽ Re-index lại toàn bộ sản phẩm. Cần một khoảng thời gian. Tiếp tục?')) return;
                                            try {
                                                toast.info("Đang bắt đầu Re-index...");
                                                await llmService.reindexVectorDb();
                                                toast.success("Yêu cầu Re-index thành công.");
                                            } catch (e: any) {
                                                toast.error("Failed: " + e.message);
                                            }
                                        }}>
                                            <Globe className="h-4 w-4 mr-2" /> Re-index Vector DB
                                        </Button>
                                    </CardContent>
                                </Card>
                            )}

                            {/* --- Global RAG Configuration (New) --- */}
                            {!isEditingProfile && !isEditingProvider && (
                                <Card className="shadow-md border-blue-200 dark:border-blue-900 bg-blue-50/20">
                                    <CardHeader className="pb-3 border-b">
                                        <div className="flex justify-between items-center">
                                            <CardTitle className="text-lg flex items-center gap-2">
                                                <Zap className="h-5 w-5 text-blue-600" /> Global Configuration
                                            </CardTitle>
                                            <Button size="sm" onClick={handleSaveGlobalConfig} disabled={isSaving}>
                                                {isSaving ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : <Save className="h-4 w-4 mr-2" />}
                                                Lưu cấu hình Global
                                            </Button>
                                        </div>
                                        <CardDescription>Cấu hình áp dụng cho toàn bộ hệ thống RAG chatbot, độc lập với profile Chat chính.</CardDescription>
                                    </CardHeader>
                                    <CardContent className="p-6 space-y-6">
                                        {/* Enable Query Rewriting */}
                                        <div className="flex items-center justify-between p-4 bg-white/60 dark:bg-black/20 rounded-md border">
                                            <div className="space-y-0.5">
                                                <Label htmlFor="global-enable-rewriting" className="text-sm font-medium cursor-pointer">Enable Query Rewriting</Label>
                                                <p className="text-xs text-muted-foreground">
Sử dụng LLM riêng biệt để tối ưu câu hỏi và trích xuất filters trước khi tìm kiếm.
                                                </p>
                                            </div>
                                            <Switch
                                                id="global-enable-rewriting"
                                                checked={globalEnableRewriting}
                                                onCheckedChange={setGlobalEnableRewriting}
                                            />
                                        </div>

                                        {/* Active Rewriting Profile */}
                                        <div className="space-y-2">
                                            <Label>Profile dùng cho Rewriting</Label>
                                            <Select
                                                value={globalRewritingProfileId?.toString() || "null"}
                                                onValueChange={(v) => setGlobalRewritingProfileId(v === "null" ? null : parseInt(v))}
                                            >
                                                <SelectTrigger>
                                                    <SelectValue placeholder="Chọn profile..." />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    <SelectItem value="null">-- Disabled / Use Main Profile --</SelectItem>
                                                    {providers.flatMap(p => p.profiles || []).map(profile => (
                                                        <SelectItem key={profile.id} value={profile.id.toString()}>
                                                            {profile.name} ({profile.modelId})
                                                        </SelectItem>
                                                    ))}
                                                </SelectContent>
                                            </Select>
                                            <p className="text-xs text-muted-foreground">
                                                Chọn một profile nhẹ/nhanh (ví dụ: gpt-4o-mini, llama3) để giảm chi phí và độ trễ rewriting.
                                                Nếu để trống, hệ thống sẽ fallback về Main Chat Profile.
                                            </p>
                                        </div>

                                        {/* Product Carousel Limit */}
                                        <div className="space-y-2">
                                            <Label>Product Carousel Limit: {globalCarouselLimit}</Label>
                                            <div className="flex gap-4 items-center">
                                                <Slider
                                                    className="flex-1"
                                                    value={[globalCarouselLimit]}
                                                    min={3}
                                                    max={10}
                                                    step={1}
                                                    onValueChange={([v]) => setGlobalCarouselLimit(v)}
                                                />
                                                <div className="w-12 text-center font-mono text-sm border rounded py-1">{globalCarouselLimit}</div>
                                            </div>
                                            <p className="text-xs text-muted-foreground">Số lượng sản phẩm tối đa trả về trong Carousel.</p>
                                        </div>
                                    </CardContent>
                                </Card>
                            )}
                        </>
                    )}
                </div>
            </div>
        </div>
    );
}

function providerDisplayName(provider: LlmProvider | null) {
    if (!provider) return '...';
    return provider.name;
}

function RagConfigSummary({ configJson }: { configJson?: string }) {
    if (!configJson) return null;
    try {
        const config = JSON.parse(configJson);
        const rewriting = config.enable_query_rewriting ?? true;

        return (
            <div className="grid gap-1 pt-2">
                <div className="flex items-center gap-2">
                    <Zap className="h-3 w-3 text-blue-500" />
                    <Label className="text-blue-600 dark:text-blue-400 font-semibold text-xs uppercase">Global Settings</Label>
                </div>
                <div className="bg-blue-50/50 dark:bg-blue-950/20 p-2 rounded-md border border-blue-100 dark:border-blue-900 text-xs space-y-1">
                    <div className="flex justify-between">
                        <span className="text-muted-foreground">Query Rewriting:</span>
                        <span className={`font-medium ${rewriting ? 'text-green-600' : 'text-red-500'}`}>
                            {rewriting ? 'Enabled' : 'Disabled'}
                        </span>
                    </div>
                    {rewriting && config.rewriting_model_id && (
                        <div className="flex justify-between">
                            <span className="text-muted-foreground">Rewriter Model:</span>
                            <span className="font-mono bg-white dark:bg-black px-1 rounded border">{config.rewriting_model_id}</span>
                        </div>
                    )}
                    <div className="flex justify-between">
                        <span className="text-muted-foreground">Carousel Limit:</span>
                        <span className="font-medium">{config.product_carousel_limit ?? 5} items</span>
                    </div>
                </div>
            </div>
        );
    } catch {
        return null;
    }
}

function VisualConfigEditor({ jsonString, onChange }: { jsonString: string, onChange: (val: string) => void }) {
    const [config, setConfig] = useState<any>({});
    const [error, setError] = useState<string | null>(null);

    // Add Parameter State
    const [newKey, setNewKey] = useState("");
    const [newType, setNewType] = useState("string");
    const [newValue, setNewValue] = useState("");

    useEffect(() => {
        try {
            const parsed = JSON.parse(jsonString || '{}');
            setConfig(parsed);
            setError(null);
        } catch (e) {
            setError("Invalid JSON detected. Switched to safe mode.");
        }
    }, [jsonString]);

    const updateConfig = (key: string, value: any) => {
        let newConfig = { ...config };
        if (value === undefined || value === null) delete newConfig[key];
        else newConfig[key] = value;
        setConfig(newConfig);
        onChange(JSON.stringify(newConfig, null, 2));
    };

    const handleAddParam = () => {
        if (!newKey) return;
        let val: any = newValue;
        if (newType === 'number') val = parseFloat(newValue) || 0;
        if (newType === 'boolean') val = (newValue.toLowerCase() === 'true');

        updateConfig(newKey, val);
        setNewKey("");
        setNewValue("");
    };

    if (error) return <div className="text-destructive text-sm p-4 bg-destructive/10 rounded-md">{error}</div>;

    return (
        <div className="space-y-4 bg-card border rounded-lg p-4">
            {Object.keys(config).length === 0 ? (
                <div className="text-center text-muted-foreground text-sm py-8 border-2 border-dashed rounded-lg bg-muted/10">
                    Configuration is empty. Add parameters below.
                </div>
            ) : (
                <div className="space-y-3">
                    {Object.entries(config).map(([key, value]) => (
                        <div key={key} className="flex gap-4 items-start p-3 bg-muted/30 rounded-lg group hover:bg-muted/50 transition border border-transparent hover:border-muted-foreground/20">
                            <div className="flex-1 space-y-2">
                                <div className="flex justify-between items-center">
                                    <Label className="font-mono text-xs font-semibold text-primary">{key}</Label>
                                    <Badge variant="outline" className="text-[10px] h-4 leading-none text-muted-foreground">{typeof value}</Badge>
                                </div>

                                {/* Dynamic Control */}
                                {typeof value === 'boolean' ? (
                                    <div className="flex items-center space-x-2 h-8">
                                        <Switch
                                            checked={value as boolean}
                                            onCheckedChange={(c) => updateConfig(key, c)}
                                        />
                                        <span className="text-xs text-muted-foreground">{value ? 'True' : 'False'}</span>
                                    </div>
                                ) : typeof value === 'number' ? (
                                    <div className="space-y-2">
                                        <div className="flex gap-2 items-center">
                                            <Slider
                                                className="flex-1"
                                                value={[value as number]}
                                                min={0}
                                                max={(value as number) <= 2 ? 2 : (value as number) <= 10 ? 10 : 8000}
                                                step={(value as number) <= 2 ? 0.1 : 1}
                                                onValueChange={([v]) => updateConfig(key, v)}
                                            />
                                            <Input
                                                type="number"
                                                className="w-20 h-8 text-xs font-mono text-right"
                                                value={value as number}
                                                onChange={(e) => updateConfig(key, parseFloat(e.target.value))}
                                            />
                                        </div>
                                    </div>
                                ) : (
                                    <Input
                                        value={value as string}
                                        onChange={(e) => updateConfig(key, e.target.value)}
                                        className="h-8 text-sm"
                                    />
                                )}
                            </div>
                            <Button
                                variant="ghost"
                                size="sm"
                                className="h-8 w-8 p-0 text-muted-foreground hover:text-destructive opacity-50 group-hover:opacity-100 transition"
                                onClick={() => updateConfig(key, undefined)}
                                title="Remove Parameter"
                            >
                                <Trash2 className="h-4 w-4" />
                            </Button>
                        </div>
                    ))}
                </div>
            )}



            <Separator />

            {/* Add Parameter Section */}
            <div className="grid gap-3 p-4 bg-muted/20 rounded-lg border border-dashed">
                <Label className="text-xs font-semibold uppercase text-muted-foreground">Add New Parameter</Label>
                <div className="flex gap-2">
                    <Input
                        placeholder="Key (e.g. top_k)"
                        className="flex-1 h-8 text-sm font-mono"
                        value={newKey}
                        onChange={(e) => setNewKey(e.target.value)}
                    />
                    <Select value={newType} onValueChange={setNewType}>
                        <SelectTrigger className="w-[110px] h-8 text-xs">
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="string">String</SelectItem>
                            <SelectItem value="number">Number</SelectItem>
                            <SelectItem value="boolean">Boolean</SelectItem>
                        </SelectContent>
                    </Select>
                </div>
                <div className="flex gap-2">
                    {newType === 'boolean' ? (
                        <Select value={newValue} onValueChange={setNewValue}>
                            <SelectTrigger className="flex-1 h-8 text-xs">
                                <SelectValue placeholder="Select value" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="true">True</SelectItem>
                                <SelectItem value="false">False</SelectItem>
                            </SelectContent>
                        </Select>
                    ) : (
                        <Input
                            type={newType === 'number' ? 'number' : 'text'}
                            placeholder="Value"
                            className="flex-1 h-8 text-sm"
                            value={newValue}
                            onChange={(e) => setNewValue(e.target.value)}
                        />
                    )}
                    <Button
                        size="sm"
                        className="h-8 px-4"
                        disabled={!newKey || !newValue}
                        onClick={handleAddParam}
                    >
                        <Plus className="h-4 w-4 mr-1" /> Add
                    </Button>
                </div>
            </div>

            <div className="flex justify-end pt-2">
                <Button variant="destructive" size="sm" onClick={() => {
                    if (confirm("Clear entire configuration?")) {
                        setConfig({});
                        onChange("{}");
                    }
                }}>
                    <Trash2 className="h-4 w-4 mr-2" /> Clear All Config
                </Button>
            </div>
        </div >
    );
}
