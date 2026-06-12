'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Progress } from '@/components/ui/progress';
import { Separator } from '@/components/ui/separator';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';
import {
    BarChart,
    CheckCircle,
    Eye,
    Facebook,
    Hash,
    Image,
    Search,
    Smartphone,
    Target,
    Twitter,
    XCircle
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';

interface SEOData {
    title: string;
    description: string;
    keywords: string[];
    canonicalUrl: string;
    ogTitle: string;
    ogDescription: string;
    ogImage: string;
    twitterTitle: string;
    twitterDescription: string;
    twitterImage: string;
    twitterCard: 'summary' | 'summary_large_image';
    focusKeyword: string;
    slug: string;
}

interface SEOScore {
    overall: number;
    details: {
        titleLength: { score: number; message: string; status: 'good' | 'warning' | 'error' };
        descriptionLength: { score: number; message: string; status: 'good' | 'warning' | 'error' };
        keywordUsage: { score: number; message: string; status: 'good' | 'warning' | 'error' };
        slugOptimization: { score: number; message: string; status: 'good' | 'warning' | 'error' };
        imageAlt: { score: number; message: string; status: 'good' | 'warning' | 'error' };
        internalLinks: { score: number; message: string; status: 'good' | 'warning' | 'error' };
        readability: { score: number; message: string; status: 'good' | 'warning' | 'error' };
        socialMeta: { score: number; message: string; status: 'good' | 'warning' | 'error' };
    };
}

interface BlogSEOPageProps {
    postId?: string;
    initialData?: Partial<SEOData>;
}

export default function BlogSEOPage({ postId, initialData }: BlogSEOPageProps) {
    const [seoData, setSeoData] = useState<SEOData>({
        title: '',
        description: '',
        keywords: [],
        canonicalUrl: '',
        ogTitle: '',
        ogDescription: '',
        ogImage: '',
        twitterTitle: '',
        twitterDescription: '',
        twitterImage: '',
        twitterCard: 'summary_large_image',
        focusKeyword: '',
        slug: '',
        ...initialData
    });

    const [keywordInput, setKeywordInput] = useState('');
    const [seoScore, setSeoScore] = useState<SEOScore | null>(null);
    const [analyzing, setAnalyzing] = useState(false);
    const [saving, setSaving] = useState(false);

    // Calculate SEO Score
    const calculateSEOScore = useCallback((data: SEOData): SEOScore => {
        const checks = {
            titleLength: {
                score: data.title.length >= 30 && data.title.length <= 60 ? 100 :
                    data.title.length >= 20 && data.title.length <= 70 ? 70 : 30,
                message: data.title.length >= 30 && data.title.length <= 60
                    ? ' di tiu  ti u (30-60 k t)'
                    : data.title.length > 70
                        ? 'Tiu  qu di, c th b ct trong kt qu tm kim'
                        : 'Tiu  qu ngn, nn c 30-60 k t',
                status: (data.title.length >= 30 && data.title.length <= 60 ? 'good' :
                    data.title.length >= 20 && data.title.length <= 70 ? 'warning' : 'error') as 'good' | 'warning' | 'error'
            },

            descriptionLength: {
                score: data.description.length >= 120 && data.description.length <= 160 ? 100 :
                    data.description.length >= 100 && data.description.length <= 180 ? 70 : 30,
                message: data.description.length >= 120 && data.description.length <= 160
                    ? ' di m t ti u (120-160 k t)'
                    : data.description.length > 180
                        ? 'M t qu di, c th b ct trong kt qu tm kim'
                        : 'M t qu ngn, nn c 120-160 k t',
                status: (data.description.length >= 120 && data.description.length <= 160 ? 'good' :
                    data.description.length >= 100 && data.description.length <= 180 ? 'warning' : 'error') as 'good' | 'warning' | 'error'
            },

            keywordUsage: {
                score: data.focusKeyword && (
                    data.title.toLowerCase().includes(data.focusKeyword.toLowerCase()) ||
                    data.description.toLowerCase().includes(data.focusKeyword.toLowerCase())
                ) ? 100 : data.focusKeyword ? 50 : 0,
                message: !data.focusKeyword
                    ? 'Cha t t kha chnh'
                    : (data.title.toLowerCase().includes(data.focusKeyword.toLowerCase()) ||
                        data.description.toLowerCase().includes(data.focusKeyword.toLowerCase()))
                        ? 'T kha chnh c s dng trong tiu  hoc m t'
                        : 'T kha chnh cha c s dng trong tiu  hoc m t',
                status: (!data.focusKeyword ? 'error' :
                    (data.title.toLowerCase().includes(data.focusKeyword.toLowerCase()) ||
                        data.description.toLowerCase().includes(data.focusKeyword.toLowerCase())) ? 'good' : 'warning') as 'good' | 'warning' | 'error'
            },

            slugOptimization: {
                score: data.slug && /^[a-z0-9-]+$/.test(data.slug) && data.slug.length <= 75 ? 100 :
                    data.slug && data.slug.length <= 100 ? 70 : 30,
                message: !data.slug
                    ? 'Cha c URL slug'
                    : /^[a-z0-9-]+$/.test(data.slug) && data.slug.length <= 75
                        ? 'URL slug ti u'
                        : 'URL slug nên chứa chữ thường, số và dấu gạch ngang',
                status: (!data.slug ? 'error' :
                    /^[a-z0-9-]+$/.test(data.slug) && data.slug.length <= 75 ? 'good' : 'warning') as 'good' | 'warning' | 'error'
            },

            imageAlt: {
                score: data.ogImage ? 100 : 0,
                message: data.ogImage ? ' c hnh nh i din' : 'Cha c hnh nh i din',
                status: (data.ogImage ? 'good' : 'warning') as 'good' | 'warning' | 'error'
            },

            internalLinks: {
                score: 80, // Mock score
                message: 'Cn kim tra lin kt ni b trong ni dung',
                status: 'good' as 'good' | 'warning' | 'error'
            },

            readability: {
                score: 85, // Mock score
                message: 'Kh nng c hiu tt',
                status: 'good' as 'good' | 'warning' | 'error'
            },

            socialMeta: {
                score: (data.ogTitle && data.ogDescription ? 100 :
                    (data.ogTitle || data.ogDescription ? 50 : 0)),
                message: (data.ogTitle && data.ogDescription)
                    ? ' ti u cho mng x hi'
                    : (data.ogTitle || data.ogDescription)
                        ? 'Thiu mt s th meta mng x hi'
                        : 'Cha ti u cho mng x hi',
                status: ((data.ogTitle && data.ogDescription) ? 'good' :
                    (data.ogTitle || data.ogDescription) ? 'warning' : 'error') as 'good' | 'warning' | 'error'
            }
        };

        const overall = Math.round(
            Object.values(checks).reduce((sum, check) => sum + check.score, 0) / Object.keys(checks).length
        );

        return { overall, details: checks };
    }, []);

    // Update SEO score when data changes
    useEffect(() => {
        const score = calculateSEOScore(seoData);
        setSeoScore(score);
    }, [seoData, calculateSEOScore]);

    // Handle keyword management
    const addKeyword = () => {
        if (keywordInput.trim() && !seoData.keywords.includes(keywordInput.trim())) {
            setSeoData(prev => ({
                ...prev,
                keywords: [...prev.keywords, keywordInput.trim()]
            }));
            setKeywordInput('');
        }
    };

    const removeKeyword = (keyword: string) => {
        setSeoData(prev => ({
            ...prev,
            keywords: prev.keywords.filter(k => k !== keyword)
        }));
    };

    // Auto-fill Open Graph and Twitter data from basic data
    const autoFillSocialMeta = () => {
        setSeoData(prev => ({
            ...prev,
            ogTitle: prev.ogTitle || prev.title,
            ogDescription: prev.ogDescription || prev.description,
            twitterTitle: prev.twitterTitle || prev.title,
            twitterDescription: prev.twitterDescription || prev.description,
            twitterImage: prev.twitterImage || prev.ogImage
        }));
        toast.success(' t ng in thng tin mng x hi');
    };

    // Generate slug from title
    const generateSlug = () => {
        const slug = seoData.title
            .toLowerCase()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '') // Remove accents
            .replace(/[^a-z0-9\s-]/g, '') // Remove special characters
            .trim()
            .replace(/\s+/g, '-') // Replace spaces with hyphens
            .replace(/-+/g, '-'); // Replace multiple hyphens with single hyphen

        setSeoData(prev => ({ ...prev, slug }));
        toast.success(' to URL slug t ng');
    };

    // Save SEO data
    const handleSave = async () => {
        try {
            setSaving(true);
            // Mock API call
            console.log('Saving SEO data:', seoData);
            toast.success(' lu thng tin SEO');
        } catch (error) {
            console.error('Failed to save SEO data:', error);
            toast.error('Khng th lu thng tin SEO');
        } finally {
            setSaving(false);
        }
    };

    // Analyze content for SEO
    const analyzeContent = async () => {
        try {
            setAnalyzing(true);
            // Mock analysis
            setTimeout(() => {
                setAnalyzing(false);
                toast.success(' phn tch SEO hon tt');
            }, 2000);
        } catch (error) {
            console.error('Failed to analyze content:', error);
            setAnalyzing(false);
            toast.error('Khng th phn tch ni dung');
        }
    };

    const getStatusIcon = (status: 'good' | 'warning' | 'error') => {
        switch (status) {
            case 'good':
                return <CheckCircle className="w-4 h-4 text-green-600" />;
            case 'warning':
                return <Target className="w-4 h-4 text-yellow-600" />;
            case 'error':
                return <XCircle className="w-4 h-4 text-red-600" />;
        }
    };

    const getScoreColor = (score: number) => {
        if (score >= 80) return 'text-green-600';
        if (score >= 60) return 'text-yellow-600';
        return 'text-red-600';
    };

    const getProgressColor = (score: number) => {
        if (score >= 80) return 'bg-green-500';
        if (score >= 60) return 'bg-yellow-500';
        return 'bg-red-500';
    };

    return (
        <div className="p-6 max-w-7xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Ti u SEO</h1>
                    <p className="text-gray-500">Ti u ha ni dung cho cc cng c tm kim v mng x hi</p>
                </div>
                <div className="flex gap-2">
                    <Button variant="outline" onClick={analyzeContent} disabled={analyzing}>
                        {analyzing ? (
                            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-600 mr-2" />
                        ) : (
                            <BarChart className="w-4 h-4 mr-2" />
                        )}
                        Phn tch SEO
                    </Button>
                    <Button onClick={handleSave} disabled={saving}>
                        {saving ? (
                            <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                        ) : null}
                        Lưu cài đặt
                    </Button>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* SEO Score Card */}
                <div className="lg:col-span-1">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Target className="w-5 h-5" />
                                im SEO
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-6">
                            {/* Overall Score */}
                            <div className="text-center">
                                <div className={cn("text-4xl font-bold mb-2", getScoreColor(seoScore?.overall || 0))}>
                                    {seoScore?.overall || 0}/100
                                </div>
                                <Progress
                                    value={seoScore?.overall || 0}
                                    className="h-2"
                                />
                                <p className="text-sm text-gray-500 mt-2">
                                    {(seoScore?.overall || 0) >= 80 ? 'Tuyt vi!' :
                                        (seoScore?.overall || 0) >= 60 ? 'Kh tt' : 'Cần cải thiện'}
                                </p>
                            </div>

                            <Separator />

                            {/* Detailed Scores */}
                            <div className="space-y-3">
                                <h4 className="font-semibold">Chi tit nh gi</h4>
                                {seoScore && Object.entries(seoScore.details).map(([key, check]) => (
                                    <div key={key} className="flex items-start gap-3">
                                        {getStatusIcon(check.status)}
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center justify-between">
                                                <span className="text-sm font-medium capitalize">
                                                    {key.replace(/([A-Z])/g, ' $1').toLowerCase()}
                                                </span>
                                                <span className={cn("text-xs font-bold", getScoreColor(check.score))}>
                                                    {check.score}
                                                </span>
                                            </div>
                                            <p className="text-xs text-gray-500 mt-1">{check.message}</p>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </CardContent>
                    </Card>
                </div>

                {/* SEO Configuration */}
                <div className="lg:col-span-2">
                    <Tabs defaultValue="basic" className="space-y-6">
                        <TabsList className="grid w-full grid-cols-4">
                            <TabsTrigger value="basic">C bn</TabsTrigger>
                            <TabsTrigger value="social">Mng x hi</TabsTrigger>
                            <TabsTrigger value="advanced">Nng cao</TabsTrigger>
                            <TabsTrigger value="preview">Xem trc</TabsTrigger>
                        </TabsList>

                        {/* Basic SEO Tab */}
                        <TabsContent value="basic" className="space-y-6">
                            <Card>
                                <CardHeader>
                                    <CardTitle>Thng tin SEO c bn</CardTitle>
                                    <CardDescription>
                                        Cu hnh cc thng tin SEO c bn cho bi vit
                                    </CardDescription>
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    <div>
                                        <Label htmlFor="title">Tiu  SEO *</Label>
                                        <Input
                                            id="title"
                                            value={seoData.title}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, title: e.target.value }))}
                                            placeholder="Nhp tiu  ti u SEO..."
                                        />
                                        <p className="text-xs text-gray-500 mt-1">
                                            {seoData.title.length}/60 k t (ti u: 30-60)
                                        </p>
                                    </div>

                                    <div>
                                        <Label htmlFor="description">M t SEO *</Label>
                                        <Textarea
                                            id="description"
                                            value={seoData.description}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, description: e.target.value }))}
                                            placeholder="Nhp m t hp dn cho kt qu tm kim..."
                                            className="min-h-20"
                                        />
                                        <p className="text-xs text-gray-500 mt-1">
                                            {seoData.description.length}/160 k t (ti u: 120-160)
                                        </p>
                                    </div>

                                    <div>
                                        <Label htmlFor="slug">URL Slug</Label>
                                        <div className="flex gap-2">
                                            <Input
                                                id="slug"
                                                value={seoData.slug}
                                                onChange={(e) => setSeoData(prev => ({ ...prev, slug: e.target.value }))}
                                                placeholder="url-slug-bai-viet"
                                            />
                                            <Button
                                                type="button"
                                                variant="outline"
                                                onClick={generateSlug}
                                                disabled={!seoData.title}
                                            >
                                                T ng
                                            </Button>
                                        </div>
                                        <p className="text-xs text-gray-500 mt-1">
                                            Chỉ sử dụng chữ thường, số và dấu gạch ngang
                                        </p>
                                    </div>

                                    <div>
                                        <Label htmlFor="focusKeyword">T kha chnh</Label>
                                        <Input
                                            id="focusKeyword"
                                            value={seoData.focusKeyword}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, focusKeyword: e.target.value }))}
                                            placeholder="t kha chnh"
                                        />
                                    </div>

                                    <div>
                                        <Label htmlFor="keywords">T kha ph</Label>
                                        <div className="flex gap-2">
                                            <Input
                                                id="keywords"
                                                value={keywordInput}
                                                onChange={(e) => setKeywordInput(e.target.value)}
                                                placeholder="Thêm từ khóa..."
                                                onKeyPress={(e) => e.key === 'Enter' && (e.preventDefault(), addKeyword())}
                                            />
                                            <Button type="button" variant="outline" onClick={addKeyword}>
                                                <Hash className="w-4 h-4" />
                                            </Button>
                                        </div>
                                        {seoData.keywords.length > 0 && (
                                            <div className="flex flex-wrap gap-2 mt-2">
                                                {seoData.keywords.map((keyword) => (
                                                    <Badge
                                                        key={keyword}
                                                        variant="secondary"
                                                        className="cursor-pointer"
                                                        onClick={() => removeKeyword(keyword)}
                                                    >
                                                        {keyword} 
                                                    </Badge>
                                                ))}
                                            </div>
                                        )}
                                    </div>

                                    <div>
                                        <Label htmlFor="canonicalUrl">Canonical URL</Label>
                                        <Input
                                            id="canonicalUrl"
                                            value={seoData.canonicalUrl}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, canonicalUrl: e.target.value }))}
                                            placeholder="https://example.com/bai-viet"
                                        />
                                    </div>
                                </CardContent>
                            </Card>
                        </TabsContent>

                        {/* Social Media Tab */}
                        <TabsContent value="social" className="space-y-6">
                            <Card>
                                <CardHeader>
                                    <CardTitle className="flex items-center gap-2">
                                        <Facebook className="w-5 h-5" />
                                        Open Graph (Facebook)
                                    </CardTitle>
                                    <CardDescription>
                                        Ti u hin th khi chia s trn Facebook v cc nn tng khc
                                    </CardDescription>
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    <div className="flex justify-end">
                                        <Button
                                            type="button"
                                            variant="outline"
                                            size="sm"
                                            onClick={autoFillSocialMeta}
                                        >
                                            T ng in t thng tin c bn
                                        </Button>
                                    </div>

                                    <div>
                                        <Label htmlFor="ogTitle">Tiu  OG</Label>
                                        <Input
                                            id="ogTitle"
                                            value={seoData.ogTitle}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, ogTitle: e.target.value }))}
                                            placeholder="Tiu  hin th khi chia s..."
                                        />
                                    </div>

                                    <div>
                                        <Label htmlFor="ogDescription">M t OG</Label>
                                        <Textarea
                                            id="ogDescription"
                                            value={seoData.ogDescription}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, ogDescription: e.target.value }))}
                                            placeholder="M t hin th khi chia s..."
                                            className="min-h-20"
                                        />
                                    </div>

                                    <div>
                                        <Label htmlFor="ogImage">Hnh nh OG</Label>
                                        <Input
                                            id="ogImage"
                                            value={seoData.ogImage}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, ogImage: e.target.value }))}
                                            placeholder="https://example.com/image.jpg"
                                        />
                                        <p className="text-xs text-gray-500 mt-1">
                                            Kch thc khuyn ngh: 1200x630px
                                        </p>
                                    </div>
                                </CardContent>
                            </Card>

                            <Card>
                                <CardHeader>
                                    <CardTitle className="flex items-center gap-2">
                                        <Twitter className="w-5 h-5" />
                                        Twitter Card
                                    </CardTitle>
                                    <CardDescription>
                                        Ti u hin th khi chia s trn Twitter
                                    </CardDescription>
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    <div>
                                        <Label htmlFor="twitterTitle">Tiu  Twitter</Label>
                                        <Input
                                            id="twitterTitle"
                                            value={seoData.twitterTitle}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, twitterTitle: e.target.value }))}
                                            placeholder="Tiu  Twitter Card..."
                                        />
                                    </div>

                                    <div>
                                        <Label htmlFor="twitterDescription">M t Twitter</Label>
                                        <Textarea
                                            id="twitterDescription"
                                            value={seoData.twitterDescription}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, twitterDescription: e.target.value }))}
                                            placeholder="M t Twitter Card..."
                                            className="min-h-20"
                                        />
                                    </div>

                                    <div>
                                        <Label htmlFor="twitterImage">Hnh nh Twitter</Label>
                                        <Input
                                            id="twitterImage"
                                            value={seoData.twitterImage}
                                            onChange={(e) => setSeoData(prev => ({ ...prev, twitterImage: e.target.value }))}
                                            placeholder="https://example.com/image.jpg"
                                        />
                                    </div>
                                </CardContent>
                            </Card>
                        </TabsContent>

                        {/* Advanced Tab */}
                        <TabsContent value="advanced" className="space-y-6">
                            <Card>
                                <CardHeader>
                                    <CardTitle>Cài đặt nâng cao</CardTitle>
                                    <CardDescription>
                                        Cc ty chn SEO nng cao cho chuyn gia
                                    </CardDescription>
                                </CardHeader>
                                <CardContent className="space-y-4">
                                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                        <div className="p-4 border rounded-lg">
                                            <h4 className="font-semibold mb-2">Schema Markup</h4>
                                            <p className="text-sm text-gray-600 mb-2">
                                                T ng to structured data cho bi vit
                                            </p>
                                            <Button variant="outline" size="sm">
                                                Cấu hình Schema
                                            </Button>
                                        </div>

                                        <div className="p-4 border rounded-lg">
                                            <h4 className="font-semibold mb-2">Sitemap</h4>
                                            <p className="text-sm text-gray-600 mb-2">
                                                Quản lý sitemap XML cho blog
                                            </p>
                                            <Button variant="outline" size="sm">
                                                To Sitemap
                                            </Button>
                                        </div>

                                        <div className="p-4 border rounded-lg">
                                            <h4 className="font-semibold mb-2">Robot.txt</h4>
                                            <p className="text-sm text-gray-600 mb-2">
                                                Cu hnh robots.txt cho SEO
                                            </p>
                                            <Button variant="outline" size="sm">
                                                Chỉnh sửa Robots
                                            </Button>
                                        </div>

                                        <div className="p-4 border rounded-lg">
                                            <h4 className="font-semibold mb-2">Analytics</h4>
                                            <p className="text-sm text-gray-600 mb-2">
                                                Tch hp Google Analytics & Search Console
                                            </p>
                                            <Button variant="outline" size="sm">
                                                Kt ni Analytics
                                            </Button>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        </TabsContent>

                        {/* Preview Tab */}
                        <TabsContent value="preview" className="space-y-6">
                            <Card>
                                <CardHeader>
                                    <CardTitle className="flex items-center gap-2">
                                        <Eye className="w-5 h-5" />
                                        Xem trc kt qu tm kim
                                    </CardTitle>
                                    <CardDescription>
                                        Xem cch bi vit hin th trn cc nn tng
                                    </CardDescription>
                                </CardHeader>
                                <CardContent className="space-y-6">
                                    {/* Google Search Preview */}
                                    <div>
                                        <h4 className="font-semibold mb-4 flex items-center gap-2">
                                            <Search className="w-4 h-4" />
                                            Google Search
                                        </h4>
                                        <div className="border rounded-lg p-4 bg-white">
                                            <div className="text-blue-600 text-lg hover:underline cursor-pointer">
                                                {seoData.title || 'Tiu  bi vit...'}
                                            </div>
                                            <div className="text-green-700 text-sm mt-1">
                                                {seoData.canonicalUrl || 'https://example.com/bai-viet'}
                                            </div>
                                            <div className="text-gray-600 text-sm mt-2">
                                                {seoData.description || 'M t bi vit s hin th  y...'}
                                            </div>
                                        </div>
                                    </div>

                                    {/* Facebook Preview */}
                                    <div>
                                        <h4 className="font-semibold mb-4 flex items-center gap-2">
                                            <Facebook className="w-4 h-4" />
                                            Facebook Share
                                        </h4>
                                        <div className="border rounded-lg overflow-hidden bg-white max-w-md">
                                            {seoData.ogImage && (
                                                <div className="aspect-video bg-gray-200 flex items-center justify-center">
                                                    <img
                                                        src={seoData.ogImage}
                                                        alt="OG Image"
                                                        className="w-full h-full object-cover"
                                                        onError={(e) => {
                                                            (e.target as HTMLImageElement).style.display = 'none';
                                                        }}
                                                    />
                                                    <Image className="w-8 h-8 text-gray-400" />
                                                </div>
                                            )}
                                            <div className="p-3">
                                                <div className="text-gray-500 text-xs uppercase">
                                                    example.com
                                                </div>
                                                <div className="font-semibold mt-1">
                                                    {seoData.ogTitle || seoData.title || 'Tiu  bi vit...'}
                                                </div>
                                                <div className="text-gray-600 text-sm mt-1">
                                                    {seoData.ogDescription || seoData.description || 'M t bi vit...'}
                                                </div>
                                            </div>
                                        </div>
                                    </div>

                                    {/* Mobile Preview */}
                                    <div>
                                        <h4 className="font-semibold mb-4 flex items-center gap-2">
                                            <Smartphone className="w-4 h-4" />
                                            Mobile View
                                        </h4>
                                        <div className="border rounded-lg p-4 bg-gray-100 max-w-xs">
                                            <div className="bg-white rounded p-3">
                                                <div className="text-blue-600 text-sm font-medium">
                                                    {seoData.title || 'Tiu ...'}
                                                </div>
                                                <div className="text-green-600 text-xs mt-1">
                                                    example.com
                                                </div>
                                                <div className="text-gray-600 text-xs mt-2 line-clamp-2">
                                                    {seoData.description || 'M t...'}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        </TabsContent>
                    </Tabs>
                </div>
            </div>
        </div>
    );
}