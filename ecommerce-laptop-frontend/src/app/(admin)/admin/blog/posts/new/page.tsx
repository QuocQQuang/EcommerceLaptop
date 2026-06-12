'use client';

import ProductLinkPicker from '@/components/blog/ProductLinkPicker';
import UnsplashImagePicker from '@/components/blog/UnsplashImagePicker';
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
import { Switch } from '@/components/ui/switch';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';
import { blogService } from '@/services/blogService';
import { BlogCategory, CreateBlogRequest } from '@/types/api';
import {
    Clock,
    Image as ImageIcon,
    Loader2,
    Package,
    Save,
    Send,
    Settings
} from 'lucide-react';
import dynamic from 'next/dynamic';
import { useRouter } from 'next/navigation';
import { useEffect, useMemo, useRef, useState } from 'react';
import 'react-quill-new/dist/quill.snow.css';

// ReactQuill editor (React 19 compatible)
const ReactQuill = dynamic(
    () => import('react-quill-new').then(mod => mod.default),
    { ssr: false }
) as any;

interface BlogEditorState {
    blog: Partial<CreateBlogRequest>;
    categories: BlogCategory[];
    loading: boolean;
    saving: boolean;
    publishing: boolean;
    autoSaving: boolean;
    errors: Record<string, string>;
}

export default function NewBlogPage() {
    const router = useRouter();
    const quillRef = useRef<any>(null);
    const [quillInstance, setQuillInstance] = useState<any>(null);
    const [isQuillReady, setIsQuillReady] = useState(false);
    const [state, setState] = useState<BlogEditorState>({
        blog: {
            title: '',
            excerpt: '',
            content: '',
            featuredImageUrl: '',
            metaTitle: '',
            metaDescription: '',
            isPublished: false,
            isFeatured: false,
            categoryId: 0
        },
        categories: [],
        loading: true,
        saving: false,
        publishing: false,
        autoSaving: false,
        errors: {},
    });
    // Quill editor configuration with custom handlers
    const quillModules = useMemo(() => ({
        toolbar: {
            container: [
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
            handlers: {
                'image': () => {
                    // This will be handled by our custom image picker
                    console.log('Image button clicked');
                }
            }
        },
        clipboard: {
            matchVisual: true, // Allow HTML paste
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
                const categoriesResponse = await blogService.getAllCategories();

                setState(prev => ({
                    ...prev,
                    categories: categoriesResponse,
                    loading: false,
                }));
            } catch (error) {
                console.error('Failed to load data:', error);
                setState(prev => ({ ...prev, loading: false }));
            }
        };

        loadData();
    }, []);

    // Auto-generate slug preview from title (not sent to backend)
    const [slugPreview, setSlugPreview] = useState('');
    useEffect(() => {
        if (state.blog.title) {
            const slug = blogService.generateSlug(state.blog.title);
            setSlugPreview(slug);
        }
    }, [state.blog.title]);

    // Auto-save functionality (simplified, no create on auto-save for new)
    useEffect(() => {
        const autoSaveTimer = setTimeout(() => {
            if (state.blog.title && state.blog.content && !state.saving && !state.publishing) {
                // For new page, auto-save could be local storage, but skip for now
            }
        }, 30000);
        return () => clearTimeout(autoSaveTimer);
    }, [state.blog.title, state.blog.content]);

    // Store Quill instance when it's available
    useEffect(() => {
        const checkQuillInstance = () => {
            // Try multiple methods to find Quill instance
            let editor = null;

            // Method 1: Try ref
            if (quillRef.current) {
                editor = quillRef.current.getEditor?.();
            }

            // Method 2: Try DOM search
            if (!editor) {
                const quillElement = document.querySelector('.ql-editor');
                if (quillElement) {
                    editor = (quillElement as any).__quill;
                }
            }

            // Method 3: Try container
            if (!editor) {
                const containerElement = document.querySelector('.ql-container');
                if (containerElement) {
                    editor = (containerElement as any).__quill;
                }
            }

            // Method 4: Search all elements
            if (!editor) {
                const allElements = document.querySelectorAll('*');
                for (let i = 0; i < allElements.length; i++) {
                    const element = allElements[i] as any;
                    if (element.__quill && typeof element.__quill.getSelection === 'function') {
                        editor = element.__quill;
                        break;
                    }
                }
            }

            if (editor && typeof editor.getSelection === 'function') {
                setQuillInstance(editor);
                setIsQuillReady(true);
                console.log('Quill instance stored and ready:', editor);
                return true;
            }
            return false;
        };

        // Check immediately
        if (checkQuillInstance()) {
            return;
        }

        // Check periodically until found
        const interval = setInterval(() => {
            if (!quillInstance) {
                if (checkQuillInstance()) {
                    clearInterval(interval);
                }
            } else {
                clearInterval(interval);
            }
        }, 100);

        // Cleanup after 10 seconds
        const timeout = setTimeout(() => {
            clearInterval(interval);
        }, 10000);

        return () => {
            clearInterval(interval);
            clearTimeout(timeout);
        };
    }, [quillInstance]);

    // Update blog field
    const updateBlog = (field: keyof CreateBlogRequest, value: any) => {
        setState(prev => ({
            ...prev,
            blog: {
                ...prev.blog,
                [field]: value,
            },
            errors: {
                ...prev.errors,
                [field]: '',
            }
        }));
    };

    // Validation
    const validateBlog = (): boolean => {
        const errors: Record<string, string> = {};

        if (!state.blog.title?.trim()) {
            errors.title = 'Tiêu đề là bắt buộc';
        }

        if (!state.blog.content?.trim()) {
            errors.content = 'Nội dung là bắt buộc';
        }

        if (!state.blog.categoryId) {
            errors.categoryId = 'Danh mục là bắt buộc';
        }

        setState(prev => ({ ...prev, errors }));
        return Object.keys(errors).length === 0;
    };

    // Save as draft
    const saveDraft = async () => {
        if (!validateBlog()) return;

        try {
            setState(prev => ({ ...prev, saving: true }));

            const blogData: CreateBlogRequest = {
                ...state.blog as CreateBlogRequest,
                isPublished: false
            };

            const savedBlog = await blogService.createBlog(blogData);
            router.push(`/admin/blog/posts/${savedBlog.id}`);
        } catch (error) {
            console.error('Failed to save draft:', error);
        } finally {
            setState(prev => ({ ...prev, saving: false }));
        }
    };

    // Publish blog
    const publishBlog = async () => {
        if (!validateBlog()) return;

        try {
            setState(prev => ({ ...prev, publishing: true }));

            const blogData: CreateBlogRequest = {
                ...state.blog as CreateBlogRequest,
                isPublished: true
            };

            const publishedBlog = await blogService.createBlog(blogData);
            router.push(`/admin/blog/posts/${publishedBlog.id}`);
        } catch (error) {
            console.error('Failed to publish blog:', error);
        } finally {
            setState(prev => ({ ...prev, publishing: false }));
        }
    };

    // Calculate reading time
    const readingTime = useMemo(() => {
        if (!state.blog.content) return 0;
        return blogService.calculateReadingTime(state.blog.content);
    }, [state.blog.content]);

    // Handle image insertion from Unsplash
    const handleImageInsert = (imageUrl: string, altText: string, attribution: string) => {
        const insertImage = () => {
            // Try multiple methods to find Quill instance
            let quill = null;

            // Method 1: Use stored Quill instance
            if (quillInstance) {
                quill = quillInstance;
                console.log('Using stored Quill instance for image');
            }

            // Method 2: Try to get from ref
            if (!quill && quillRef.current) {
                quill = quillRef.current.getEditor?.();
                if (quill) {
                    console.log('Using Quill instance from ref for image');
                }
            }

            // Method 3: Try DOM methods
            if (!quill) {
                const quillElement = document.querySelector('.ql-editor');
                if (quillElement) {
                    quill = (quillElement as any).__quill;
                    if (quill) {
                        console.log('Using Quill instance from .ql-editor for image');
                    }
                }
            }

            // Method 4: Try container
            if (!quill) {
                const containerElement = document.querySelector('.ql-container');
                if (containerElement) {
                    quill = (containerElement as any).__quill;
                    if (quill) {
                        console.log('Using Quill instance from .ql-container for image');
                    }
                }
            }

            // Method 5: Try to find any Quill instance in the document
            if (!quill) {
                const allElements = document.querySelectorAll('*');
                for (let i = 0; i < allElements.length; i++) {
                    const element = allElements[i] as any;
                    if (element.__quill && typeof element.__quill.getSelection === 'function') {
                        quill = element.__quill;
                        console.log('Using Quill instance from DOM search for image');
                        break;
                    }
                }
            }

            // Method 6: Try to get from window
            if (!quill && (window as any).quillInstances) {
                const instances = (window as any).quillInstances;
                if (instances && instances.length > 0) {
                    quill = instances[0];
                    console.log('Using Quill instance from window for image');
                }
            }

            if (quill && typeof quill.getSelection === 'function') {
                console.log('Quill instance found for image:', quill);
                const range = quill.getSelection();
                const index = range ? range.index : quill.getLength();

                // Insert image with proper attributes
                quill.insertEmbed(index, 'image', imageUrl, 'user');

                // Add line breaks and attribution
                quill.insertText(index + 1, '\n\n', 'user');
                quill.insertText(index + 3, `*${attribution}*`, 'user');
                quill.insertText(index + 3 + attribution.length + 2, '\n\n', 'user');

                // Set cursor after the image and attribution
                quill.setSelection(index + 3 + attribution.length + 4);

                // Force update the content state
                const newContent = quill.root.innerHTML;
                updateBlog('content', newContent);
                return true;
            } else {
                console.error('Quill instance not found in image insert');
                console.log('Stored QuillInstance:', quillInstance);
                console.log('QuillRef:', quillRef.current);
                console.log('Available elements:', document.querySelectorAll('.ql-editor, .ql-container'));
                console.log('All elements with __quill:', Array.from(document.querySelectorAll('*')).filter(el => (el as any).__quill));
                return false;
            }
        };

        // Try immediately first
        if (!insertImage()) {
            // If failed, try again after a short delay
            setTimeout(() => {
                if (!insertImage()) {
                    console.error('Failed to insert image after retry');
                }
            }, 500);
        }
    };

    // Handle product link insertion
    const handleProductLinkInsert = (productId: number, productName: string, productPrice: number, productImage?: string) => {
        const insertProductLink = () => {
            // Try multiple methods to find Quill instance
            let quill = null;

            // Method 1: Use stored Quill instance
            if (quillInstance) {
                quill = quillInstance;
                console.log('Using stored Quill instance');
            }

            // Method 2: Try to get from ref
            if (!quill && quillRef.current) {
                quill = quillRef.current.getEditor?.();
                if (quill) {
                    console.log('Using Quill instance from ref');
                }
            }

            // Method 3: Try DOM methods
            if (!quill) {
                const quillElement = document.querySelector('.ql-editor');
                if (quillElement) {
                    quill = (quillElement as any).__quill;
                    if (quill) {
                        console.log('Using Quill instance from .ql-editor');
                    }
                }
            }

            // Method 4: Try container
            if (!quill) {
                const containerElement = document.querySelector('.ql-container');
                if (containerElement) {
                    quill = (containerElement as any).__quill;
                    if (quill) {
                        console.log('Using Quill instance from .ql-container');
                    }
                }
            }

            // Method 5: Try to find any Quill instance in the document
            if (!quill) {
                const allElements = document.querySelectorAll('*');
                for (let i = 0; i < allElements.length; i++) {
                    const element = allElements[i] as any;
                    if (element.__quill && typeof element.__quill.getSelection === 'function') {
                        quill = element.__quill;
                        console.log('Using Quill instance from DOM search');
                        break;
                    }
                }
            }

            // Method 6: Try to get from window
            if (!quill && (window as any).quillInstances) {
                const instances = (window as any).quillInstances;
                if (instances && instances.length > 0) {
                    quill = instances[0];
                    console.log('Using Quill instance from window');
                }
            }

            if (quill && typeof quill.getSelection === 'function') {
                console.log('Quill instance found for product link:', quill);
                const range = quill.getSelection();
                const index = range ? range.index : quill.getLength();

                // Create product link in the format [product:ID:Name:Price:Image]
                const productLink = `[product:${productId}:${productName}:${productPrice}:${productImage || ''}]`;

                // Insert with line breaks for better formatting
                quill.insertText(index, '\n\n', 'user');
                quill.insertText(index + 2, productLink, 'user');
                quill.insertText(index + 2 + productLink.length, '\n\n', 'user');

                // Set cursor after the product link
                quill.setSelection(index + 2 + productLink.length + 2);

                // Force update the content state
                const newContent = quill.root.innerHTML;
                updateBlog('content', newContent);
                return true;
            } else {
                console.error('Quill instance not found in product link insert');
                console.log('Stored QuillInstance:', quillInstance);
                console.log('QuillRef:', quillRef.current);
                console.log('Available elements:', document.querySelectorAll('.ql-editor, .ql-container'));
                console.log('All elements with __quill:', Array.from(document.querySelectorAll('*')).filter(el => (el as any).__quill));
                return false;
            }
        };

        // Try immediately first
        if (!insertProductLink()) {
            // If failed, try again after a short delay
            setTimeout(() => {
                if (!insertProductLink()) {
                    console.error('Failed to insert product link after retry');
                }
            }, 500);
        }
    };

    if (state.loading) {
        return (
            <div className="p-6">
                <div className="flex items-center justify-center h-64">
                    <Loader2 className="w-8 h-8 animate-spin" />
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
                        <h1 className="text-2xl font-bold text-gray-900">Tạo bài viết mới</h1>
                        <div className="flex items-center gap-2">
                            {state.autoSaving && (
                                <span className="text-sm text-gray-500 flex items-center gap-1">
                                    <Loader2 className="w-4 h-4 animate-spin" />
                                    Đang lưu...
                                </span>
                            )}
                            <Button
                                variant="outline"
                                onClick={saveDraft}
                                disabled={state.saving || state.publishing}
                            >
                                {state.saving && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                                <Save className="w-4 h-4 mr-2" />
                                Lưu nháp
                            </Button>
                            <Button
                                onClick={publishBlog}
                                disabled={state.saving || state.publishing}
                            >
                                {state.publishing && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                                <Send className="w-4 h-4 mr-2" />
                                Xuất bản
                            </Button>
                        </div>
                    </div>

                    {/* Title */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Tiêu đề</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div>
                                <Label htmlFor="title">Tiêu đề bài viết *</Label>
                                <Input
                                    id="title"
                                    value={state.blog.title || ''}
                                    onChange={(e) => updateBlog('title', e.target.value)}
                                    placeholder="Nhập tiêu đề bài viết..."
                                    className={cn(state.errors.title && "border-red-500")}
                                />
                                {state.errors.title && (
                                    <p className="text-sm text-red-600 mt-1">{state.errors.title}</p>
                                )}
                            </div>

                            <div>
                                <Label>Slug Preview</Label>
                                <Input
                                    value={slugPreview}
                                    placeholder="Slug sẽ được tạo tự động"
                                    disabled
                                />
                                <p className="text-xs text-gray-500 mt-1">
                                    URL: /blog/{slugPreview || 'slug-url'}
                                </p>
                            </div>

                            <div>
                                <Label htmlFor="excerpt">Tóm tắt</Label>
                                <Textarea
                                    id="excerpt"
                                    value={state.blog.excerpt || ''}
                                    onChange={(e) => updateBlog('excerpt', e.target.value)}
                                    placeholder="Tóm tắt ngắn về bài viết..."
                                    rows={3}
                                />
                            </div>
                        </CardContent>
                    </Card>

                    {/* Content Editor */}
                    <Card>
                        <CardHeader>
                            <div className="flex items-center justify-between">
                                <CardTitle>Nội dung bài viết *</CardTitle>
                                <div className="flex items-center gap-2">
                                    {readingTime > 0 && (
                                        <Badge variant="outline" className="flex items-center gap-1">
                                            <Clock className="w-3 h-3" />
                                            {readingTime} phút đọc
                                        </Badge>
                                    )}
                                </div>
                            </div>
                        </CardHeader>
                        <CardContent>
                            {/* Custom Toolbar */}
                            <div className="flex items-center gap-2 mb-4 p-3 bg-gray-50 rounded-lg border">
                                <span className="text-sm font-medium text-gray-700">Chèn nội dung:</span>
                                <UnsplashImagePicker
                                    onImageSelect={handleImageInsert}
                                    trigger={
                                        <Button variant="outline" size="sm" disabled={!isQuillReady}>
                                            <ImageIcon className="w-4 h-4 mr-2" />
                                            Ảnh từ Unsplash
                                        </Button>
                                    }
                                />
                                <ProductLinkPicker
                                    onProductSelect={handleProductLinkInsert}
                                    trigger={
                                        <Button variant="outline" size="sm" disabled={!isQuillReady}>
                                            <Package className="w-4 h-4 mr-2" />
                                            Link sản phẩm
                                        </Button>
                                    }
                                />
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => {
                                        const htmlContent = prompt('Dn HTML content vo y:');
                                        if (htmlContent) {
                                            // Method 1: Try to find Quill instance
                                            let quill = null;

                                            // Look for .ql-editor
                                            const quillElement = document.querySelector('.ql-editor');
                                            if (quillElement) {
                                                quill = (quillElement as any).__quill;
                                            }

                                            if (quill) {
                                                try {
                                                    quill.clipboard.dangerouslyPasteHTML(htmlContent);
                                                    console.log('HTML pasted successfully');
                                                } catch (error) {
                                                    console.error('Error pasting HTML:', error);
                                                    // Fallback: Update content directly
                                                    updateBlog('content', htmlContent);
                                                }
                                            } else {
                                                console.log('Quill instance not found, using fallback method');
                                                // Fallback: Update content directly
                                                updateBlog('content', htmlContent);
                                            }
                                        }
                                    }}
                                >
                                    <Settings className="w-4 h-4 mr-2" />
                                    Paste HTML
                                </Button>
                            </div>

                            <div className={cn("border rounded-lg", state.errors.content && "border-red-500")}>
                                <ReactQuill
                                    value={state.blog.content || ''}
                                    onChange={(value: string) => updateBlog('content', value)}
                                    modules={quillModules}
                                    formats={quillFormats}
                                    placeholder="Viết nội dung bài viết của bạn ở đây..."
                                    className="min-h-[400px]"
                                    ref={quillRef}
                                />
                            </div>
                            {state.errors.content && (
                                <p className="text-sm text-red-600 mt-1">{state.errors.content}</p>
                            )}

                            {/* Help Text */}
                            <div className="mt-3 p-3 bg-blue-50 rounded-lg">
                                <h4 className="text-sm font-medium text-blue-900 mb-2">Hướng dẫn sử dụng:</h4>
                                <ul className="text-xs text-blue-800 space-y-1">
                                    <li> Sử dụng nút "Link sản phẩm" để chèn link đến sản phẩm</li>
                                    <li> Format link sản phẩm: [product:ID:Tên:Giá:Ảnh]</li>
                                </ul>
                            </div>
                        </CardContent>
                    </Card>
                </div>

                {/* Sidebar */}
                <div className="space-y-6">
                    {/* Featured Image */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <ImageIcon className="w-5 h-5" />
                                Ảnh đại diện
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div>
                                <Label htmlFor="featuredImage">URL ảnh</Label>
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
                                    <SelectValue placeholder="Chọn danh mục" />
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
                            {state.errors.categoryId && (
                                <p className="text-sm text-red-600 mt-1">{state.errors.categoryId}</p>
                            )}
                        </CardContent>
                    </Card>

                    {/* Settings */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Settings className="w-5 h-5" />
                                Cài đặt
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center justify-between">
                                <Label htmlFor="isPublished">Xuất bản ngay</Label>
                                <Switch
                                    id="isPublished"
                                    checked={state.blog.isPublished || false}
                                    onCheckedChange={(checked) => updateBlog('isPublished', checked)}
                                />
                            </div>

                            {/* Temporarily hide featured posts until homepage section is implemented */}
                            {/* <div className="flex items-center justify-between">
                                <Label htmlFor="isFeatured">Bài viết nổi bật</Label>
                                <Switch
                                    id="isFeatured"
                                    checked={state.blog.isFeatured || false}
                                    onCheckedChange={(checked) => updateBlog('isFeatured', checked)}
                                />
                            </div> */}
                        </CardContent>
                    </Card>

                    {/* SEO - Temporarily hidden until meta tags are implemented in HTML head */}
                            {/* <Card>
                        <CardHeader>
                            <CardTitle>SEO</CardTitle>
                            <CardDescription>
                                Tối ưu hóa cho công cụ tìm kiếm
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div>
                                <Label htmlFor="metaTitle">Meta Title</Label>
                                <Input
                                    id="metaTitle"
                                    value={state.blog.metaTitle || ''}
                                    onChange={(e) => updateBlog('metaTitle', e.target.value)}
                                    placeholder="Tiêu đề meta..."
                                />
                            </div>

                            <div>
                                <Label htmlFor="metaDescription">Meta Description</Label>
                                <Textarea
                                    id="metaDescription"
                                    value={state.blog.metaDescription || ''}
                                    onChange={(e) => updateBlog('metaDescription', e.target.value)}
                                    placeholder="Mô tả meta..."
                                    rows={3}
                                />
                            </div>
                        </CardContent>
                    </Card> */}
                </div>
            </div>
        </div>
    );
}
