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
            toast.success('Cài đặt đã được lưu thành công');
        } catch (error) {
            console.error('Failed to save settings:', error);
            toast.error('Không thể lưu cài đặt');
        } finally {
            setSaving(false);
        }
    };

    // Reset settings
    const handleReset = () => {
        if (confirm('Bạn có chắc chắn muốn khôi phục cài đặt mặc định?')) {
            // Reset to default values
            toast.success('Đã khôi phục cài đặt mặc định');
        }
    };

    const themeOptions = [
        { value: 'default', label: 'Theme mặc định' },
        { value: 'dark', label: 'Theme tối' },
        { value: 'light', label: 'Theme sáng' },
        { value: 'minimal', label: 'Theme tối giản' },
        { value: 'modern', label: 'Theme hiện đại' }
    ];

    const timezones = [
        { value: 'Asia/Ho_Chi_Minh', label: 'Việt Nam (GMT+7)' },
        { value: 'UTC', label: 'UTC (GMT+0)' },
        { value: 'America/New_York', label: 'New York (GMT-5)' },
        { value: 'Europe/London', label: 'London (GMT+0)' },
        { value: 'Asia/Tokyo', label: 'Tokyo (GMT+9)' }
    ];

    const languages = [
        { value: 'vi-VN', label: 'Tiếng Việt' },
        { value: 'en-US', label: 'English (US)' },
        { value: 'en-GB', label: 'English (UK)' },
        { value: 'zh-CN', label: ' ()' },
        { value: 'ja-JP', label: '' }
    ];

    const userRoles = [
        { value: 'subscriber', label: 'Người đăng ký' },
        { value: 'contributor', label: 'Cộng tác viên' },
        { value: 'author', label: 'Tác giả' },
        { value: 'editor', label: 'Biên tập viên' }
    ];

    return (
        <div className="p-6 max-w-7xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Cài đặt Blog</h1>
                    <p className="text-gray-500">Quản lý cấu hình và tùy chỉnh blog của bạn</p>
                </div>
                <div className="flex gap-2">
                    <Button variant="outline" onClick={handleReset}>
                        Khôi phục mặc định
                    </Button>
                    <Button onClick={handleSave} disabled={saving}>
                        {saving ? (
                            <>
                                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                                đang lưu...
                            </>
                        ) : (
                            <>
                                <Save className="w-4 h-4 mr-2" />
                                Lưu cài đặt
                            </>
                        )}
                    </Button>
                </div>
            </div>

            {/* Settings Tabs */}
            <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
                <TabsList className="grid w-full grid-cols-7">
                    <TabsTrigger value="general">Chung</TabsTrigger>
                    <TabsTrigger value="theme">Giao diện</TabsTrigger>
                    <TabsTrigger value="content">Nội dung</TabsTrigger>
                    <TabsTrigger value="seo">SEO</TabsTrigger>
                    <TabsTrigger value="email">Email</TabsTrigger>
                    <TabsTrigger value="security">Bảo mật</TabsTrigger>
                    <TabsTrigger value="advanced">Nâng cao</TabsTrigger>
                </TabsList>

                {/* General Settings Tab */}
                <TabsContent value="general" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Globe className="w-5 h-5" />
                                Thông tin chung
                            </CardTitle>
                            <CardDescription>
                                Cấu hình thông tin cơ bản của blog
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="siteName">Tên blog *</Label>
                                    <Input
                                        id="siteName"
                                        value={settings.siteName}
                                        onChange={(e) => updateSetting('siteName', e.target.value)}
                                        placeholder="Nhập tên blog..."
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
                                <Label htmlFor="siteDescription">Mô tả blog</Label>
                                <Textarea
                                    id="siteDescription"
                                    value={settings.siteDescription}
                                    onChange={(e) => updateSetting('siteDescription', e.target.value)}
                                    placeholder="Mô tả ngắn về blog của bạn..."
                                    className="min-h-20"
                                />
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                <div>
                                    <Label htmlFor="timezone">Múi giờ</Label>
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
                                    <Label htmlFor="language">Ngôn ngữ</Label>
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
                                    <Label htmlFor="dateFormat">Định dạng ngày</Label>
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
                            <CardTitle>Liên kết mạng xã hội</CardTitle>
                            <CardDescription>
                                Thêm các liên kết mạng xã hội của blog
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
                                Giao diện và thiết kế
                            </CardTitle>
                            <CardDescription>
                                Tùy chỉnh giao diện và màu sắc của blog
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
                                    <Label htmlFor="fontFamily">Font chữ</Label>
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
                                    <Label htmlFor="primaryColor">Màu chính</Label>
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
                                    <Label htmlFor="accentColor">Màu phụ</Label>
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
                                <Label htmlFor="customCSS">CSS tùy chỉnh</Label>
                                <Textarea
                                    id="customCSS"
                                    value={settings.customCSS}
                                    onChange={(e) => updateSetting('customCSS', e.target.value)}
                                    placeholder="                                /* Thêm CSS tùy chỉnh của bạn ở đây */"
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
                                Cài đặt nội dung
                            </CardTitle>
                            <CardDescription>
                                Quản lý cách hiển thị và tương tác với nội dung
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="postsPerPage">Số bài viết mỗi trang</Label>
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
                                    <Label htmlFor="excerptLength">Độ dài tóm tắt (ký tự)</Label>
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
                                        <Label>Cho phép bình luận</Label>
                                        <p className="text-sm text-gray-500">
                                            Người dùng có thể bình luận trên bài viết
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.allowComments}
                                        onCheckedChange={(checked) => updateSetting('allowComments', checked)}
                                    />
                                </div>

                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Duyệt bình luận</Label>
                                        <p className="text-sm text-gray-500">
                                            Bình luận cần được duyệt trước khi hiển thị
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.moderateComments}
                                        onCheckedChange={(checked) => updateSetting('moderateComments', checked)}
                                    />
                                </div>

                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Cho phép đăng ký</Label>
                                        <p className="text-sm text-gray-500">
                                            Người dùng có thể tự tạo tài khoản
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
                                    <Label htmlFor="defaultUserRole">Vai trò mặc định</Label>
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
                                Cài đặt SEO và Analytics
                            </CardTitle>
                            <CardDescription>
                                Tối ưu hóa cho công cụ tìm kiếm và theo dõi Analytics
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div>
                                <Label htmlFor="metaDescription">Mô tả meta mặc định</Label>
                                <Textarea
                                    id="metaDescription"
                                    value={settings.metaDescription}
                                    onChange={(e) => updateSetting('metaDescription', e.target.value)}
                                    placeholder="Mô tả mặc định cho trang web..."
                                    className="min-h-20"
                                />
                            </div>

                            <div>
                                <Label htmlFor="metaKeywords">Từ khóa meta mặc định</Label>
                                <Input
                                    id="metaKeywords"
                                    value={settings.metaKeywords}
                                    onChange={(e) => updateSetting('metaKeywords', e.target.value)}
                                    placeholder="từ khóa 1, từ khóa 2, từ khóa 3"
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
                                        <Label>Tạo Sitemap tự động</Label>
                                        <p className="text-sm text-gray-500">
                                            Tự động tạo sitemap XML cho SEO
                                        </p>
                                    </div>
                                    <Switch
                                        checked={settings.enableSitemap}
                                        onCheckedChange={(checked) => updateSetting('enableSitemap', checked)}
                                    />
                                </div>

                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Tạo robots.txt</Label>
                                        <p className="text-sm text-gray-500">
                                            Tự động tạo file robots.txt
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
                                Cài đặt Email
                            </CardTitle>
                            <CardDescription>
                                Cấu hình SMTP và thông báo email
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="emailFrom">Email gửi</Label>
                                    <Input
                                        id="emailFrom"
                                        type="email"
                                        value={settings.emailFrom}
                                        onChange={(e) => updateSetting('emailFrom', e.target.value)}
                                        placeholder="noreply@example.com"
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="emailFromName">Tên người gửi</Label>
                                    <Input
                                        id="emailFromName"
                                        value={settings.emailFromName}
                                        onChange={(e) => updateSetting('emailFromName', e.target.value)}
                                        placeholder="My Blog"
                                    />
                                </div>
                            </div>

                            <Separator />

                            <h4 className="font-semibold">Cài đặt SMTP</h4>
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
                                    <Label>Thông báo email</Label>
                                    <p className="text-sm text-gray-500">
                                        Gửi email thông báo cho các sự kiện
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
                                Cài đặt Bảo mật
                            </CardTitle>
                            <CardDescription>
                                Bảo vệ blog và tài khoản người dùng
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Kích hoạt CAPTCHA</Label>
                                        <p className="text-sm text-gray-500">
                                            Sử dụng reCAPTCHA để chống spam
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
                                        <Label>Xác thực 2 bước</Label>
                                        <p className="text-sm text-gray-500">
                                            Yêu cầu xác thực 2 bước cho admin
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
                                    <Label htmlFor="sessionTimeout">Thời gian hết hạn session (giây)</Label>
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
                                    <Label htmlFor="maxLoginAttempts">Số lần đăng nhập sai tối đa</Label>
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
                                Cài đặt Nâng cao
                            </CardTitle>
                            <CardDescription>
                                Cấu hình hiệu năng, API và các tính năng nâng cao
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-4">
                                <h4 className="font-semibold">Hiệu năng</h4>
                                <div className="space-y-4 ml-4">
                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Kích hoạt Cache</Label>
                                            <p className="text-sm text-gray-500">
                                                Lưu cache để tăng tốc độ tải trang
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.enableCaching}
                                            onCheckedChange={(checked) => updateSetting('enableCaching', checked)}
                                        />
                                    </div>

                                    {settings.enableCaching && (
                                        <div>
                                            <Label htmlFor="cacheExpiry">Thời gian cache (giây)</Label>
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
                                            <Label>Nén Gzip</Label>
                                            <p className="text-sm text-gray-500">
                                                Nén nội dung để giảm băng thông
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
                                                Sử dụng Content Delivery Network
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
                                <h4 className="font-semibold">API và Tích hợp</h4>
                                <div className="space-y-4 ml-4">
                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>API REST</Label>
                                            <p className="text-sm text-gray-500">
                                                Cho phép truy cập qua API
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
                                                Gửi thông báo qua webhook
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
                                <h4 className="font-semibold">Bảo trì và Debug</h4>
                                <div className="space-y-4 ml-4">
                                    <div>
                                        <Label htmlFor="backupFrequency">Tần suất backup</Label>
                                        <Select
                                            value={settings.backupFrequency}
                                            onValueChange={(value) => updateSetting('backupFrequency', value)}
                                        >
                                            <SelectTrigger>
                                                <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectItem value="hourly">Hàng giờ</SelectItem>
                                                <SelectItem value="daily">Hàng ngày</SelectItem>
                                                <SelectItem value="weekly">Hàng tuần</SelectItem>
                                                <SelectItem value="monthly">Hàng tháng</SelectItem>
                                                <SelectItem value="never">Không backup</SelectItem>
                                            </SelectContent>
                                        </Select>
                                    </div>

                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Chế độ Debug</Label>
                                            <p className="text-sm text-gray-500">
                                                Hiển thị thông tin debug cho developer
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.debugMode}
                                            onCheckedChange={(checked) => updateSetting('debugMode', checked)}
                                        />
                                    </div>

                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Chế độ bảo trì</Label>
                                            <p className="text-sm text-gray-500">
                                                Tạm khóa truy cập cho người dùng
                                            </p>
                                        </div>
                                        <Switch
                                            checked={settings.maintenanceMode}
                                            onCheckedChange={(checked) => updateSetting('maintenanceMode', checked)}
                                        />
                                    </div>

                                    {settings.maintenanceMode && (
                                        <div>
                                            <Label htmlFor="maintenanceMessage">Thông báo bảo trì</Label>
                                            <Textarea
                                                id="maintenanceMessage"
                                                value={settings.maintenanceMessage}
                                                onChange={(e) => updateSetting('maintenanceMessage', e.target.value)}
                                                placeholder="Trang web đang bảo trì..."
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
