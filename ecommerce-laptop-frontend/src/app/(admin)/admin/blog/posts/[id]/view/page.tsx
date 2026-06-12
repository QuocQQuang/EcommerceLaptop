'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Textarea } from '@/components/ui/textarea';
import { blogService } from '@/services/blogService';
import { Blog, BlogComment, CommentStatus, CreateCommentRequest } from '@/types/api';
import {
    ArrowLeft,
    Calendar,
    Clock,
    Edit,
    Eye,
    Heart,
    MessageCircle,
    Share2,
    Tag,
    ThumbsUp,
    User
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';

interface BlogViewState {
    blog: Blog | null;
    comments: BlogComment[];
    loading: boolean;
    commentsLoading: boolean;
    submittingComment: boolean;
    newComment: {
        authorName: string;
        authorEmail: string;
        content: string;
    };
    showCommentForm: boolean;
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

export default function BlogViewPage({ params }: { params: { id: string } }) {
    const router = useRouter();
    const [state, setState] = useState<BlogViewState>({
        blog: null,
        comments: [],
        loading: true,
        commentsLoading: true,
        submittingComment: false,
        newComment: {
            authorName: '',
            authorEmail: '',
            content: ''
        },
        showCommentForm: false,
    });

    // Load blog details
    const loadBlog = useCallback(async () => {
        try {
            setState(prev => ({ ...prev, loading: true }));
            const blog = await blogService.getBlogById(params.id);
            setState(prev => ({ ...prev, blog, loading: false }));
        } catch (error) {
            console.error('Failed to load blog:', error);
            setState(prev => ({ ...prev, loading: false }));
            toast.error('Không thể tải bài viết');
        }
    }, [params.id]);

    // Load comments
    const loadComments = useCallback(async () => {
        try {
            setState(prev => ({ ...prev, commentsLoading: true }));
            const commentsResponse = await blogService.getCommentsByBlogId(params.id, 1, 50);
            setState(prev => ({
                ...prev,
                comments: commentsResponse.items,
                commentsLoading: false
            }));
        } catch (error) {
            console.error('Failed to load comments:', error);
            setState(prev => ({ ...prev, commentsLoading: false }));
        }
    }, [params.id]);

    useEffect(() => {
        loadBlog();
        loadComments();
    }, [loadBlog, loadComments]);

    // Submit comment
    const handleSubmitComment = async () => {
        if (!state.newComment.content.trim() || !state.newComment.authorName.trim() || !state.newComment.authorEmail.trim()) {
            toast.error('Vui lòng điền đầy đủ thông tin');
            return;
        }

        try {
            setState(prev => ({ ...prev, submittingComment: true }));

            const commentData: CreateCommentRequest = {
                blogId: params.id,
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

            toast.success('Bình luận đã được gửi và đang chờ duyệt');
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

    const handleEditBlog = () => {
        if (!state.blog) {
            return;
        }

        router.push(`/admin/blog/posts/${state.blog.id}/edit`);
    };

    if (state.loading) {
        return (
            <div className="p-6">
                <div className="flex items-center justify-center h-64">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900" />
                </div>
            </div>
        );
    }

    if (!state.blog) {
        return (
            <div className="p-6">
                <div className="text-center">
                    <h2 className="text-xl font-semibold text-gray-900 mb-2">Không tìm thấy bài viết</h2>
                    <p className="text-gray-600 mb-4">Bài viết bạn đang tìm kiếm không tồn tại hoặc đã bị xóa.</p>
                    <Button onClick={() => router.push('/admin/blog/posts')}>
                        <ArrowLeft className="w-4 h-4 mr-2" />
                        Quay lại danh sách
                    </Button>
                </div>
            </div>
        );
    }

    const approvedComments = state.comments.filter(c => c.status === CommentStatus.Approved);

    return (
        <div className="p-6 max-w-4xl mx-auto">
            {/* Header */}
            <div className="flex items-center justify-between mb-6">
                <Button variant="outline" onClick={() => router.push('/admin/blog/posts')}>
                    <ArrowLeft className="w-4 h-4 mr-2" />
                    Quay lại
                </Button>
                <Button onClick={handleEditBlog}>
                    <Edit className="w-4 h-4 mr-2" />
                    Chỉnh sửa
                </Button>
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
                            {state.blog.status === 'published' ? 'Đã xuất bản' :
                                state.blog.status === 'draft' ? 'Bản nháp' :
                                    state.blog.status === 'scheduled' ? 'Đã lên lịch' : 'Lưu trữ'}
                        </Badge>
                    </div>

                    {/* Title */}
                    <h1 className="text-3xl font-bold text-gray-900 mb-4">{state.blog.title}</h1>

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
                            {formatReadingTime(state.blog.readingTime || 0)}
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
                        <div className="mb-6">
                            <img
                                src={state.blog.featuredImageUrl}
                                alt={state.blog.featuredImageAlt || state.blog.title}
                                className="w-full h-64 object-cover rounded-lg"
                            />
                        </div>
                    )}

                    {/* Excerpt */}
                    {state.blog.excerpt && (
                        <div className="mb-6 p-4 bg-gray-50 border-l-4 border-blue-500 rounded-r-lg">
                            <p className="text-gray-700 font-medium italic">{state.blog.excerpt}</p>
                        </div>
                    )}

                    {/* Content */}
                    <div
                        className="prose prose-lg max-w-none"
                        dangerouslySetInnerHTML={{ __html: state.blog.content }}
                    />

                    {/* Engagement Actions */}
                    <div className="flex items-center justify-between mt-8 pt-6 border-t border-gray-200">
                        <div className="flex items-center gap-4">
                            <Button variant="outline" size="sm">
                                <Heart className="w-4 h-4 mr-2" />
                                Thích ({state.blog.likeCount})
                            </Button>
                            <Button variant="outline" size="sm">
                                <Share2 className="w-4 h-4 mr-2" />
                                Chia sẻ ({state.blog.shareCount})
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
                    {state.blog.allowComments && (
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
                                        <Textarea
                                            value={state.newComment.content}
                                            onChange={(e) => setState(prev => ({
                                                ...prev,
                                                newComment: { ...prev.newComment, content: e.target.value }
                                            }))}
                                            placeholder="Chia sẻ suy nghĩ của bạn về bài viết..."
                                            className="min-h-20"
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
                                                    đang gửi...
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
                                {state.blog.allowComments
                                    ? 'Hãy là người đầu tiên bình luận về bài viết này!'
                                    : 'Bình luận đã được tắt cho bài viết này.'
                                }
                            </p>
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}
