'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Progress } from '@/components/ui/progress';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { cn } from '@/lib/utils';
import {
    Archive,
    Calendar,
    CheckCircle,
    Clock,
    Database,
    Download,
    FileText,
    History,
    Import,
    RefreshCw,
    Save,
    Settings,
    Upload,
    XCircle
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface BackupItem {
    id: string;
    name: string;
    type: 'full' | 'partial' | 'settings' | 'content-only';
    status: 'completed' | 'in-progress' | 'failed' | 'scheduled';
    size: string;
    createdAt: string;
    description: string;
    includes: string[];
    downloadUrl?: string;
}

interface BackupSettings {
    autoBackup: boolean;
    frequency: 'daily' | 'weekly' | 'monthly';
    retentionDays: number;
    compressionLevel: 'none' | 'low' | 'medium' | 'high';
    encryptBackups: boolean;
    encryptionKey: string;
    backupLocation: 'local' | 'cloud' | 'both';
    cloudProvider: 'aws' | 'google' | 'azure';
    cloudSettings: {
        accessKey: string;
        secretKey: string;
        bucket: string;
        region: string;
    };
    includePosts: boolean;
    includeComments: boolean;
    includeMedia: boolean;
    includeSettings: boolean;
    includeUsers: boolean;
    includeAnalytics: boolean;
}

interface RestoreProgress {
    isRestoring: boolean;
    currentStep: string;
    progress: number;
    totalSteps: number;
    currentStepProgress: number;
    logs: string[];
}

export default function BlogBackupRestorePage() {
    const [backups, setBackups] = useState<BackupItem[]>([]);
    const [settings, setSettings] = useState<BackupSettings>({
        autoBackup: true,
        frequency: 'weekly',
        retentionDays: 30,
        compressionLevel: 'medium',
        encryptBackups: true,
        encryptionKey: '',
        backupLocation: 'both',
        cloudProvider: 'aws',
        cloudSettings: {
            accessKey: '',
            secretKey: '',
            bucket: '',
            region: 'us-east-1'
        },
        includePosts: true,
        includeComments: true,
        includeMedia: true,
        includeSettings: true,
        includeUsers: true,
        includeAnalytics: false
    });
    const [restoreProgress, setRestoreProgress] = useState<RestoreProgress>({
        isRestoring: false,
        currentStep: '',
        progress: 0,
        totalSteps: 0,
        currentStepProgress: 0,
        logs: []
    });
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [selectedBackups, setSelectedBackups] = useState<string[]>([]);
    const [uploadedFile, setUploadedFile] = useState<File | null>(null);

    // Load backups and settings
    useEffect(() => {
        loadBackups();
        loadSettings();
    }, []);

    const loadBackups = async () => {
        try {
            setLoading(true);
            // Mock backup data
            const mockBackups: BackupItem[] = [
                {
                    id: '1',
                    name: 'Full Backup - 2025-01-15',
                    type: 'full',
                    status: 'completed',
                    size: '2.3 GB',
                    createdAt: '2025-01-15T10:30:00Z',
                    description: 'Complete blog backup including all posts, media, and settings',
                    includes: ['Posts', 'Comments', 'Media', 'Settings', 'Users', 'Analytics'],
                    downloadUrl: '/api/backups/1/download'
                },
                {
                    id: '2',
                    name: 'Content Only - 2025-01-14',
                    type: 'content-only',
                    status: 'completed',
                    size: '156 MB',
                    createdAt: '2025-01-14T08:00:00Z',
                    description: 'Posts and comments backup',
                    includes: ['Posts', 'Comments'],
                    downloadUrl: '/api/backups/2/download'
                },
                {
                    id: '3',
                    name: 'Scheduled Backup - 2025-01-16',
                    type: 'full',
                    status: 'scheduled',
                    size: '--',
                    createdAt: '2025-01-16T00:00:00Z',
                    description: 'Automated weekly backup',
                    includes: ['Posts', 'Comments', 'Media', 'Settings', 'Users']
                }
            ];
            setBackups(mockBackups);
        } catch (error) {
            console.error('Failed to load backups:', error);
            toast.error('Khng th ti danh sch backup');
        } finally {
            setLoading(false);
        }
    };

    const loadSettings = async () => {
        try {
            // Mock load settings from API
            console.log('Loading backup settings...');
        } catch (error) {
            console.error('Failed to load settings:', error);
        }
    };

    // Create new backup
    const createBackup = async (type: BackupItem['type'], customName?: string) => {
        try {
            const backupName = customName || `${type === 'full' ? 'Full' : 'Partial'} Backup - ${new Date().toLocaleDateString('vi-VN')}`;

            // Mock backup creation
            const newBackup: BackupItem = {
                id: Date.now().toString(),
                name: backupName,
                type,
                status: 'in-progress',
                size: '--',
                createdAt: new Date().toISOString(),
                description: 'Creating backup...',
                includes: Object.entries(settings).filter(([key, value]) =>
                    key.startsWith('include') && value
                ).map(([key]) => key.replace('include', ''))
            };

            setBackups(prev => [newBackup, ...prev]);

            // Simulate backup progress
            setTimeout(() => {
                setBackups(prev => prev.map(backup =>
                    backup.id === newBackup.id
                        ? { ...backup, status: 'completed' as const, size: '1.2 GB', downloadUrl: `/api/backups/${backup.id}/download` }
                        : backup
                ));
                toast.success('Backup  c to thnh cng');
            }, 3000);

            toast.success(' bt u to backup');
        } catch (error) {
            console.error('Failed to create backup:', error);
            toast.error('Khng th to backup');
        }
    };

    // Restore from backup
    const restoreFromBackup = async (backupId: string) => {
        if (!confirm('Bn c chc chn mun khi phc t backup ny? Thao tc ny s ghi  d liu hin ti.')) {
            return;
        }

        try {
            setRestoreProgress({
                isRestoring: true,
                currentStep: 'Preparing restore...',
                progress: 0,
                totalSteps: 5,
                currentStepProgress: 0,
                logs: ['Bt u qu trnh khi phc...']
            });

            const steps = [
                'Validating backup file...',
                'Extracting backup data...',
                'Restoring database...',
                'Restoring media files...',
                'Finalizing restoration...'
            ];

            for (let i = 0; i < steps.length; i++) {
                setRestoreProgress(prev => ({
                    ...prev,
                    currentStep: steps[i],
                    progress: ((i + 1) / steps.length) * 100,
                    logs: [...prev.logs, steps[i]]
                }));

                // Simulate step progress
                for (let progress = 0; progress <= 100; progress += 20) {
                    setRestoreProgress(prev => ({
                        ...prev,
                        currentStepProgress: progress
                    }));
                    await new Promise(resolve => setTimeout(resolve, 200));
                }
            }

            setRestoreProgress(prev => ({
                ...prev,
                isRestoring: false,
                currentStep: 'Restore completed successfully!',
                logs: [...prev.logs, 'Khi phc hon tt thnh cng!']
            }));

            toast.success('Khi phc thnh cng t backup');
        } catch (error) {
            console.error('Failed to restore backup:', error);
            setRestoreProgress(prev => ({
                ...prev,
                isRestoring: false,
                currentStep: 'Restore failed!',
                logs: [...prev.logs, 'Li: Khng th khi phc t backup']
            }));
            toast.error('Khng th khi phc t backup');
        }
    };

    // Delete backup
    const deleteBackup = async (backupId: string) => {
        if (!confirm('Bn c chc chn mun xa backup ny?')) return;

        try {
            setBackups(prev => prev.filter(backup => backup.id !== backupId));
            toast.success(' xa backup');
        } catch (error) {
            console.error('Failed to delete backup:', error);
            toast.error('Khng th xa backup');
        }
    };

    // Save settings
    const saveSettings = async () => {
        try {
            setSaving(true);
            // Mock API call
            console.log('Saving backup settings:', settings);
            toast.success(' lu ci t backup');
        } catch (error) {
            console.error('Failed to save settings:', error);
            toast.error('Khng th lu ci t');
        } finally {
            setSaving(false);
        }
    };

    // Handle file upload
    const handleFileUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (file) {
            setUploadedFile(file);
            toast.success(` chn file: ${file.name}`);
        }
    };

    // Import backup
    const importBackup = async () => {
        if (!uploadedFile) {
            toast.error('Vui lng chn file backup');
            return;
        }

        try {
            // Mock import process
            const formData = new FormData();
            formData.append('backup', uploadedFile);

            toast.success('ang import backup...');

            // Simulate import
            setTimeout(() => {
                const newBackup: BackupItem = {
                    id: Date.now().toString(),
                    name: `Imported - ${uploadedFile.name}`,
                    type: 'full',
                    status: 'completed',
                    size: `${(uploadedFile.size / (1024 * 1024)).toFixed(1)} MB`,
                    createdAt: new Date().toISOString(),
                    description: 'Imported backup file',
                    includes: ['Posts', 'Comments', 'Media', 'Settings'],
                    downloadUrl: `/api/backups/imported-${Date.now()}/download`
                };

                setBackups(prev => [newBackup, ...prev]);
                setUploadedFile(null);
                toast.success('Import backup thnh cng');
            }, 2000);
        } catch (error) {
            console.error('Failed to import backup:', error);
            toast.error('Khng th import backup');
        }
    };

    // Update setting
    const updateSetting = <K extends keyof BackupSettings>(key: K, value: BackupSettings[K]) => {
        setSettings(prev => ({
            ...prev,
            [key]: value
        }));
    };

    // Get status badge
    const getStatusBadge = (status: BackupItem['status']) => {
        const variants = {
            completed: { variant: 'default' as const, icon: CheckCircle, text: 'Hon thnh' },
            'in-progress': { variant: 'secondary' as const, icon: Clock, text: 'ang x l' },
            failed: { variant: 'destructive' as const, icon: XCircle, text: 'Tht bi' },
            scheduled: { variant: 'outline' as const, icon: Calendar, text: ' ln lch' }
        };

        const config = variants[status];
        const IconComponent = config.icon;

        return (
            <Badge variant={config.variant} className="flex items-center gap-1">
                <IconComponent className="w-3 h-3" />
                {config.text}
            </Badge>
        );
    };

    // Get type badge
    const getTypeBadge = (type: BackupItem['type']) => {
        const variants = {
            'full': { color: 'bg-blue-500', text: 'y ' },
            'partial': { color: 'bg-green-500', text: 'Mt phn' },
            'settings': { color: 'bg-purple-500', text: 'Ci t' },
            'content-only': { color: 'bg-orange-500', text: 'Ni dung' }
        };

        const config = variants[type];

        return (
            <Badge variant="secondary" className={cn("text-white", config.color)}>
                {config.text}
            </Badge>
        );
    };

    return (
        <div className="p-6 max-w-7xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Backup & Khi phc</h1>
                    <p className="text-gray-500">Qun l backup v khi phc d liu blog</p>
                </div>
                <div className="flex gap-2">
                    <Button
                        variant="outline"
                        onClick={() => createBackup('content-only')}
                        disabled={loading}
                    >
                        <Save className="w-4 h-4 mr-2" />
                        Backup nhanh
                    </Button>
                    <Button
                        onClick={() => createBackup('full')}
                        disabled={loading}
                    >
                        <Database className="w-4 h-4 mr-2" />
                        Backup y 
                    </Button>
                </div>
            </div>

            <Tabs defaultValue="backups" className="space-y-6">
                <TabsList className="grid w-full grid-cols-4">
                    <TabsTrigger value="backups">Backup</TabsTrigger>
                    <TabsTrigger value="restore">Khi phc</TabsTrigger>
                    <TabsTrigger value="settings">Ci t</TabsTrigger>
                    <TabsTrigger value="history">Lch s</TabsTrigger>
                </TabsList>

                {/* Backups Tab */}
                <TabsContent value="backups" className="space-y-6">
                    {/* Quick Actions */}
                    <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                        <Card className="cursor-pointer hover:shadow-md transition-shadow"
                            onClick={() => createBackup('full')}>
                            <CardContent className="p-6 text-center">
                                <Database className="w-8 h-8 text-blue-600 mx-auto mb-2" />
                                <h3 className="font-semibold">Backup y </h3>
                                <p className="text-sm text-gray-500">Ton b d liu</p>
                            </CardContent>
                        </Card>

                        <Card className="cursor-pointer hover:shadow-md transition-shadow"
                            onClick={() => createBackup('content-only')}>
                            <CardContent className="p-6 text-center">
                                <FileText className="w-8 h-8 text-green-600 mx-auto mb-2" />
                                <h3 className="font-semibold">Ch ni dung</h3>
                                <p className="text-sm text-gray-500">Bi vit & bnh lun</p>
                            </CardContent>
                        </Card>

                        <Card className="cursor-pointer hover:shadow-md transition-shadow"
                            onClick={() => createBackup('settings')}>
                            <CardContent className="p-6 text-center">
                                <Settings className="w-8 h-8 text-purple-600 mx-auto mb-2" />
                                <h3 className="font-semibold">Ci t</h3>
                                <p className="text-sm text-gray-500">Ch cu hnh</p>
                            </CardContent>
                        </Card>

                        <Card className="cursor-pointer hover:shadow-md transition-shadow">
                            <CardContent className="p-6 text-center">
                                <Calendar className="w-8 h-8 text-orange-600 mx-auto mb-2" />
                                <h3 className="font-semibold">Ln lch</h3>
                                <p className="text-sm text-gray-500">Backup t ng</p>
                            </CardContent>
                        </Card>
                    </div>

                    {/* Backup List */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Archive className="w-5 h-5" />
                                Danh sch Backup ({backups.length})
                            </CardTitle>
                            <CardDescription>
                                Qun l cc file backup ca blog
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            {loading ? (
                                <div className="flex items-center justify-center py-12">
                                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900" />
                                </div>
                            ) : (
                                <Table>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead className="w-12">
                                                <Checkbox
                                                    checked={selectedBackups.length === backups.length}
                                                    onCheckedChange={(checked) => {
                                                        setSelectedBackups(checked ? backups.map(b => b.id) : []);
                                                    }}
                                                />
                                            </TableHead>
                                            <TableHead>Tn backup</TableHead>
                                            <TableHead>Loi</TableHead>
                                            <TableHead>Trng thi</TableHead>
                                            <TableHead>Kch thc</TableHead>
                                            <TableHead>Ngy to</TableHead>
                                            <TableHead className="text-right">Thao tc</TableHead>
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {backups.length > 0 ? (
                                            backups.map((backup) => (
                                                <TableRow key={backup.id}>
                                                    <TableCell>
                                                        <Checkbox
                                                            checked={selectedBackups.includes(backup.id)}
                                                            onCheckedChange={(checked) => {
                                                                setSelectedBackups(prev =>
                                                                    checked
                                                                        ? [...prev, backup.id]
                                                                        : prev.filter(id => id !== backup.id)
                                                                );
                                                            }}
                                                        />
                                                    </TableCell>
                                                    <TableCell>
                                                        <div>
                                                            <div className="font-semibold">{backup.name}</div>
                                                            <div className="text-sm text-gray-500">{backup.description}</div>
                                                            <div className="flex flex-wrap gap-1 mt-1">
                                                                {backup.includes.map((item) => (
                                                                    <Badge key={item} variant="outline" className="text-xs">
                                                                        {item}
                                                                    </Badge>
                                                                ))}
                                                            </div>
                                                        </div>
                                                    </TableCell>
                                                    <TableCell>{getTypeBadge(backup.type)}</TableCell>
                                                    <TableCell>{getStatusBadge(backup.status)}</TableCell>
                                                    <TableCell>{backup.size}</TableCell>
                                                    <TableCell>
                                                        {new Date(backup.createdAt).toLocaleDateString('vi-VN')}
                                                    </TableCell>
                                                    <TableCell className="text-right">
                                                        <div className="flex items-center justify-end gap-1">
                                                            {backup.downloadUrl && backup.status === 'completed' && (
                                                                <Button
                                                                    variant="ghost"
                                                                    size="sm"
                                                                    onClick={() => window.open(backup.downloadUrl)}
                                                                >
                                                                    <Download className="w-4 h-4" />
                                                                </Button>
                                                            )}
                                                            {backup.status === 'completed' && (
                                                                <Button
                                                                    variant="ghost"
                                                                    size="sm"
                                                                    onClick={() => restoreFromBackup(backup.id)}
                                                                >
                                                                    <RefreshCw className="w-4 h-4" />
                                                                </Button>
                                                            )}
                                                            <Button
                                                                variant="ghost"
                                                                size="sm"
                                                                onClick={() => deleteBackup(backup.id)}
                                                                className="text-red-600 hover:text-red-700"
                                                            >
                                                                <XCircle className="w-4 h-4" />
                                                            </Button>
                                                        </div>
                                                    </TableCell>
                                                </TableRow>
                                            ))
                                        ) : (
                                            <TableRow>
                                                <TableCell colSpan={7} className="text-center py-8">
                                                    <Archive className="w-12 h-12 text-gray-300 mx-auto mb-4" />
                                                    <p className="text-gray-500 mb-2">Cha c backup no</p>
                                                    <p className="text-sm text-gray-400">Hy to backup u tin</p>
                                                </TableCell>
                                            </TableRow>
                                        )}
                                    </TableBody>
                                </Table>
                            )}
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* Restore Tab */}
                <TabsContent value="restore" className="space-y-6">
                    {/* Upload Backup */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Upload className="w-5 h-5" />
                                Import Backup
                            </CardTitle>
                            <CardDescription>
                                Ti ln file backup  khi phc
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div>
                                <Label htmlFor="backup-file">Chn file backup</Label>
                                <Input
                                    id="backup-file"
                                    type="file"
                                    accept=".zip,.tar.gz,.sql"
                                    onChange={handleFileUpload}
                                    className="mt-2"
                                />
                                {uploadedFile && (
                                    <p className="text-sm text-green-600 mt-2">
                                         chn: {uploadedFile.name} ({(uploadedFile.size / (1024 * 1024)).toFixed(1)} MB)
                                    </p>
                                )}
                            </div>
                            <Button
                                onClick={importBackup}
                                disabled={!uploadedFile}
                                className="w-full"
                            >
                                <Import className="w-4 h-4 mr-2" />
                                Import Backup
                            </Button>
                        </CardContent>
                    </Card>

                    {/* Restore Progress */}
                    {restoreProgress.isRestoring && (
                        <Card>
                            <CardHeader>
                                <CardTitle>ang khi phc...</CardTitle>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div>
                                    <div className="flex justify-between mb-2">
                                        <span>Tin  tng th</span>
                                        <span>{Math.round(restoreProgress.progress)}%</span>
                                    </div>
                                    <Progress value={restoreProgress.progress} className="mb-4" />

                                    <div className="flex justify-between mb-2">
                                        <span>{restoreProgress.currentStep}</span>
                                        <span>{Math.round(restoreProgress.currentStepProgress)}%</span>
                                    </div>
                                    <Progress value={restoreProgress.currentStepProgress} />
                                </div>

                                <div className="bg-gray-50 rounded-lg p-4 max-h-40 overflow-y-auto">
                                    <h4 className="font-semibold mb-2">Logs:</h4>
                                    {restoreProgress.logs.map((log, index) => (
                                        <p key={index} className="text-sm text-gray-600">
                                            {new Date().toLocaleTimeString('vi-VN')}: {log}
                                        </p>
                                    ))}
                                </div>
                            </CardContent>
                        </Card>
                    )}

                    {/* Available Backups for Restore */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Chn backup  khi phc</CardTitle>
                            <CardDescription>
                                Chn file backup  khi phc d liu
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="space-y-3">
                                {backups
                                    .filter(backup => backup.status === 'completed')
                                    .map((backup) => (
                                        <div key={backup.id} className="border rounded-lg p-4 hover:bg-gray-50">
                                            <div className="flex items-center justify-between">
                                                <div className="flex-1">
                                                    <div className="flex items-center gap-3">
                                                        <h3 className="font-semibold">{backup.name}</h3>
                                                        {getTypeBadge(backup.type)}
                                                    </div>
                                                    <p className="text-sm text-gray-600 mt-1">{backup.description}</p>
                                                    <div className="flex items-center gap-4 text-sm text-gray-500 mt-2">
                                                        <span>Kch thc: {backup.size}</span>
                                                        <span>Ngy: {new Date(backup.createdAt).toLocaleDateString('vi-VN')}</span>
                                                    </div>
                                                </div>
                                                <Button
                                                    onClick={() => restoreFromBackup(backup.id)}
                                                    disabled={restoreProgress.isRestoring}
                                                    variant="outline"
                                                >
                                                    <RefreshCw className="w-4 h-4 mr-2" />
                                                    Khi phc
                                                </Button>
                                            </div>
                                        </div>
                                    ))}
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* Settings Tab */}
                <TabsContent value="settings" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Settings className="w-5 h-5" />
                                Ci t Backup
                            </CardTitle>
                            <CardDescription>
                                Cu hnh backup t ng v cc ty chn
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-6">
                            {/* Auto Backup */}
                            <div>
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Backup t ng</Label>
                                        <p className="text-sm text-gray-500">
                                            T ng to backup theo lch trnh
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.autoBackup}
                                        onCheckedChange={(checked) => updateSetting('autoBackup', checked)}
                                    />
                                </div>

                                {settings.autoBackup && (
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4 ml-6">
                                        <div>
                                            <Label htmlFor="frequency">Tn sut</Label>
                                            <Select
                                                value={settings.frequency}
                                                onValueChange={(value: any) => updateSetting('frequency', value)}
                                            >
                                                <SelectTrigger>
                                                    <SelectValue />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    <SelectItem value="daily">Hng ngy</SelectItem>
                                                    <SelectItem value="weekly">Hng tun</SelectItem>
                                                    <SelectItem value="monthly">Hng thng</SelectItem>
                                                </SelectContent>
                                            </Select>
                                        </div>
                                        <div>
                                            <Label htmlFor="retention">Thi gian lu tr (ngy)</Label>
                                            <Input
                                                id="retention"
                                                type="number"
                                                value={settings.retentionDays}
                                                onChange={(e) => updateSetting('retentionDays', parseInt(e.target.value))}
                                                min="1"
                                                max="365"
                                            />
                                        </div>
                                    </div>
                                )}
                            </div>

                            <Separator />

                            {/* Backup Content */}
                            <div>
                                <h3 className="font-semibold mb-4">Ni dung backup</h3>
                                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                                    <div className="flex items-center space-x-2">
                                        <Checkbox
                                            id="includePosts"
                                            checked={settings.includePosts}
                                            onCheckedChange={(checked) => updateSetting('includePosts', !!checked)}
                                        />
                                        <Label htmlFor="includePosts">Bi vit</Label>
                                    </div>
                                    <div className="flex items-center space-x-2">
                                        <Checkbox
                                            id="includeComments"
                                            checked={settings.includeComments}
                                            onCheckedChange={(checked) => updateSetting('includeComments', !!checked)}
                                        />
                                        <Label htmlFor="includeComments">Bnh lun</Label>
                                    </div>
                                    <div className="flex items-center space-x-2">
                                        <Checkbox
                                            id="includeMedia"
                                            checked={settings.includeMedia}
                                            onCheckedChange={(checked) => updateSetting('includeMedia', !!checked)}
                                        />
                                        <Label htmlFor="includeMedia">Media</Label>
                                    </div>
                                    <div className="flex items-center space-x-2">
                                        <Checkbox
                                            id="includeSettings"
                                            checked={settings.includeSettings}
                                            onCheckedChange={(checked) => updateSetting('includeSettings', !!checked)}
                                        />
                                        <Label htmlFor="includeSettings">Ci t</Label>
                                    </div>
                                    <div className="flex items-center space-x-2">
                                        <Checkbox
                                            id="includeUsers"
                                            checked={settings.includeUsers}
                                            onCheckedChange={(checked) => updateSetting('includeUsers', !!checked)}
                                        />
                                        <Label htmlFor="includeUsers">Ngi dng</Label>
                                    </div>
                                    <div className="flex items-center space-x-2">
                                        <Checkbox
                                            id="includeAnalytics"
                                            checked={settings.includeAnalytics}
                                            onCheckedChange={(checked) => updateSetting('includeAnalytics', !!checked)}
                                        />
                                        <Label htmlFor="includeAnalytics">Analytics</Label>
                                    </div>
                                </div>
                            </div>

                            <Separator />

                            {/* Compression & Security */}
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                <div>
                                    <Label htmlFor="compression">Mc nn</Label>
                                    <Select
                                        value={settings.compressionLevel}
                                        onValueChange={(value: any) => updateSetting('compressionLevel', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="none">Khng nn</SelectItem>
                                            <SelectItem value="low">Thp</SelectItem>
                                            <SelectItem value="medium">Trung bnh</SelectItem>
                                            <SelectItem value="high">Cao</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>

                                <div>
                                    <div className="flex items-center justify-between mb-2">
                                        <Label>M ha backup</Label>
                                        <Switch
                                            checked={settings.encryptBackups}
                                            onCheckedChange={(checked) => updateSetting('encryptBackups', checked)}
                                        />
                                    </div>
                                    {settings.encryptBackups && (
                                        <Input
                                            type="password"
                                            placeholder="Nhp kha m ha..."
                                            value={settings.encryptionKey}
                                            onChange={(e) => updateSetting('encryptionKey', e.target.value)}
                                        />
                                    )}
                                </div>
                            </div>

                            <Separator />

                            {/* Storage Location */}
                            <div>
                                <h3 className="font-semibold mb-4">V tr lu tr</h3>
                                <div className="space-y-4">
                                    <Select
                                        value={settings.backupLocation}
                                        onValueChange={(value: any) => updateSetting('backupLocation', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="local">My ch local</SelectItem>
                                            <SelectItem value="cloud">Cloud storage</SelectItem>
                                            <SelectItem value="both">C hai</SelectItem>
                                        </SelectContent>
                                    </Select>

                                    {(settings.backupLocation === 'cloud' || settings.backupLocation === 'both') && (
                                        <div className="space-y-4">
                                            <Select
                                                value={settings.cloudProvider}
                                                onValueChange={(value: any) => updateSetting('cloudProvider', value)}
                                            >
                                                <SelectTrigger>
                                                    <SelectValue />
                                                </SelectTrigger>
                                                <SelectContent>
                                                    <SelectItem value="aws">Amazon S3</SelectItem>
                                                    <SelectItem value="google">Google Cloud Storage</SelectItem>
                                                    <SelectItem value="azure">Azure Blob Storage</SelectItem>
                                                </SelectContent>
                                            </Select>

                                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                                <Input
                                                    placeholder="Access Key"
                                                    value={settings.cloudSettings.accessKey}
                                                    onChange={(e) => updateSetting('cloudSettings', {
                                                        ...settings.cloudSettings,
                                                        accessKey: e.target.value
                                                    })}
                                                />
                                                <Input
                                                    type="password"
                                                    placeholder="Secret Key"
                                                    value={settings.cloudSettings.secretKey}
                                                    onChange={(e) => updateSetting('cloudSettings', {
                                                        ...settings.cloudSettings,
                                                        secretKey: e.target.value
                                                    })}
                                                />
                                                <Input
                                                    placeholder="Bucket Name"
                                                    value={settings.cloudSettings.bucket}
                                                    onChange={(e) => updateSetting('cloudSettings', {
                                                        ...settings.cloudSettings,
                                                        bucket: e.target.value
                                                    })}
                                                />
                                                <Input
                                                    placeholder="Region"
                                                    value={settings.cloudSettings.region}
                                                    onChange={(e) => updateSetting('cloudSettings', {
                                                        ...settings.cloudSettings,
                                                        region: e.target.value
                                                    })}
                                                />
                                            </div>
                                        </div>
                                    )}
                                </div>
                            </div>

                            <div className="flex justify-end">
                                <Button onClick={saveSettings} disabled={saving}>
                                    {saving ? (
                                        <>
                                            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                                            ang lu...
                                        </>
                                    ) : (
                                        <>
                                            <Save className="w-4 h-4 mr-2" />
                                            Lu ci t
                                        </>
                                    )}
                                </Button>
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* History Tab */}
                <TabsContent value="history" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <History className="w-5 h-5" />
                                Lch s Backup & Restore
                            </CardTitle>
                            <CardDescription>
                                Theo di cc hot ng backup v restore
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="space-y-4">
                                {/* Mock history entries */}
                                <div className="border-l-4 border-green-500 pl-4 py-2">
                                    <div className="flex items-center justify-between">
                                        <div>
                                            <h4 className="font-semibold">Backup y  thnh cng</h4>
                                            <p className="text-sm text-gray-600">Full Backup - 2025-01-15</p>
                                        </div>
                                        <span className="text-sm text-gray-500">2 gi trc</span>
                                    </div>
                                </div>

                                <div className="border-l-4 border-blue-500 pl-4 py-2">
                                    <div className="flex items-center justify-between">
                                        <div>
                                            <h4 className="font-semibold">Khi phc thnh cng</h4>
                                            <p className="text-sm text-gray-600">Khi phc t Content Only - 2025-01-14</p>
                                        </div>
                                        <span className="text-sm text-gray-500">1 ngy trc</span>
                                    </div>
                                </div>

                                <div className="border-l-4 border-orange-500 pl-4 py-2">
                                    <div className="flex items-center justify-between">
                                        <div>
                                            <h4 className="font-semibold">Backup t ng  ln lch</h4>
                                            <p className="text-sm text-gray-600">Backup hng tun s chy vo 00:00</p>
                                        </div>
                                        <span className="text-sm text-gray-500">2 ngy trc</span>
                                    </div>
                                </div>

                                <div className="border-l-4 border-red-500 pl-4 py-2">
                                    <div className="flex items-center justify-between">
                                        <div>
                                            <h4 className="font-semibold">Backup tht bi</h4>
                                            <p className="text-sm text-gray-600">Li: Khng  dung lng lu tr</p>
                                        </div>
                                        <span className="text-sm text-gray-500">3 ngy trc</span>
                                    </div>
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    );
}