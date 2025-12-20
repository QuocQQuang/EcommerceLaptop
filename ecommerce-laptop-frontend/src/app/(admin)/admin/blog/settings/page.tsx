'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import {
    Globe,
    Mail,
    Monitor,
    Palette,
    Save,
    Settings,
    Shield
} from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

interface BlogSettings {
    // General Settings
    siteName: string;
    siteDescription: string;
    siteUrl: string;
    timezone: string;
    language: string;
    dateFormat: string;
    timeFormat: string;

    // Theme Settings
    theme: string;
    primaryColor: string;
    accentColor: string;
    fontFamily: string;
    logoUrl: string;
    faviconUrl: string;
    customCSS: string;

    // Content Settings
    postsPerPage: number;
    excerptLength: number;
    allowComments: boolean;
    moderateComments: boolean;
    allowRegistration: boolean;
    defaultUserRole: string;

    // SEO Settings
    enableSitemap: boolean;
    enableRobots: boolean;
    metaDescription: string;
    metaKeywords: string;
    googleAnalyticsId: string;
    googleTagManagerId: string;

    // Email Settings
    emailFrom: string;
    emailFromName: string;
    smtpHost: string;
    smtpPort: number;
    smtpUser: string;
    smtpPassword: string;
    enableEmailNotifications: boolean;

    // Security Settings
    enableCaptcha: boolean;
    captchaSiteKey: string;
    captchaSecretKey: string;
    enableTwoFactor: boolean;
    sessionTimeout: number;
    maxLoginAttempts: number;

    // Performance Settings
    enableCaching: boolean;
    cacheExpiry: number;
    enableCompression: boolean;
    enableCDN: boolean;
    cdnUrl: string;

    // Social Settings
    facebookUrl: string;
    twitterUrl: string;
    linkedinUrl: string;
    instagramUrl: string;
    youtubeUrl: string;

    // Advanced Settings
    enableApi: boolean;
    enableWebhooks: boolean;
    backupFrequency: string;
    debugMode: boolean;
    maintenanceMode: boolean;
    maintenanceMessage: string;
}

export default function BlogSettingsPage() {
    const [settings, setSettings] = useState<BlogSettings>({
        // General Settings
        siteName: 'My Blog',
        siteDescription: 'A professional blog platform',
        siteUrl: 'https://example.com',
        timezone: 'Asia/Ho_Chi_Minh',
        language: 'vi-VN',
        dateFormat: 'DD/MM/YYYY',
        timeFormat: '24',

        // Theme Settings
        theme: 'default',
        primaryColor: '#3b82f6',
        accentColor: '#10b981',
        fontFamily: 'Inter',
        logoUrl: '',
        faviconUrl: '',
        customCSS: '',

        // Content Settings
        postsPerPage: 10,
        excerptLength: 150,
        allowComments: true,
        moderateComments: true,
        allowRegistration: false,
        defaultUserRole: 'subscriber',

        // SEO Settings
        enableSitemap: true,
        enableRobots: true,
        metaDescription: '',
        metaKeywords: '',
        googleAnalyticsId: '',
        googleTagManagerId: '',

        // Email Settings
        emailFrom: '',
        emailFromName: '',
        smtpHost: '',
        smtpPort: 587,
        smtpUser: '',
        smtpPassword: '',
        enableEmailNotifications: true,

        // Security Settings
        enableCaptcha: false,
        captchaSiteKey: '',
        captchaSecretKey: '',
        enableTwoFactor: false,
        sessionTimeout: 3600,
        maxLoginAttempts: 5,

        // Performance Settings
        enableCaching: true,
        cacheExpiry: 3600,
        enableCompression: true,
        enableCDN: false,
        cdnUrl: '',

        // Social Settings
        facebookUrl: '',
        twitterUrl: '',
        linkedinUrl: '',
        instagramUrl: '',
        youtubeUrl: '',

        // Advanced Settings
        enableApi: true,
        enableWebhooks: false,
        backupFrequency: 'daily',
        debugMode: false,
        maintenanceMode: false,
        maintenanceMessage: 'Website is under maintenance. Please come back later.',
    });

    const [saving, setSaving] = useState(false);
    const [activeTab, setActiveTab] = useState('general');

    // Handle settings update
    const updateSetting = (key: keyof BlogSettings, value: any) => {
        setSettings(prev => ({
            ...prev,
            [key]: value
        }));
    };

    // Save settings
    const handleSave = async () => {
        try {
            setSaving(true);
            // Mock API call
            console.log('Saving blog settings:', settings);
            toast.success('Ci t  c lu thnh cng');
        } catch (error) {
            console.error('Failed to save settings:', error);
            toast.error('Khng th lu ci t');
        } finally {
            setSaving(false);
        }
    };

    // Reset settings
    const handleReset = () => {
        if (confirm('Bn c chc chn mun khi phc ci t mc nh?')) {
            // Reset to default values
            toast.success(' khi phc ci t mc nh');
        }
    };

    const themeOptions = [
        { value: 'default', label: 'Theme mc nh' },
        { value: 'dark', label: 'Theme ti' },
        { value: 'light', label: 'Theme sng' },
        { value: 'minimal', label: 'Theme ti gin' },
        { value: 'modern', label: 'Theme hin i' }
    ];

    const timezones = [
        { value: 'Asia/Ho_Chi_Minh', label: 'Vit Nam (GMT+7)' },
        { value: 'UTC', label: 'UTC (GMT+0)' },
        { value: 'America/New_York', label: 'New York (GMT-5)' },
        { value: 'Europe/London', label: 'London (GMT+0)' },
        { value: 'Asia/Tokyo', label: 'Tokyo (GMT+9)' }
    ];

    const languages = [
        { value: 'vi-VN', label: 'Ting Vit' },
        { value: 'en-US', label: 'English (US)' },
        { value: 'en-GB', label: 'English (UK)' },
        { value: 'zh-CN', label: ' ()' },
        { value: 'ja-JP', label: '' }
    ];

    const userRoles = [
        { value: 'subscriber', label: 'Ngi ng k' },
        { value: 'contributor', label: 'Cng tc vin' },
        { value: 'author', label: 'Tc gi' },
        { value: 'editor', label: 'Bin tp vin' }
    ];

    return (
        <div className="p-6 max-w-7xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Ci t Blog</h1>
                    <p className="text-gray-500">Qun l cu hnh v ty chnh blog ca bn</p>
                </div>
                <div className="flex gap-2">
                    <Button variant="outline" onClick={handleReset}>
                        Khi phc mc nh
                    </Button>
                    <Button onClick={handleSave} disabled={saving}>
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
            </div>

            {/* Settings Tabs */}
            <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
                <TabsList className="grid w-full grid-cols-7">
                    <TabsTrigger value="general">Chung</TabsTrigger>
                    <TabsTrigger value="theme">Giao din</TabsTrigger>
                    <TabsTrigger value="content">Ni dung</TabsTrigger>
                    <TabsTrigger value="seo">SEO</TabsTrigger>
                    <TabsTrigger value="email">Email</TabsTrigger>
                    <TabsTrigger value="security">Bo mt</TabsTrigger>
                    <TabsTrigger value="advanced">Nng cao</TabsTrigger>
                </TabsList>

                {/* General Settings Tab */}
                <TabsContent value="general" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Globe className="w-5 h-5" />
                                Thng tin chung
                            </CardTitle>
                            <CardDescription>
                                Cu hnh thng tin c bn ca blog
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="siteName">Tn blog *</Label>
                                    <Input
                                        id="siteName"
                                        value={settings.siteName}
                                        onChange={(e) => updateSetting('siteName', e.target.value)}
                                        placeholder="Nhp tn blog..."
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="siteUrl">URL blog *</Label>
                                    <Input
                                        id="siteUrl"
                                        value={settings.siteUrl}
                                        onChange={(e) => updateSetting('siteUrl', e.target.value)}
                                        placeholder="https://example.com"
                                    />
                                </div>
                            </div>

                            <div>
                                <Label htmlFor="siteDescription">M t blog</Label>
                                <Textarea
                                    id="siteDescription"
                                    value={settings.siteDescription}
                                    onChange={(e) => updateSetting('siteDescription', e.target.value)}
                                    placeholder="M t ngn v blog ca bn..."
                                    className="min-h-20"
                                />
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                <div>
                                    <Label htmlFor="timezone">Mi gi</Label>
                                    <Select
                                        value={settings.timezone}
                                        onValueChange={(value) => updateSetting('timezone', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {timezones.map((timezone) => (
                                                <SelectItem key={timezone.value} value={timezone.value}>
                                                    {timezone.label}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                                <div>
                                    <Label htmlFor="language">Ngn ng</Label>
                                    <Select
                                        value={settings.language}
                                        onValueChange={(value) => updateSetting('language', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {languages.map((language) => (
                                                <SelectItem key={language.value} value={language.value}>
                                                    {language.label}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                                <div>
                                    <Label htmlFor="dateFormat">nh dng ngy</Label>
                                    <Select
                                        value={settings.dateFormat}
                                        onValueChange={(value) => updateSetting('dateFormat', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="DD/MM/YYYY">DD/MM/YYYY</SelectItem>
                                            <SelectItem value="MM/DD/YYYY">MM/DD/YYYY</SelectItem>
                                            <SelectItem value="YYYY-MM-DD">YYYY-MM-DD</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle>Lin kt mng x hi</CardTitle>
                            <CardDescription>
                                Thm cc lin kt mng x hi ca blog
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="facebookUrl">Facebook</Label>
                                    <Input
                                        id="facebookUrl"
                                        value={settings.facebookUrl}
                                        onChange={(e) => updateSetting('facebookUrl', e.target.value)}
                                        placeholder="https://facebook.com/yourpage"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="twitterUrl">Twitter</Label>
                                    <Input
                                        id="twitterUrl"
                                        value={settings.twitterUrl}
                                        onChange={(e) => updateSetting('twitterUrl', e.target.value)}
                                        placeholder="https://twitter.com/youraccount"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="linkedinUrl">LinkedIn</Label>
                                    <Input
                                        id="linkedinUrl"
                                        value={settings.linkedinUrl}
                                        onChange={(e) => updateSetting('linkedinUrl', e.target.value)}
                                        placeholder="https://linkedin.com/company/yourcompany"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="instagramUrl">Instagram</Label>
                                    <Input
                                        id="instagramUrl"
                                        value={settings.instagramUrl}
                                        onChange={(e) => updateSetting('instagramUrl', e.target.value)}
                                        placeholder="https://instagram.com/youraccount"
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* Theme Settings Tab */}
                <TabsContent value="theme" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Palette className="w-5 h-5" />
                                Giao din v thit k
                            </CardTitle>
                            <CardDescription>
                                Ty chnh giao din v mu sc ca blog
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="theme">Theme</Label>
                                    <Select
                                        value={settings.theme}
                                        onValueChange={(value) => updateSetting('theme', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {themeOptions.map((theme) => (
                                                <SelectItem key={theme.value} value={theme.value}>
                                                    {theme.label}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                                <div>
                                    <Label htmlFor="fontFamily">Font ch</Label>
                                    <Select
                                        value={settings.fontFamily}
                                        onValueChange={(value) => updateSetting('fontFamily', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="Inter">Inter</SelectItem>
                                            <SelectItem value="Roboto">Roboto</SelectItem>
                                            <SelectItem value="Open Sans">Open Sans</SelectItem>
                                            <SelectItem value="Lato">Lato</SelectItem>
                                            <SelectItem value="Montserrat">Montserrat</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="primaryColor">Mu chnh</Label>
                                    <div className="flex gap-2">
                                        <Input
                                            id="primaryColor"
                                            type="color"
                                            value={settings.primaryColor}
                                            onChange={(e) => updateSetting('primaryColor', e.target.value)}
                                            className="w-16 h-10 p-1"
                                        />
                                        <Input
                                            value={settings.primaryColor}
                                            onChange={(e) => updateSetting('primaryColor', e.target.value)}
                                            placeholder="#3b82f6"
                                            className="flex-1"
                                        />
                                    </div>
                                </div>
                                <div>
                                    <Label htmlFor="accentColor">Mu ph</Label>
                                    <div className="flex gap-2">
                                        <Input
                                            id="accentColor"
                                            type="color"
                                            value={settings.accentColor}
                                            onChange={(e) => updateSetting('accentColor', e.target.value)}
                                            className="w-16 h-10 p-1"
                                        />
                                        <Input
                                            value={settings.accentColor}
                                            onChange={(e) => updateSetting('accentColor', e.target.value)}
                                            placeholder="#10b981"
                                            className="flex-1"
                                        />
                                    </div>
                                </div>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="logoUrl">Logo URL</Label>
                                    <Input
                                        id="logoUrl"
                                        value={settings.logoUrl}
                                        onChange={(e) => updateSetting('logoUrl', e.target.value)}
                                        placeholder="https://example.com/logo.png"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="faviconUrl">Favicon URL</Label>
                                    <Input
                                        id="faviconUrl"
                                        value={settings.faviconUrl}
                                        onChange={(e) => updateSetting('faviconUrl', e.target.value)}
                                        placeholder="https://example.com/favicon.ico"
                                    />
                                </div>
                            </div>

                            <div>
                                <Label htmlFor="customCSS">CSS ty chnh</Label>
                                <Textarea
                                    id="customCSS"
                                    value={settings.customCSS}
                                    onChange={(e) => updateSetting('customCSS', e.target.value)}
                                    placeholder="/* Thm CSS ty chnh ca bn  y */"
                                    className="min-h-32 font-mono text-sm"
                                />
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* Content Settings Tab */}
                <TabsContent value="content" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Monitor className="w-5 h-5" />
                                Ci t ni dung
                            </CardTitle>
                            <CardDescription>
                                Qun l cch hin th v tng tc vi ni dung
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="postsPerPage">S bi vit mi trang</Label>
                                    <Input
                                        id="postsPerPage"
                                        type="number"
                                        value={settings.postsPerPage}
                                        onChange={(e) => updateSetting('postsPerPage', parseInt(e.target.value))}
                                        min="1"
                                        max="50"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="excerptLength"> di tm tt (k t)</Label>
                                    <Input
                                        id="excerptLength"
                                        type="number"
                                        value={settings.excerptLength}
                                        onChange={(e) => updateSetting('excerptLength', parseInt(e.target.value))}
                                        min="50"
                                        max="500"
                                    />
                                </div>
                            </div>

                            <Separator />

                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Cho php bnh lun</Label>
                                        <p className="text-sm text-gray-500">
                                            Ngi dng c th bnh lun trn bi vit
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.allowComments}
                                        onCheckedChange={(checked) => updateSetting('allowComments', checked)}
                                    />
                                </div>

                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Duyt bnh lun</Label>
                                        <p className="text-sm text-gray-500">
                                            Bnh lun cn c duyt trc khi hin th
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.moderateComments}
                                        onCheckedChange={(checked) => updateSetting('moderateComments', checked)}
                                    />
                                </div>

                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Cho php ng k</Label>
                                        <p className="text-sm text-gray-500">
                                            Ngi dng c th t to ti khon
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.allowRegistration}
                                        onCheckedChange={(checked) => updateSetting('allowRegistration', checked)}
                                    />
                                </div>
                            </div>

                            {settings.allowRegistration && (
                                <div>
                                    <Label htmlFor="defaultUserRole">Vai tr mc nh</Label>
                                    <Select
                                        value={settings.defaultUserRole}
                                        onValueChange={(value) => updateSetting('defaultUserRole', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {userRoles.map((role) => (
                                                <SelectItem key={role.value} value={role.value}>
                                                    {role.label}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                            )}
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* SEO Settings Tab */}
                <TabsContent value="seo" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Globe className="w-5 h-5" />
                                Ci t SEO v Analytics
                            </CardTitle>
                            <CardDescription>
                                Ti u ha cho cng c tm kim v theo di Analytics
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div>
                                <Label htmlFor="metaDescription">M t meta mc nh</Label>
                                <Textarea
                                    id="metaDescription"
                                    value={settings.metaDescription}
                                    onChange={(e) => updateSetting('metaDescription', e.target.value)}
                                    placeholder="M t mc nh cho trang web..."
                                    className="min-h-20"
                                />
                            </div>

                            <div>
                                <Label htmlFor="metaKeywords">T kha meta mc nh</Label>
                                <Input
                                    id="metaKeywords"
                                    value={settings.metaKeywords}
                                    onChange={(e) => updateSetting('metaKeywords', e.target.value)}
                                    placeholder="t kha 1, t kha 2, t kha 3"
                                />
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="googleAnalyticsId">Google Analytics ID</Label>
                                    <Input
                                        id="googleAnalyticsId"
                                        value={settings.googleAnalyticsId}
                                        onChange={(e) => updateSetting('googleAnalyticsId', e.target.value)}
                                        placeholder="G-XXXXXXXXXX"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="googleTagManagerId">Google Tag Manager ID</Label>
                                    <Input
                                        id="googleTagManagerId"
                                        value={settings.googleTagManagerId}
                                        onChange={(e) => updateSetting('googleTagManagerId', e.target.value)}
                                        placeholder="GTM-XXXXXXX"
                                    />
                                </div>
                            </div>

                            <Separator />

                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>To Sitemap t ng</Label>
                                        <p className="text-sm text-gray-500">
                                            T ng to sitemap XML cho SEO
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.enableSitemap}
                                        onCheckedChange={(checked) => updateSetting('enableSitemap', checked)}
                                    />
                                </div>

                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>To robots.txt</Label>
                                        <p className="text-sm text-gray-500">
                                            T ng to file robots.txt
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.enableRobots}
                                        onCheckedChange={(checked) => updateSetting('enableRobots', checked)}
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* Email Settings Tab */}
                <TabsContent value="email" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Mail className="w-5 h-5" />
                                Ci t Email
                            </CardTitle>
                            <CardDescription>
                                Cu hnh SMTP v thng bo email
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="emailFrom">Email gi</Label>
                                    <Input
                                        id="emailFrom"
                                        type="email"
                                        value={settings.emailFrom}
                                        onChange={(e) => updateSetting('emailFrom', e.target.value)}
                                        placeholder="noreply@example.com"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="emailFromName">Tn ngi gi</Label>
                                    <Input
                                        id="emailFromName"
                                        value={settings.emailFromName}
                                        onChange={(e) => updateSetting('emailFromName', e.target.value)}
                                        placeholder="My Blog"
                                    />
                                </div>
                            </div>

                            <Separator />

                            <h4 className="font-semibold">Ci t SMTP</h4>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="md:col-span-2">
                                    <Label htmlFor="smtpHost">SMTP Host</Label>
                                    <Input
                                        id="smtpHost"
                                        value={settings.smtpHost}
                                        onChange={(e) => updateSetting('smtpHost', e.target.value)}
                                        placeholder="smtp.gmail.com"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="smtpPort">SMTP Port</Label>
                                    <Input
                                        id="smtpPort"
                                        type="number"
                                        value={settings.smtpPort}
                                        onChange={(e) => updateSetting('smtpPort', parseInt(e.target.value))}
                                        placeholder="587"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="smtpUser">SMTP Username</Label>
                                    <Input
                                        id="smtpUser"
                                        value={settings.smtpUser}
                                        onChange={(e) => updateSetting('smtpUser', e.target.value)}
                                        placeholder="username"
                                    />
                                </div>
                                <div className="md:col-span-2">
                                    <Label htmlFor="smtpPassword">SMTP Password</Label>
                                    <Input
                                        id="smtpPassword"
                                        type="password"
                                        value={settings.smtpPassword}
                                        onChange={(e) => updateSetting('smtpPassword', e.target.value)}
                                        placeholder=""
                                    />
                                </div>
                            </div>

                            <Separator />

                            <div className="flex items-center justify-between">
                                <div className="space-y-0.5">
                                    <Label>Thng bo email</Label>
                                    <p className="text-sm text-gray-500">
                                        Gi email thng bo cho cc s kin
                                    </p>
                                </div>
                                <Switch
                                    checked={settings.enableEmailNotifications}
                                    onCheckedChange={(checked) => updateSetting('enableEmailNotifications', checked)}
                                />
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* Security Settings Tab */}
                <TabsContent value="security" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Shield className="w-5 h-5" />
                                Ci t Bo mt
                            </CardTitle>
                            <CardDescription>
                                Bo v blog v ti khon ngi dng
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Kch hot CAPTCHA</Label>
                                        <p className="text-sm text-gray-500">
                                            S dng reCAPTCHA  chng spam
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.enableCaptcha}
                                        onCheckedChange={(checked) => updateSetting('enableCaptcha', checked)}
                                    />
                                </div>

                                {settings.enableCaptcha && (
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4 ml-6">
                                        <div>
                                            <Label htmlFor="captchaSiteKey">Site Key</Label>
                                            <Input
                                                id="captchaSiteKey"
                                                value={settings.captchaSiteKey}
                                                onChange={(e) => updateSetting('captchaSiteKey', e.target.value)}
                                                placeholder="6LeIxAcTAAAAAJcZVRqyHh71UMIEGNQ_MXjiZKhI"
                                            />
                                        </div>
                                        <div>
                                            <Label htmlFor="captchaSecretKey">Secret Key</Label>
                                            <Input
                                                id="captchaSecretKey"
                                                type="password"
                                                value={settings.captchaSecretKey}
                                                onChange={(e) => updateSetting('captchaSecretKey', e.target.value)}
                                                placeholder="6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe"
                                            />
                                        </div>
                                    </div>
                                )}

                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Xc thc 2 bc</Label>
                                        <p className="text-sm text-gray-500">
                                            Yu cu xc thc 2 bc cho admin
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.enableTwoFactor}
                                        onCheckedChange={(checked) => updateSetting('enableTwoFactor', checked)}
                                    />
                                </div>
                            </div>

                            <Separator />

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="sessionTimeout">Thi gian ht hn session (giy)</Label>
                                    <Input
                                        id="sessionTimeout"
                                        type="number"
                                        value={settings.sessionTimeout}
                                        onChange={(e) => updateSetting('sessionTimeout', parseInt(e.target.value))}
                                        min="300"
                                        max="86400"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="maxLoginAttempts">S ln ng nhp sai ti a</Label>
                                    <Input
                                        id="maxLoginAttempts"
                                        type="number"
                                        value={settings.maxLoginAttempts}
                                        onChange={(e) => updateSetting('maxLoginAttempts', parseInt(e.target.value))}
                                        min="3"
                                        max="10"
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* Advanced Settings Tab */}
                <TabsContent value="advanced" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Settings className="w-5 h-5" />
                                Ci t Nng cao
                            </CardTitle>
                            <CardDescription>
                                Cu hnh hiu nng, API v cc tnh nng nng cao
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-4">
                                <h4 className="font-semibold">Hiu nng</h4>
                                <div className="space-y-4 ml-4">
                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Kch hot Cache</Label>
                                            <p className="text-sm text-gray-500">
                                                Lu cache  tng tc  ti trang
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.enableCaching}
                                            onCheckedChange={(checked) => updateSetting('enableCaching', checked)}
                                        />
                                    </div>

                                    {settings.enableCaching && (
                                        <div>
                                            <Label htmlFor="cacheExpiry">Thi gian cache (giy)</Label>
                                            <Input
                                                id="cacheExpiry"
                                                type="number"
                                                value={settings.cacheExpiry}
                                                onChange={(e) => updateSetting('cacheExpiry', parseInt(e.target.value))}
                                                min="60"
                                                max="86400"
                                            />
                                        </div>
                                    )}

                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Nn Gzip</Label>
                                            <p className="text-sm text-gray-500">
                                                Nn ni dung  gim bng thng
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.enableCompression}
                                            onCheckedChange={(checked) => updateSetting('enableCompression', checked)}
                                        />
                                    </div>

                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>CDN</Label>
                                            <p className="text-sm text-gray-500">
                                                S dng Content Delivery Network
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.enableCDN}
                                            onCheckedChange={(checked) => updateSetting('enableCDN', checked)}
                                        />
                                    </div>

                                    {settings.enableCDN && (
                                        <div>
                                            <Label htmlFor="cdnUrl">CDN URL</Label>
                                            <Input
                                                id="cdnUrl"
                                                value={settings.cdnUrl}
                                                onChange={(e) => updateSetting('cdnUrl', e.target.value)}
                                                placeholder="https://cdn.example.com"
                                            />
                                        </div>
                                    )}
                                </div>
                            </div>

                            <Separator />

                            <div className="space-y-4">
                                <h4 className="font-semibold">API v Tch hp</h4>
                                <div className="space-y-4 ml-4">
                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>API REST</Label>
                                            <p className="text-sm text-gray-500">
                                                Cho php truy cp qua API
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.enableApi}
                                            onCheckedChange={(checked) => updateSetting('enableApi', checked)}
                                        />
                                    </div>

                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Webhooks</Label>
                                            <p className="text-sm text-gray-500">
                                                Gi thng bo qua webhook
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.enableWebhooks}
                                            onCheckedChange={(checked) => updateSetting('enableWebhooks', checked)}
                                        />
                                    </div>
                                </div>
                            </div>

                            <Separator />

                            <div className="space-y-4">
                                <h4 className="font-semibold">Bo tr v Debug</h4>
                                <div className="space-y-4 ml-4">
                                    <div>
                                        <Label htmlFor="backupFrequency">Tn sut backup</Label>
                                        <Select
                                            value={settings.backupFrequency}
                                            onValueChange={(value) => updateSetting('backupFrequency', value)}
                                        >
                                            <SelectTrigger>
                                                <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectItem value="hourly">Hng gi</SelectItem>
                                                <SelectItem value="daily">Hng ngy</SelectItem>
                                                <SelectItem value="weekly">Hng tun</SelectItem>
                                                <SelectItem value="monthly">Hng thng</SelectItem>
                                                <SelectItem value="never">Khng backup</SelectItem>
                                            </SelectContent>
                                        </Select>
                                    </div>

                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Ch  Debug</Label>
                                            <p className="text-sm text-gray-500">
                                                Hin th thng tin debug cho developer
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.debugMode}
                                            onCheckedChange={(checked) => updateSetting('debugMode', checked)}
                                        />
                                    </div>

                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Ch  bo tr</Label>
                                            <p className="text-sm text-gray-500">
                                                Tm kha truy cp cho ngi dng
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.maintenanceMode}
                                            onCheckedChange={(checked) => updateSetting('maintenanceMode', checked)}
                                        />
                                    </div>

                                    {settings.maintenanceMode && (
                                        <div>
                                            <Label htmlFor="maintenanceMessage">Thng bo bo tr</Label>
                                            <Textarea
                                                id="maintenanceMessage"
                                                value={settings.maintenanceMessage}
                                                onChange={(e) => updateSetting('maintenanceMessage', e.target.value)}
                                                placeholder="Trang web ang bo tr..."
                                                className="min-h-20"
                                            />
                                        </div>
                                    )}
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    );
}