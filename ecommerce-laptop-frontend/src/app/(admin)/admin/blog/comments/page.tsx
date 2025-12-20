'use client';

import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuSeparator, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
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
import { BlogComment, CommentStatus, UpdateCommentStatusRequest } from '@/types/api';
import {
    Check,
    MessageCircle,
    MoreHorizontal,
    Reply,
    Search,
    Trash2,
    User,
    X
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

interface CommentsPageState {
    comments: BlogComment[];
    loading: boolean;
    searchTerm: string;
    statusFilter: CommentStatus | 'all';
    selectedComment: BlogComment | null;
    replyModalOpen: boolean;
    replyContent: string;
    deletingComment: BlogComment | null;
    approvingComment: BlogComment | null;
    rejectingComment: BlogComment | null;
    replying: boolean;
    bulkSelectMode: boolean;
    selectedComments: Set<string>;
}

export default function CommentsPage() {
    const [state, setState] = useState<CommentsPageState>({
        comments: [],
        loading: true,
        searchTerm: '',
        statusFilter: 'all',
        selectedComment: null,
        replyModalOpen: false,
        replyContent: '',
        deletingComment: null,
        approvingComment: null,
        rejectingComment: null,
        replying: false,
        bulkSelectMode: false,
        selectedComments: new Set(),
    });

    // Load comments
    const loadComments = useCallback(async () => {
        try {
            setState(prev => ({ ...prev, loading: true }));

            const searchParams: any = {};
            if (state.statusFilter !== 'all') {
                searchParams.status = state.statusFilter;
            }
            if (state.searchTerm) {
                searchParams.search = state.searchTerm;
            }

            const commentsResponse = await blogService.searchComments(searchParams);
            setState(prev => ({ ...prev, comments: commentsResponse.items, loading: false }));
        } catch (error) {
            console.error('Failed to load comments:', error);
            setState(prev => ({ ...prev, loading: false }));
        }
    }, [state.statusFilter, state.searchTerm]);

    useEffect(() => {
        const debounceTimer = setTimeout(() => {
            loadComments();
        }, 300);

        return () => clearTimeout(debounceTimer);
    }, [loadComments]);

    // Filter comments
    const filteredComments = state.comments.filter(comment => {
        const matchesSearch = state.searchTerm === '' ||
            comment.content.toLowerCase().includes(state.searchTerm.toLowerCase()) ||
            comment.authorName.toLowerCase().includes(state.searchTerm.toLowerCase()) ||
            comment.authorEmail.toLowerCase().includes(state.searchTerm.toLowerCase());

        const matchesStatus = state.statusFilter === 'all' || comment.status === state.statusFilter;

        return matchesSearch && matchesStatus;
    });

    // Toggle bulk select mode
    const toggleBulkSelectMode = () => {
        setState(prev => ({
            ...prev,
            bulkSelectMode: !prev.bulkSelectMode,
            selectedComments: new Set(),
        }));
    };

    // Toggle comment selection
    const toggleCommentSelection = (commentId: string) => {
        setState(prev => {
            const newSelectedComments = new Set(prev.selectedComments);
            if (newSelectedComments.has(commentId)) {
                newSelectedComments.delete(commentId);
            } else {
                newSelectedComments.add(commentId);
            }
            return { ...prev, selectedComments: newSelectedComments };
        });
    };

    // Select all comments
    const selectAllComments = () => {
        setState(prev => ({
            ...prev,
            selectedComments: new Set(filteredComments.map(comment => comment.id)),
        }));
    };

    // Deselect all comments
    const deselectAllComments = () => {
        setState(prev => ({
            ...prev,
            selectedComments: new Set(),
        }));
    };

    // Update comment status
    const updateCommentStatus = async (commentId: string, status: CommentStatus) => {
        try {
            const updateData: UpdateCommentStatusRequest = { status };
            await blogService.updateCommentStatus(commentId, updateData);
            await loadComments();
        } catch (error) {
            console.error('Failed to update comment status:', error);
        }
    };

    // Approve comment
    const approveComment = async (comment: BlogComment) => {
        setState(prev => ({ ...prev, approvingComment: comment }));
        try {
            await updateCommentStatus(comment.id, CommentStatus.Approved);
        } finally {
            setState(prev => ({ ...prev, approvingComment: null }));
        }
    };

    // Reject comment
    const rejectComment = async (comment: BlogComment) => {
        setState(prev => ({ ...prev, rejectingComment: comment }));
        try {
            await updateCommentStatus(comment.id, CommentStatus.Rejected);
        } finally {
            setState(prev => ({ ...prev, rejectingComment: null }));
        }
    };

    // Delete comment
    const deleteComment = async (comment: BlogComment) => {
        try {
            await blogService.deleteComment(comment.id);
            await loadComments();
            setState(prev => ({ ...prev, deletingComment: null }));
        } catch (error) {
            console.error('Failed to delete comment:', error);
            setState(prev => ({ ...prev, deletingComment: null }));
        }
    };

    // Bulk approve comments
    const bulkApproveComments = async () => {
        try {
            const approvalPromises = Array.from(state.selectedComments).map(commentId =>
                updateCommentStatus(commentId, CommentStatus.Approved)
            );

            await Promise.all(approvalPromises);
            await loadComments();

            setState(prev => ({
                ...prev,
                bulkSelectMode: false,
                selectedComments: new Set(),
            }));
        } catch (error) {
            console.error('Failed to bulk approve comments:', error);
        }
    };

    // Bulk reject comments
    const bulkRejectComments = async () => {
        try {
            const rejectionPromises = Array.from(state.selectedComments).map(commentId =>
                updateCommentStatus(commentId, CommentStatus.Rejected)
            );

            await Promise.all(rejectionPromises);
            await loadComments();

            setState(prev => ({
                ...prev,
                bulkSelectMode: false,
                selectedComments: new Set(),
            }));
        } catch (error) {
            console.error('Failed to bulk reject comments:', error);
        }
    };

    // Open reply modal
    const openReplyModal = (comment: BlogComment) => {
        setState(prev => ({
            ...prev,
            selectedComment: comment,
            replyModalOpen: true,
            replyContent: '',
        }));
    };

    // Close reply modal
    const closeReplyModal = () => {
        setState(prev => ({
            ...prev,
            selectedComment: null,
            replyModalOpen: false,
            replyContent: '',
        }));
    };

    // Reply to comment
    const replyToComment = async () => {
        if (!state.selectedComment || !state.replyContent.trim()) return;

        try {
            setState(prev => ({ ...prev, replying: true }));

            // In a real app, this would be an admin reply
            // For now, we'll create a regular comment as a reply
            const replyData = {
                content: state.replyContent,
                parentId: state.selectedComment.id,
                authorName: 'Admin', // In real app, get from auth
                authorEmail: 'admin@example.com', // In real app, get from auth
            };

            // await blogService.createComment(state.selectedComment.blogId, replyData);
            // For now, just close the modal
            closeReplyModal();
        } catch (error) {
            console.error('Failed to reply to comment:', error);
        } finally {
            setState(prev => ({ ...prev, replying: false }));
        }
    };

    // Get status badge color
    const getStatusBadgeVariant = (status: CommentStatus) => {
        switch (status) {
            case CommentStatus.Approved:
                return 'default';
            case CommentStatus.Pending:
                return 'secondary';
            case CommentStatus.Rejected:
                return 'destructive';
            default:
                return 'secondary';
        }
    };

    // Get status text
    const getStatusText = (status: CommentStatus) => {
        switch (status) {
            case CommentStatus.Approved:
                return ' duyt';
            case CommentStatus.Pending:
                return 'Ch duyt';
            case CommentStatus.Rejected:
                return ' t chi';
            default:
                return 'Khng xc nh';
        }
    };

    const pendingCount = state.comments.filter(c => c.status === CommentStatus.Pending).length;
    const approvedCount = state.comments.filter(c => c.status === CommentStatus.Approved).length;
    const rejectedCount = state.comments.filter(c => c.status === CommentStatus.Rejected).length;

    return (
        <div className="p-6 max-w-6xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Qun l bnh lun</h1>
                    <p className="text-gray-500">Duyt v qun l bnh lun t ngi dng</p>
                </div>
                <div className="flex items-center gap-2">
                    {state.bulkSelectMode ? (
                        <>
                            <Button variant="outline" onClick={toggleBulkSelectMode}>
                                Hy
                            </Button>
                            {state.selectedComments.size > 0 && (
                                <>
                                    <Button onClick={bulkApproveComments}>
                                        <Check className="w-4 h-4 mr-2" />
                                        Duyt ({state.selectedComments.size})
                                    </Button>
                                    <Button
                                        variant="destructive"
                                        onClick={bulkRejectComments}
                                    >
                                        <X className="w-4 h-4 mr-2" />
                                        T chi ({state.selectedComments.size})
                                    </Button>
                                </>
                            )}
                        </>
                    ) : (
                        filteredComments.length > 0 && (
                            <Button variant="outline" onClick={toggleBulkSelectMode}>
                                Chn nhiu
                            </Button>
                        )
                    )}
                </div>
            </div>

            {/* Stats */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <Card>
                    <CardContent className="pt-6">
                        <div className="text-2xl font-bold text-gray-900">{state.comments.length}</div>
                        <p className="text-sm text-gray-500">Tng bnh lun</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardContent className="pt-6">
                        <div className="text-2xl font-bold text-yellow-600">{pendingCount}</div>
                        <p className="text-sm text-gray-500">Ch duyt</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardContent className="pt-6">
                        <div className="text-2xl font-bold text-green-600">{approvedCount}</div>
                        <p className="text-sm text-gray-500"> duyt</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardContent className="pt-6">
                        <div className="text-2xl font-bold text-red-600">{rejectedCount}</div>
                        <p className="text-sm text-gray-500"> t chi</p>
                    </CardContent>
                </Card>
            </div>

            {/* Filters */}
            <Card>
                <CardContent className="pt-6">
                    <div className="flex items-center gap-4">
                        <div className="relative flex-1">
                            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                            <Input
                                value={state.searchTerm}
                                onChange={(e) => setState(prev => ({ ...prev, searchTerm: e.target.value }))}
                                placeholder="Tm kim bnh lun, tn ngi dng, email..."
                                className="pl-10"
                            />
                        </div>

                        <Select
                            value={state.statusFilter}
                            onValueChange={(value: CommentStatus | 'all') =>
                                setState(prev => ({ ...prev, statusFilter: value }))
                            }
                        >
                            <SelectTrigger className="w-48">
                                <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Tt c trng thi</SelectItem>
                                <SelectItem value={CommentStatus.Pending}>Ch duyt</SelectItem>
                                <SelectItem value={CommentStatus.Approved}> duyt</SelectItem>
                                <SelectItem value={CommentStatus.Rejected}> t chi</SelectItem>
                            </SelectContent>
                        </Select>

                        {state.bulkSelectMode && filteredComments.length > 0 && (
                            <div className="flex items-center gap-2">
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={selectAllComments}
                                    disabled={state.selectedComments.size === filteredComments.length}
                                >
                                    Chn tt c
                                </Button>
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={deselectAllComments}
                                    disabled={state.selectedComments.size === 0}
                                >
                                    B chn
                                </Button>
                            </div>
                        )}
                    </div>
                </CardContent>
            </Card>

            {/* Comments Table */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <MessageCircle className="w-5 h-5" />
                        Danh sch bnh lun ({filteredComments.length})
                    </CardTitle>
                    {state.selectedComments.size > 0 && (
                        <CardDescription>
                             chn {state.selectedComments.size} bnh lun
                        </CardDescription>
                    )}
                </CardHeader>
                <CardContent>
                    {state.loading ? (
                        <div className="flex items-center justify-center h-64">
                            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900" />
                        </div>
                    ) : filteredComments.length === 0 ? (
                        <div className="text-center py-12">
                            <MessageCircle className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-900 mb-2">
                                {state.searchTerm || state.statusFilter !== 'all'
                                    ? 'Khng tm thy bnh lun'
                                    : 'Cha c bnh lun no'
                                }
                            </h3>
                            <p className="text-gray-500">
                                {state.searchTerm || state.statusFilter !== 'all'
                                    ? 'Th thay i b lc hoc t kha tm kim'
                                    : 'Bnh lun t ngi dng s hin th  y'
                                }
                            </p>
                        </div>
                    ) : (
                        <div className="space-y-4">
                            {filteredComments.map(comment => (
                                <div
                                    key={comment.id}
                                    className={cn(
                                        "border rounded-lg p-4 space-y-3",
                                        state.selectedComments.has(comment.id) && "bg-blue-50 border-blue-200"
                                    )}
                                >
                                    <div className="flex items-start justify-between">
                                        <div className="flex items-start gap-3 flex-1">
                                            {state.bulkSelectMode && (
                                                <input
                                                    type="checkbox"
                                                    checked={state.selectedComments.has(comment.id)}
                                                    onChange={() => toggleCommentSelection(comment.id)}
                                                    className="mt-1 rounded"
                                                />
                                            )}

                                            <div className="flex-1">
                                                <div className="flex items-center gap-2 mb-2">
                                                    <div className="flex items-center gap-2">
                                                        <User className="w-4 h-4 text-gray-400" />
                                                        <span className="font-medium">{comment.authorName}</span>
                                                        <span className="text-sm text-gray-500">({comment.authorEmail})</span>
                                                    </div>
                                                    <Badge variant={getStatusBadgeVariant(comment.status)}>
                                                        {getStatusText(comment.status)}
                                                    </Badge>
                                                    <span className="text-sm text-gray-500">
                                                        {new Date(comment.createdAt).toLocaleString('vi-VN')}
                                                    </span>
                                                </div>

                                                <p className="text-gray-700 mb-2">{comment.content}</p>

                                                <div className="text-sm text-gray-500">
                                                    Bi vit: <span className="font-medium">{comment.blog?.title}</span>
                                                </div>
                                            </div>
                                        </div>

                                        <DropdownMenu>
                                            <DropdownMenuTrigger asChild>
                                                <Button variant="ghost" size="sm">
                                                    <MoreHorizontal className="w-4 h-4" />
                                                </Button>
                                            </DropdownMenuTrigger>
                                            <DropdownMenuContent align="end">
                                                {comment.status !== CommentStatus.Approved && (
                                                    <DropdownMenuItem onClick={() => approveComment(comment)}>
                                                        <Check className="w-4 h-4 mr-2" />
                                                        Duyt bnh lun
                                                    </DropdownMenuItem>
                                                )}

                                                {comment.status !== CommentStatus.Rejected && (
                                                    <DropdownMenuItem onClick={() => rejectComment(comment)}>
                                                        <X className="w-4 h-4 mr-2" />
                                                        T chi
                                                    </DropdownMenuItem>
                                                )}

                                                <DropdownMenuItem onClick={() => openReplyModal(comment)}>
                                                    <Reply className="w-4 h-4 mr-2" />
                                                    Tr li
                                                </DropdownMenuItem>

                                                <DropdownMenuSeparator />

                                                <DropdownMenuItem
                                                    onClick={() => setState(prev => ({ ...prev, deletingComment: comment }))}
                                                    className="text-red-600"
                                                >
                                                    <Trash2 className="w-4 h-4 mr-2" />
                                                    Xa bnh lun
                                                </DropdownMenuItem>
                                            </DropdownMenuContent>
                                        </DropdownMenu>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Reply Modal */}
            <Dialog open={state.replyModalOpen} onOpenChange={(open) => !open && closeReplyModal()}>
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>Tr li bnh lun</DialogTitle>
                        <DialogDescription>
                            Tr li bnh lun t {state.selectedComment?.authorName}
                        </DialogDescription>
                    </DialogHeader>

                    {state.selectedComment && (
                        <div className="space-y-4">
                            <div className="p-3 bg-gray-50 rounded-lg">
                                <div className="text-sm font-medium mb-1">Bnh lun gc:</div>
                                <p className="text-sm text-gray-700">{state.selectedComment.content}</p>
                            </div>

                            <div>
                                <Label htmlFor="replyContent">Ni dung tr li</Label>
                                <Textarea
                                    id="replyContent"
                                    value={state.replyContent}
                                    onChange={(e) => setState(prev => ({ ...prev, replyContent: e.target.value }))}
                                    placeholder="Nhp ni dung tr li..."
                                    rows={4}
                                />
                            </div>
                        </div>
                    )}

                    <DialogFooter>
                        <Button variant="outline" onClick={closeReplyModal}>
                            Hy
                        </Button>
                        <Button
                            onClick={replyToComment}
                            disabled={state.replying || !state.replyContent.trim()}
                        >
                            {state.replying && <div className="w-4 h-4 mr-2 animate-spin rounded-full border-2 border-current border-t-transparent" />}
                            Gi tr li
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* Delete Confirmation */}
            <AlertDialog
                open={!!state.deletingComment}
                onOpenChange={(open) => !open && setState(prev => ({ ...prev, deletingComment: null }))}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Xa bnh lun</AlertDialogTitle>
                        <AlertDialogDescription>
                            Bn c chc chn mun xa bnh lun t {state.deletingComment?.authorName}?
                            Hnh ng ny khng th hon tc.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel>Hy</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={() => state.deletingComment && deleteComment(state.deletingComment)}
                            className="bg-red-600 hover:bg-red-700"
                        >
                            Xa bnh lun
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </div>
    );
}