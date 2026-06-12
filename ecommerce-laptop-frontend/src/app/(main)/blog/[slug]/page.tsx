'use client';

import BlogContentPreview from '@/components/blog/BlogContentPreview';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { blogService } from '@/services/blogService';
import { Blog, BlogComment, CommentStatus, CreateCommentRequest } from '@/types/api';
import {
    ArrowLeft,
    Calendar,
    Clock,
    ExternalLink,
    Eye,
    Heart,
    MessageCircle,
    Share2,
    ShoppingCart,
    Tag,
    ThumbsUp,
    User
} from 'lucide-react';
import dynamic from 'next/dynamic';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { use, useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';

// Dynamic import for ReactQuill to avoid SSR issues
const ReactQuill = dynamic(
    () => import('react-quill-new').then(mod => mod.default),
    { ssr: false }
);

interface BlogViewState {
    blog: Blog | null;
    comments: BlogComment[];
    loading: boolean;
    commentsLoading: boolean;
    submittingComment: boolean;
    liking?: boolean;
    newComment: {
        authorName: string;
        authorEmail: string;
        content: string;
    };
    showCommentForm: boolean;
    relatedProducts: any[];
    hasLiked?: boolean;
}

interface CommentItemProps {
    comment: BlogComment;
    onReply?: (parentId: string) => void;
}

function CommentItem({ comment, onReply }: CommentItemProps) {
    const getStatusBadgeVariant = (status: CommentStatus) => {
        switch (status) {
            case CommentStatus.Approved:
                return 'default';
            case CommentStatus.Pending:
                return 'secondary';
            case CommentStatus.Rejected:
                return 'destructive';
            case CommentStatus.Spam:
                return 'outline';
            default:
                return 'secondary';
        }
    };

    const getStatusText = (status: CommentStatus) => {
        switch (status) {
            case CommentStatus.Approved:
                return 'Đã duyệt';
            case CommentStatus.Pending:
                return 'Chờ duyệt';
            case CommentStatus.Rejected:
                return 'Từ chối';
            case CommentStatus.Spam:
                return 'Spam';
            default:
                return 'Không xác định';
        }
    };

    return (
        <div className="border-b border-gray-100 pb-4 last:border-0">
            <div className="flex items-start gap-3">
                <div className="w-10 h-10 bg-gray-200 rounded-full flex items-center justify-center">
                    <User className="w-5 h-5 text-gray-500" />
                </div>
                <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                        <span className="font-semibold text-gray-900">{comment.authorName}</span>
                        <Badge variant={getStatusBadgeVariant(comment.status)} className="text-xs">
                            {getStatusText(comment.status)}
                        </Badge>
                        <span className="text-sm text-gray-500">
                            {new Date(comment.createdAt).toLocaleDateString('vi-VN')}
                        </span>
                    </div>
                    <p className="text-gray-700 leading-relaxed">{comment.content}</p>

                    {comment.status === CommentStatus.Approved && (
                        <div className="flex items-center gap-4 mt-3">
                            <Button
                                variant="ghost"
                                size="sm"
                                onClick={() => onReply?.(comment.id)}
                                className="text-blue-600 hover:text-blue-700 px-0"
                            >
                                <MessageCircle className="w-4 h-4 mr-1" />
                                Phản hồi
                            </Button>
                            <Button variant="ghost" size="sm" className="text-gray-600 hover:text-gray-700 px-0">
                                <ThumbsUp className="w-4 h-4 mr-1" />
                                Thích
                            </Button>
                        </div>
                    )}

                    {/* Replies */}
                    {comment.replies && comment.replies.length > 0 && (
                        <div className="ml-6 mt-4 space-y-4">
                            {comment.replies.map((reply) => (
                                <CommentItem key={reply.id} comment={reply} onReply={onReply} />
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

interface ProductLinkProps {
    productId: number;
    productName: string;
    productPrice?: number;
    productImage?: string;
}

function ProductLink({ productId, productName, productPrice, productImage }: ProductLinkProps) {
    return (
        <Card className="my-4 border-l-4 border-l-blue-500 bg-blue-50">
            <CardContent className="p-4">
                <div className="flex items-center gap-4">
                    {productImage && (
                        <img
                            src={productImage}
                            alt={productName}
                            className="w-16 h-16 object-cover rounded-lg"
                        />
                    )}
                    <div className="flex-1">
                        <h4 className="font-semibold text-gray-900 mb-1">{productName}</h4>
                        {productPrice && (
                            <p className="text-lg font-bold text-blue-600 mb-2">
                                {new Intl.NumberFormat('vi-VN', {
                                    style: 'currency',
                                    currency: 'VND'
                                }).format(productPrice)}
                            </p>
                        )}
                        <div className="flex items-center gap-2">
                            <Button asChild size="sm">
                                <Link href={`/products/${productId}`}>
                                    <ExternalLink className="w-4 h-4 mr-2" />
                                    Xem sản phẩm
                                </Link>
                            </Button>
                            <Button asChild size="sm" variant="outline">
                                <Link href={`/products/${productId}`}>
                                    <ShoppingCart className="w-4 h-4 mr-2" />
                                    Thêm vào giỏ
                                </Link>
                            </Button>
                        </div>
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}

export default function BlogViewPage({ params }: { params: Promise<{ slug: string }> }) {
    const { slug } = use(params);
    const router = useRouter();
    const [state, setState] = useState<BlogViewState>({
        blog: null,
        comments: [],
        loading: true,
        commentsLoading: true,
        submittingComment: false,
        liking: false,
        newComment: {
            authorName: '',
            authorEmail: '',
            content: ''
        },
        showCommentForm: false,
        relatedProducts: [],
        hasLiked: false
    });

    // Load blog details
    const loadBlog = useCallback(async () => {
        try {
            setState(prev => ({ ...prev, loading: true }));
            const blog = await blogService.getBlogBySlug(slug);
            setState(prev => ({ ...prev, blog, loading: false }));
        } catch (error) {
            console.error('Failed to load blog:', error);
            setState(prev => ({ ...prev, loading: false }));
            toast.error('Không thể tải bài viết');
        }
    }, [slug]);

    // Load comments
    const loadComments = useCallback(async () => {
        if (!state.blog) return;

        try {
            setState(prev => ({ ...prev, commentsLoading: true }));
            const commentsResponse = await blogService.getCommentsByBlogId(state.blog.id, 1, 50);
            setState(prev => ({
                ...prev,
                comments: commentsResponse.items,
                commentsLoading: false
            }));
        } catch (error) {
            console.error('Failed to load comments:', error);
            setState(prev => ({ ...prev, commentsLoading: false }));
        }
    }, [state.blog]);

    // Toggle like
    const handleToggleLike = useCallback(async () => {
        if (!state.blog || state.liking) return;

        try {
            setState(prev => ({ ...prev, liking: true }));

            let newCount = state.blog.likeCount;
            if (state.hasLiked) {
                newCount = await blogService.unlikeBlog(state.blog.id);
            } else {
                newCount = await blogService.likeBlog(state.blog.id);
            }

            setState(prev => ({
                ...prev,
                liking: false,
                hasLiked: !prev.hasLiked,
                blog: prev.blog ? { ...prev.blog, likeCount: newCount } as Blog : prev.blog
            }));
        } catch (error) {
            console.error('Failed to toggle like:', error);
            setState(prev => ({ ...prev, liking: false }));
            toast.error('Không thể cập nhật lượt thích');
        }
    }, [state.blog, state.hasLiked, state.liking]);

    useEffect(() => {
        loadBlog();
    }, [loadBlog]);

    useEffect(() => {
        if (state.blog) {
            loadComments();
        }
    }, [state.blog, loadComments]);

    // Submit comment
    const handleSubmitComment = async () => {
        if (!state.blog) return;

        if (!state.newComment.content.trim() || !state.newComment.authorName.trim() || !state.newComment.authorEmail.trim()) {
            toast.error('Vui lòng điền đầy đủ thông tin');
            return;
        }

        try {
            setState(prev => ({ ...prev, submittingComment: true }));

            const commentData: CreateCommentRequest = {
                blogId: state.blog.id.toString(),
                authorName: state.newComment.authorName,
                authorEmail: state.newComment.authorEmail,
                content: state.newComment.content
            };

            await blogService.createComment(commentData);

            setState(prev => ({
                ...prev,
                newComment: { authorName: '', authorEmail: '', content: '' },
                showCommentForm: false,
                submittingComment: false
            }));

            toast.success('Bình luận đã được gửi');
            await loadComments();
        } catch (error) {
            console.error('Failed to submit comment:', error);
            setState(prev => ({ ...prev, submittingComment: false }));
            toast.error('Không thể gửi bình luận');
        }
    };

    // Format reading time
    const formatReadingTime = (minutes: number) => {
        return minutes < 1 ? 'Dưới 1 phút đọc' : `${minutes} phút đọc`;
    };

    // Parse content for product links
    const parseContentForProductLinks = (content: string) => {
        // This is a simple regex to find product links in the format [product:ID:Name:Price:Image]
        const productLinkRegex = /\[product:(\d+):([^:]+):([^:]*):([^\]]*)\]/g;
        const matches = [];
        let match;

        while ((match = productLinkRegex.exec(content)) !== null) {
            matches.push({
                productId: parseInt(match[1]),
                productName: match[2],
                productPrice: match[3] ? parseFloat(match[3]) : undefined,
                productImage: match[4] || undefined,
                fullMatch: match[0]
            });
        }

        return matches;
    };

    // Render content with product links
    const renderContentWithProductLinks = (content: string) => {
        const productLinks = parseContentForProductLinks(content);
        let processedContent = content;

        // Replace product links with placeholders
        productLinks.forEach((link, index) => {
            processedContent = processedContent.replace(link.fullMatch, `__PRODUCT_LINK_${index}__`);
        });

        return { processedContent, productLinks };
    };

    if (state.loading) {
        return (
            <div className="min-h-screen bg-gray-50">
                <div className="container mx-auto px-4 py-8">
                    <div className="flex items-center justify-center h-64">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900" />
                    </div>
                </div>
            </div>
        );
    }

    if (!state.blog) {
        return (
            <div className="min-h-screen bg-gray-50">
                <div className="container mx-auto px-4 py-8">
                    <div className="text-center">
                        <h2 className="text-xl font-semibold text-gray-900 mb-2">Không tìm thấy bài viết</h2>
                        <p className="text-gray-600 mb-4">Bài viết bạn đang tìm kiếm không tồn tại hoặc đã bị xóa.</p>
                        <Button onClick={() => router.push('/blog')}>
                            <ArrowLeft className="w-4 h-4 mr-2" />
                            Quay lại danh sách
                        </Button>
                    </div>
                </div>
            </div>
        );
    }

    const approvedComments = state.comments.filter(c => c.status === CommentStatus.Approved);
    const { processedContent, productLinks } = renderContentWithProductLinks(state.blog.content);

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="container mx-auto px-4 py-8">
                {/* Header */}
                <div className="flex items-center justify-between mb-6">
                    <Button variant="outline" onClick={() => router.push('/blog')}>
                        <ArrowLeft className="w-4 h-4 mr-2" />
                        Quay lại
                    </Button>
                    <div className="flex items-center gap-2">
                        <Button variant="outline" size="sm">
                            <Share2 className="w-4 h-4 mr-2" />
                            Chia sẻ
                        </Button>
                    </div>
                </div>

                {/* Blog Content */}
                <Card className="mb-8">
                    <CardContent className="pt-6">
                        {/* Status Badge */}
                        <div className="mb-4">
                            <Badge
                                variant={state.blog.status === 'published' ? 'default' : 'secondary'}
                                className="text-sm"
                            >
                                {state.blog.status === 'published' ? 'đã xuất bản' :
                                    state.blog.status === 'draft' ? 'Bản nháp' :
                                        state.blog.status === 'scheduled' ? 'đã lên lịch' : 'Lưu trữ'}
                            </Badge>
                        </div>

                        {/* Title */}
                        <h1 className="text-4xl font-bold text-gray-900 mb-4 leading-tight">{state.blog.title}</h1>

                        {/* Meta Information */}
                        <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600 mb-6">
                            <div className="flex items-center gap-1">
                                <User className="w-4 h-4" />
                                {state.blog.author
                                    ? state.blog.author.displayName ||
                                    [state.blog.author.firstName, state.blog.author.lastName]
                                        .filter(Boolean)
                                        .join(' ') ||
                                    'Không rõ tác giả'
                                    : 'Không rõ tác giả'}
                            </div>
                            <div className="flex items-center gap-1">
                                <Calendar className="w-4 h-4" />
                                {new Date(state.blog.createdAt).toLocaleDateString('vi-VN')}
                            </div>
                            <div className="flex items-center gap-1">
                                <Clock className="w-4 h-4" />
                                {formatReadingTime((state.blog as any).readingTime || 0)}
                            </div>
                            <div className="flex items-center gap-1">
                                <Eye className="w-4 h-4" />
                                {state.blog.viewCount} lượt xem
                            </div>
                            <div className="flex items-center gap-1">
                                <MessageCircle className="w-4 h-4" />
                                {state.blog.commentCount} bình luận
                            </div>
                        </div>

                        {/* Category and Tags */}
                        <div className="flex flex-wrap items-center gap-2 mb-6">
                            {state.blog.category && (
                                <Badge variant="outline">
                                    {state.blog.category.name}
                                </Badge>
                            )}
                            {state.blog.tags && state.blog.tags.map(tag => (
                                <Badge key={tag.id} variant="secondary" className="text-xs">
                                    <Tag className="w-3 h-3 mr-1" />
                                    {tag.name}
                                </Badge>
                            ))}
                        </div>

                        {/* Featured Image */}
                        {state.blog.featuredImageUrl && (
                            <div className="mb-8">
                                <img
                                    src={state.blog.featuredImageUrl}
                                    alt={(state.blog as any).featuredImageAlt || state.blog.title}
                                    className="w-full h-96 object-cover rounded-lg shadow-lg"
                                />
                            </div>
                        )}

                        {/* Excerpt */}
                        {state.blog.excerpt && (
                            <div className="mb-8 p-6 bg-blue-50 border-l-4 border-blue-500 rounded-r-lg">
                                <p className="text-gray-700 font-medium text-lg italic">{state.blog.excerpt}</p>
                            </div>
                        )}

                        {/* Content with Product Links */}
                        <BlogContentPreview content={state.blog.content} />

                        {/* Engagement Actions */}
                        <div className="flex items-center justify-between mt-8 pt-6 border-t border-gray-200">
                            <div className="flex items-center gap-4">
                                <Button variant={state.hasLiked ? 'default' : 'outline'} size="sm" onClick={handleToggleLike} disabled={state.liking}>
                                    <Heart className="w-4 h-4 mr-2" />
                                    {state.hasLiked ? 'đã thích' : 'Thích'} ({state.blog.likeCount})
                                </Button>
                                <Button variant="outline" size="sm">
                                    <Share2 className="w-4 h-4 mr-2" />
                                    Chia sẻ ({(state.blog as any).shareCount || 0})
                                </Button>
                            </div>
                            <div className="text-sm text-gray-500">
                                Cập nhật lần cuối: {new Date(state.blog.updatedAt).toLocaleDateString('vi-VN')}
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/* Comments Section */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <MessageCircle className="w-5 h-5" />
                            Bình luận ({approvedComments.length})
                        </CardTitle>
                        <CardDescription>
                            Tương tác và thảo luận về bài viết
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        {/* Add Comment Button */}
                        {(state.blog as any).allowComments !== false && (
                            <div className="mb-6">
                                {!state.showCommentForm ? (
                                    <Button
                                        onClick={() => setState(prev => ({ ...prev, showCommentForm: true }))}
                                        className="w-full"
                                        variant="outline"
                                    >
                                        <MessageCircle className="w-4 h-4 mr-2" />
                                        Thêm bình luận
                                    </Button>
                                ) : (
                                    <div className="space-y-4 p-4 border border-gray-200 rounded-lg">
                                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                            <div>
                                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                                    Tên của bạn *
                                                </label>
                                                <input
                                                    type="text"
                                                    value={state.newComment.authorName}
                                                    onChange={(e) => setState(prev => ({
                                                        ...prev,
                                                        newComment: { ...prev.newComment, authorName: e.target.value }
                                                    }))}
                                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                                                    placeholder="Nhập tên của bạn"
                                                />
                                            </div>
                                            <div>
                                                <label className="block text-sm font-medium text-gray-700 mb-1">
                                                    Email *
                                                </label>
                                                <input
                                                    type="email"
                                                    value={state.newComment.authorEmail}
                                                    onChange={(e) => setState(prev => ({
                                                        ...prev,
                                                        newComment: { ...prev.newComment, authorEmail: e.target.value }
                                                    }))}
                                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                                                    placeholder="Nhập email của bạn"
                                                />
                                            </div>
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-gray-700 mb-1">
                                                Nội dung bình luận *
                                            </label>
                                            <textarea
                                                value={state.newComment.content}
                                                onChange={(e) => setState(prev => ({
                                                    ...prev,
                                                    newComment: { ...prev.newComment, content: e.target.value }
                                                }))}
                                                placeholder="Chia sẻ suy nghĩ của bạn về bài viết..."
                                                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 min-h-20"
                                            />
                                        </div>
                                        <div className="flex items-center gap-2">
                                            <Button
                                                onClick={handleSubmitComment}
                                                disabled={state.submittingComment}
                                                size="sm"
                                            >
                                                {state.submittingComment ? (
                                                    <>
                                                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                                                        Đang gửi...
                                                    </>
                                                ) : (
                                                    'Gửi bình luận'
                                                )}
                                            </Button>
                                            <Button
                                                variant="outline"
                                                onClick={() => setState(prev => ({
                                                    ...prev,
                                                    showCommentForm: false,
                                                    newComment: { authorName: '', authorEmail: '', content: '' }
                                                }))}
                                                size="sm"
                                            >
                                                Hủy
                                            </Button>
                                        </div>
                                        <p className="text-xs text-gray-500">
                                            Bình luận của bạn sẽ được xem xét trước khi hiển thị.
                                        </p>
                                    </div>
                                )}
                            </div>
                        )}

                        <Separator className="my-6" />

                        {/* Comments List */}
                        {state.commentsLoading ? (
                            <div className="flex items-center justify-center py-8">
                                <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-gray-900" />
                            </div>
                        ) : approvedComments.length > 0 ? (
                            <div className="space-y-6">
                                {approvedComments.map((comment) => (
                                    <CommentItem
                                        key={comment.id}
                                        comment={comment}
                                        onReply={(parentId) => {
                                            // Handle reply functionality
                                            console.log('Reply to comment:', parentId);
                                        }}
                                    />
                                ))}
                            </div>
                        ) : (
                            <div className="text-center py-8">
                                <MessageCircle className="w-12 h-12 text-gray-300 mx-auto mb-4" />
                                <p className="text-gray-500 mb-2">Chưa có bình luận nào</p>
                                <p className="text-sm text-gray-400">
                                    {(state.blog as any).allowComments !== false
                                        ? 'Hãy là người đầu tiên bình luận về bài viết này!'
                                        : 'Bình luận đã được tắt cho bài viết này.'
                                    }
                                </p>
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}
