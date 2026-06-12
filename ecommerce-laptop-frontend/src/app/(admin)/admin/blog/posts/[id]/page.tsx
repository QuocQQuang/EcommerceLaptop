'use client';

import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';
import { blogService } from '@/services/blogService';
import { Blog, BlogCategory, BlogStatus, BlogTag, UpdateBlogRequest } from '@/types/api';
import {
    Calendar,
    Clock,
    Eye,
    Image as ImageIcon,
    Loader2,
    Save,
    Send,
    Tag,
    Trash2,
    X
} from 'lucide-react';
import dynamic from 'next/dynamic';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import 'react-quill-new/dist/quill.snow.css';

// Direct dynamic import using react-quill-new (React 19 compatible)
const ReactQuill = dynamic(
    () => import('react-quill-new').then(mod => mod.default),
    {
        ssr: false,
        loading: () => (
            <div className="h-96 bg-gray-50 border border-gray-200 rounded-lg flex items-center justify-center">
                <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
            </div>
        )
    }
) as any;

interface BlogEditorState {
    blog: Blog | null;
    originalBlog: Blog | null;
    categories: BlogCategory[];
    tags: BlogTag[];
    selectedTags: number[];
    loading: boolean;
    saving: boolean;
    publishing: boolean;
    autoSaving: boolean;
    deleting: boolean;
    errors: Record<string, string>;
}

export default function BlogEditPage() {
    const params = useParams<{ id: string }>();
    const id = params?.id?.toString() ?? '';
    const router = useRouter();
    const [state, setState] = useState<BlogEditorState>({
        blog: null,
        originalBlog: null,
        categories: [],
        tags: [],
        selectedTags: [],
        loading: true,
        saving: false,
        publishing: false,
        autoSaving: false,
        deleting: false,
        errors: {},
    });

    // Quill editor configuration
    const quillModules = useMemo(() => ({
        toolbar: [
            [{ 'header': [1, 2, 3, false] }],
            ['bold', 'italic', 'underline', 'strike'],
            [{ 'color': [] }, { 'background': [] }],
            [{ 'list': 'ordered' }, { 'list': 'bullet' }],
            [{ 'indent': '-1' }, { 'indent': '+1' }],
            [{ 'align': [] }],
            ['blockquote', 'code-block'],
            ['link', 'image', 'video'],
            ['clean']
        ],
        clipboard: {
            matchVisual: false,
        }
    }), []);

    const quillFormats = [
        'header', 'bold', 'italic', 'underline', 'strike',
        'color', 'background', 'list', 'indent',
        'align', 'blockquote', 'code-block', 'link', 'image', 'video'
    ];

    // Load initial data
    useEffect(() => {
        const loadData = async () => {
            try {
                const [blogResponse, categoriesResponse, tagsResponse] = await Promise.all([
                    blogService.getBlogById(Number(id)),
                    blogService.getAllCategories(),
                    blogService.getAllTags(),
                ]);

                setState(prev => ({
                    ...prev,
                    blog: blogResponse,
                    originalBlog: { ...blogResponse },
                    categories: categoriesResponse,
                    tags: tagsResponse,
                    selectedTags: (blogResponse.tagIds || blogResponse.tags?.map(tag => tag.id)) ?? [],
                    loading: false,
                }));
            } catch (error) {
                console.error('Failed to load data:', error);
                setState(prev => ({ ...prev, loading: false }));
            }
        };

        loadData();
    }, [id]);

    // Auto-save functionality
    useEffect(() => {
        if (!state.blog || !state.originalBlog) return;

        const hasChanges = JSON.stringify(state.blog) !== JSON.stringify(state.originalBlog);

        if (hasChanges && !state.saving && !state.publishing) {
            const autoSaveTimer = setTimeout(() => {
                autoSave();
            }, 30000); // Auto-save every 30 seconds

            return () => clearTimeout(autoSaveTimer);
        }
    }, [state.blog, state.originalBlog]);

    // Update blog field
    const updateBlog = (field: string, value: any) => {
        setState(prev => ({
            ...prev,
            blog: prev.blog ? {
                ...prev.blog,
                [field]: value,
            } : null,
            errors: {
                ...prev.errors,
                [field]: '',
            }
        }));
    };

    // Handle tag selection
    const handleTagSelect = (tagId: number) => {
        setState(prev => {
            const newSelectedTags = prev.selectedTags.includes(tagId)
                ? prev.selectedTags.filter(id => id !== tagId)
                : [...prev.selectedTags, tagId];

            return {
                ...prev,
                selectedTags: newSelectedTags,
                blog: prev.blog ? {
                    ...prev.blog,
                    tagIds: newSelectedTags,
                } : null
            };
        });
    };

    // Add new tag
    const handleAddTag = async (tagName: string) => {
        if (!tagName.trim()) return;

        try {
            const newTag = await blogService.createTag({ name: tagName.trim() });
            setState(prev => ({
                ...prev,
                tags: [...prev.tags, newTag],
                selectedTags: [...prev.selectedTags, newTag.id],
                blog: prev.blog ? {
                    ...prev.blog,
                    tagIds: [...(prev.blog.tagIds as number[] || []), newTag.id],
                } : null
            }));
        } catch (error) {
            console.error('Failed to create tag:', error);
        }
    };

    // Validation
    const validateBlog = (): boolean => {
        if (!state.blog) return false;

        const errors: Record<string, string> = {};

        if (!state.blog.title?.trim()) {
            errors.title = 'Tiu  l bt buc';
        }

        if (!state.blog.content?.trim()) {
            errors.content = 'Ni dung l bt buc';
        }

        if (!state.blog.slug?.trim()) {
            errors.slug = 'Slug l bt buc';
        }

        setState(prev => ({ ...prev, errors }));
        return Object.keys(errors).length === 0;
    };

    // Auto-save
    const autoSave = async () => {
        if (!state.blog || !validateBlog()) return;

        try {
            setState(prev => ({ ...prev, autoSaving: true }));

            const updateData: UpdateBlogRequest = {
                title: state.blog.title,
                slug: state.blog.slug,
                excerpt: state.blog.excerpt,
                content: state.blog.content,
                featuredImageUrl: state.blog.featuredImageUrl,
                categoryId: state.blog.categoryId,
                tagIds: state.selectedTags.map(Number),
                // isFeatured: state.blog.isFeatured, // Temporarily disabled
            };

            const updatedBlog = await blogService.updateBlog(state.blog.id, updateData);
            setState(prev => ({
                ...prev,
                blog: updatedBlog,
                originalBlog: { ...updatedBlog }
            }));
        } catch (error) {
            console.error('Auto-save failed:', error);
        } finally {
            setState(prev => ({ ...prev, autoSaving: false }));
        }
    };

    // Save blog
    const saveBlog = async () => {
        if (!state.blog || !validateBlog()) return;

        try {
            setState(prev => ({ ...prev, saving: true }));

            const updateData: UpdateBlogRequest = {
                title: state.blog.title,
                slug: state.blog.slug,
                excerpt: state.blog.excerpt,
                content: state.blog.content,
                featuredImageUrl: state.blog.featuredImageUrl,
                categoryId: state.blog.categoryId,
                tagIds: state.selectedTags.map(Number),
                // isFeatured: state.blog.isFeatured, // Temporarily disabled
            };

            const updatedBlog = await blogService.updateBlog(state.blog.id, updateData);
            setState(prev => ({
                ...prev,
                blog: updatedBlog,
                originalBlog: { ...updatedBlog }
            }));
        } catch (error) {
            console.error('Failed to save blog:', error);
        } finally {
            setState(prev => ({ ...prev, saving: false }));
        }
    };

    // Publish blog
    const publishBlog = async () => {
        if (!state.blog || !validateBlog()) return;

        try {
            setState(prev => ({ ...prev, publishing: true }));

            const updateData: UpdateBlogRequest = {
                title: state.blog.title,
                slug: state.blog.slug,
                excerpt: state.blog.excerpt,
                content: state.blog.content,
                featuredImageUrl: state.blog.featuredImageUrl,
                categoryId: state.blog.categoryId,
                tagIds: state.selectedTags.map(Number),
                // isFeatured: state.blog.isFeatured, // Temporarily disabled
                status: BlogStatus.Published,
                publishedAt: new Date().toISOString(),
            };

            const publishedBlog = await blogService.updateBlog(state.blog.id, updateData);
            setState(prev => ({
                ...prev,
                blog: publishedBlog,
                originalBlog: { ...publishedBlog }
            }));
        } catch (error) {
            console.error('Failed to publish blog:', error);
        } finally {
            setState(prev => ({ ...prev, publishing: false }));
        }
    };

    // Delete blog
    const deleteBlog = async () => {
        if (!state.blog) return;

        try {
            setState(prev => ({ ...prev, deleting: true }));
            await blogService.deleteBlog(state.blog.id);
            router.push('/admin/blog/posts');
        } catch (error) {
            console.error('Failed to delete blog:', error);
            setState(prev => ({ ...prev, deleting: false }));
        }
    };

    // Unpublish blog
    const unpublishBlog = async () => {
        if (!state.blog) return;

        try {
            setState(prev => ({ ...prev, publishing: true }));
            const unpublishedBlog = await blogService.unpublishBlog(state.blog.id);
            setState(prev => ({
                ...prev,
                blog: unpublishedBlog,
                originalBlog: { ...unpublishedBlog }
            }));
        } catch (error) {
            console.error('Failed to unpublish blog:', error);
        } finally {
            setState(prev => ({ ...prev, publishing: false }));
        }
    };

    // Calculate reading time (client-side, not in DB)
    const readingTime = useMemo(() => {
        if (!state.blog?.content) return 0;
        return blogService.calculateReadingTime(state.blog.content);
    }, [state.blog?.content]);

    // Check if there are unsaved changes
    const hasUnsavedChanges = useMemo(() => {
        if (!state.blog || !state.originalBlog) return false;
        return JSON.stringify(state.blog) !== JSON.stringify(state.originalBlog);
    }, [state.blog, state.originalBlog]);

    if (state.loading) {
        return (
            <div className="p-6">
                <div className="flex items-center justify-center h-64">
                    <Loader2 className="w-8 h-8 animate-spin" />
                </div>
            </div>
        );
    }

    if (!state.blog) {
        return (
            <div className="p-6">
                <div className="text-center">
                    <h2 className="text-xl font-semibold text-gray-900 mb-2">Khng tm thy bi vit</h2>
                    <p className="text-gray-500 mb-4">Bi vit bn ang tm kim khng tn ti.</p>
                    <Button onClick={() => router.push('/admin/blog/posts')}>
                        Quay li danh sách
                    </Button>
                </div>
            </div>
        );
    }

    return (
        <div className="p-6 max-w-6xl mx-auto">
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                {/* Main Editor */}
                <div className="lg:col-span-2 space-y-6">
                    {/* Header */}
                    <div className="flex items-center justify-between">
                        <div>
                            <h1 className="text-2xl font-bold text-gray-900">Chỉnh sửa bài viết</h1>
                            {hasUnsavedChanges && (
                                <p className="text-sm text-orange-600 mt-1">
                                    * C thay i cha c lu
                                </p>
                            )}
                        </div>
                        <div className="flex items-center gap-2">
                            {state.autoSaving && (
                                <span className="text-sm text-gray-500 flex items-center gap-1">
                                    <Loader2 className="w-4 h-4 animate-spin" />
                                    ang lu...
                                </span>
                            )}
                            <Button
                                variant="outline"
                                onClick={() => router.push(`/blog/${state.blog?.slug}`)}
                            >
                                <Eye className="w-4 h-4 mr-2" />
                                Xem trc
                            </Button>
                            <Button
                                variant="outline"
                                onClick={saveBlog}
                                disabled={state.saving || state.publishing}
                            >
                                {state.saving && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                                <Save className="w-4 h-4 mr-2" />
                                Lu
                            </Button>
                            {state.blog.status !== BlogStatus.Published && (
                                <Button
                                    onClick={publishBlog}
                                    disabled={state.saving || state.publishing}
                                >
                                    {state.publishing && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                                    <Send className="w-4 h-4 mr-2" />
                                    Xut bn
                                </Button>
                            )}
                            {state.blog.status === BlogStatus.Published && (
                                <Button
                                    onClick={unpublishBlog}
                                    disabled={state.saving || state.publishing}
                                    variant="outline"
                                >
                                    {state.publishing && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                                    n bi vit
                                </Button>
                            )}
                            <AlertDialog>
                                <AlertDialogTrigger asChild>
                                    <Button variant="destructive" size="sm">
                                        <Trash2 className="w-4 h-4" />
                                    </Button>
                                </AlertDialogTrigger>
                                <AlertDialogContent>
                                    <AlertDialogHeader>
                                        <AlertDialogTitle>Xa bi vit</AlertDialogTitle>
                                        <AlertDialogDescription>
                                            Bn c chc chn mun xa bi vit "{state.blog.title}"?
                                            Hnh ng ny khng th hon tc.
                                        </AlertDialogDescription>
                                    </AlertDialogHeader>
                                    <AlertDialogFooter>
                                        <AlertDialogCancel>Hy</AlertDialogCancel>
                                        <AlertDialogAction
                                            onClick={deleteBlog}
                                            disabled={state.deleting}
                                            className="bg-red-600 hover:bg-red-700"
                                        >
                                            {state.deleting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                                            Xa
                                        </AlertDialogAction>
                                    </AlertDialogFooter>
                                </AlertDialogContent>
                            </AlertDialog>
                        </div>
                    </div>

                    {/* Status and Meta Info */}
                    <Card>
                        <CardContent className="pt-6">
                            <div className="flex items-center gap-4 text-sm text-gray-600">
                                <div className="flex items-center gap-1">
                                    <span>Trng thi:</span>
                                    <Badge variant={
                                        state.blog.status === BlogStatus.Published ? 'default' :
                                            state.blog.status === BlogStatus.Draft ? 'secondary' : 'destructive'
                                    }>
                                        {state.blog.status === BlogStatus.Published ? ' xut bn' :
                                            state.blog.status === BlogStatus.Draft ? 'Bn nhp' : ' n'}
                                    </Badge>
                                </div>
                                {state.blog.publishedAt && (
                                    <div className="flex items-center gap-1">
                                        <Calendar className="w-4 h-4" />
                                        <span>Xut bn: {new Date(state.blog.publishedAt).toLocaleDateString('vi-VN')}</span>
                                    </div>
                                )}
                                <div className="flex items-center gap-1">
                                    <Eye className="w-4 h-4" />
                                    <span>Lt xem: {state.blog.viewCount || 0}</span>
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Title */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Tiu  v Slug</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div>
                                <Label htmlFor="title">Tiu  bi vit *</Label>
                                <Input
                                    id="title"
                                    value={state.blog.title || ''}
                                    onChange={(e) => updateBlog('title', e.target.value)}
                                    placeholder="Nhp tiu  bi vit..."
                                    className={cn(state.errors.title && "border-red-500")}
                                />
                                {state.errors.title && (
                                    <p className="text-sm text-red-600 mt-1">{state.errors.title}</p>
                                )}
                            </div>

                            <div>
                                <Label htmlFor="slug">Slug URL *</Label>
                                <Input
                                    id="slug"
                                    value={state.blog.slug || ''}
                                    onChange={(e) => updateBlog('slug', e.target.value)}
                                    placeholder="slug-url-bai-viet"
                                    className={cn(state.errors.slug && "border-red-500")}
                                />
                                {state.errors.slug && (
                                    <p className="text-sm text-red-600 mt-1">{state.errors.slug}</p>
                                )}
                                <p className="text-xs text-gray-500 mt-1">
                                    URL: /blog/{state.blog.slug || 'slug-url'}
                                </p>
                            </div>

                            <div>
                                <Label htmlFor="excerpt">Tm tt</Label>
                                <Textarea
                                    id="excerpt"
                                    value={state.blog.excerpt || ''}
                                    onChange={(e) => updateBlog('excerpt', e.target.value)}
                                    placeholder="Tm tt ngn v bi vit..."
                                    rows={3}
                                />
                            </div>
                        </CardContent>
                    </Card>

                    {/* Content Editor */}
                    <Card>
                        <CardHeader>
                            <div className="flex items-center justify-between">
                                <CardTitle>Ni dung bi vit *</CardTitle>
                                {readingTime > 0 && (
                                    <Badge variant="outline" className="flex items-center gap-1">
                                        <Clock className="w-3 h-3" />
                                        {readingTime} pht c
                                    </Badge>
                                )}
                            </div>
                        </CardHeader>
                        <CardContent>
                            <div className={cn("border rounded-lg", state.errors.content && "border-red-500")}>
                                <ReactQuill
                                    value={state.blog.content || ''}
                                    onChange={(content: string) => updateBlog('content', content)}
                                    modules={quillModules}
                                    formats={quillFormats}
                                    placeholder="Vit ni dung bi vit ca bn  y..."
                                    style={{ minHeight: '400px' }}
                                />
                            </div>
                            {state.errors.content && (
                                <p className="text-sm text-red-600 mt-1">{state.errors.content}</p>
                            )}
                        </CardContent>
                    </Card>
                </div>

                {/* Sidebar - Same as new blog editor */}
                <div className="space-y-6">
                    {/* Featured Image */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <ImageIcon className="w-5 h-5" />
                                nh i din
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div>
                                <Label htmlFor="featuredImage">URL nh</Label>
                                <Input
                                    id="featuredImage"
                                    value={state.blog.featuredImageUrl || ''}
                                    onChange={(e) => updateBlog('featuredImageUrl', e.target.value)}
                                    placeholder="https://example.com/image.jpg"
                                />
                            </div>

                            {state.blog.featuredImageUrl && (
                                <div>
                                    <img
                                        src={state.blog.featuredImageUrl}
                                        alt="Preview"
                                        className="w-full h-32 object-cover rounded-lg"
                                    />
                                </div>
                            )}
                        </CardContent>
                    </Card>

                    {/* Category */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Danh mục</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <Select
                                value={state.blog.categoryId?.toString() || '0'}
                                onValueChange={(value) => updateBlog('categoryId', value === '0' ? undefined : parseInt(value))}
                            >
                                <SelectTrigger>
                                    <SelectValue placeholder="Chn danh mục" />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="0">Không danh mục</SelectItem>
                                    {state.categories.map(category => (
                                        <SelectItem key={category.id} value={category.id.toString()}>
                                            {category.name}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </CardContent>
                    </Card>

                    {/* Tags */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Tag className="w-5 h-5" />
                                Tags
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            {/* Selected Tags */}
                            {state.selectedTags.length > 0 && (
                                <div className="flex flex-wrap gap-2">
                                    {state.selectedTags.map(tagId => {
                                        const tag = state.tags.find(t => t.id === Number(tagId));
                                        if (!tag) return null;

                                        return (
                                            <Badge
                                                key={tagId}
                                                variant="secondary"
                                                className="flex items-center gap-1"
                                            >
                                                {tag.name}
                                                <X
                                                    className="w-3 h-3 cursor-pointer"
                                                    onClick={() => handleTagSelect(Number(tagId))}
                                                />
                                            </Badge>
                                        );
                                    })}
                                </div>
                            )}

                            {/* Available Tags */}
                            <div className="space-y-2">
                                <Label>Chn tags c sn</Label>
                                <div className="flex flex-wrap gap-2 max-h-32 overflow-y-auto">
                                    {state.tags
                                        .filter(tag => !state.selectedTags.includes(tag.id))
                                        .map(tag => (
                                            <Badge
                                                key={tag.id}
                                                variant="outline"
                                                className="cursor-pointer hover:bg-blue-50"
                                                onClick={() => handleTagSelect(tag.id)}
                                            >
                                                {tag.name}
                                            </Badge>
                                        ))
                                    }
                                </div>
                            </div>

                            {/* Add New Tag */}
                            <div>
                                <Label>Thêm tag mới</Label>
                                <div className="flex gap-2 mt-1">
                                    <Input
                                        placeholder="Tn tag..."
                                        onKeyPress={(e) => {
                                            if (e.key === 'Enter') {
                                                handleAddTag((e.target as HTMLInputElement).value);
                                                (e.target as HTMLInputElement).value = '';
                                            }
                                        }}
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Settings - Temporarily hide featured posts until homepage section is implemented */}
                    {/* <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Settings className="w-5 h-5" />
                                Ci t
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center justify-between">
                                <Label htmlFor="isFeatured">Bi vit ni bt</Label>
                                <Switch
                                    id="isFeatured"
                                    checked={state.blog.isFeatured}
                                    onCheckedChange={(checked) => updateBlog('isFeatured', checked)}
                                />
                            </div>
                        </CardContent>
                    </Card> */}
                </div>
            </div>
        </div>
    );
}