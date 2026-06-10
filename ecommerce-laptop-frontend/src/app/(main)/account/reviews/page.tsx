'use client';

import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from '@/components/ui/alert-dialog';
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
    DialogTrigger,
} from '@/components/ui/dialog';
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
import { reviewService } from '@/services/reviewService';
import {
    Camera,
    Edit3,
    MessageSquare,
    Package,
    Search,
    Star,
    Trash2
} from 'lucide-react';
import { useSession } from 'next-auth/react';
import Image from 'next/image';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface Review {
    id: number;
    productId: number;
    userId: number;
    userName: string;
    userEmail: string;
    rating: number;
    title: string;
    comment: string; // Backend uses 'comment' not 'content'
    createdAt: string;
    isVerifiedPurchase: boolean;
    canEdit?: boolean;
    canDelete?: boolean;
}

export default function ReviewsPage() {
    const { data: session } = useSession();
    const [reviews, setReviews] = useState<Review[]>([]);
    const [filteredReviews, setFilteredReviews] = useState<Review[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [statusFilter, setStatusFilter] = useState<string>('all');
    const [ratingFilter, setRatingFilter] = useState<string>('all');
    const [editingReview, setEditingReview] = useState<Review | null>(null);
    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);

    // Form state
    const [reviewForm, setReviewForm] = useState({
        rating: 5,
        title: '',
        content: '',
        images: [] as string[],
    });

    // Load user's reviews from API
    const loadReviews = async () => {
        if (!session?.user) return;

        setIsLoading(true);
        try {
            const response = await reviewService.getUserReviews(page, 10);
            const reviewsData = response.reviews || [];
            setReviews(reviewsData);
            setTotalPages(Math.ceil((response.totalCount || 0) / 10));
        } catch (error) {
            console.error('Error loading reviews:', error);
            toast.error('Không thể tải danh sách đánh giá');
            setReviews([]);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        loadReviews();
    }, [session, page]);

    useEffect(() => {
        let filtered = reviews;

        // Filter by search term
        if (searchTerm) {
            filtered = filtered.filter(review =>
                review.title?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                review.comment?.toLowerCase().includes(searchTerm.toLowerCase())
            );
        }

        // Skip status filter since backend doesn't provide status information

        // Filter by rating
        if (ratingFilter !== 'all') {
            filtered = filtered.filter(review => review.rating === parseInt(ratingFilter));
        }

        setFilteredReviews(filtered);
    }, [reviews, searchTerm, statusFilter, ratingFilter]);

    const resetForm = () => {
        setReviewForm({
            rating: 5,
            title: '',
            content: '',
            images: [],
        });
        setEditingReview(null);
    };

    const handleEdit = (review: Review) => {
        setEditingReview(review);
        setReviewForm({
            rating: review.rating,
            title: review.title,
            content: review.comment, // Use 'comment' from backend
            images: [], // Backend doesn't provide images, initialize empty
        });
        setIsDialogOpen(true);
    };

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!reviewForm.title.trim() || !reviewForm.content.trim()) {
            toast.error('Vui lòng điền đầy đủ thông tin');
            return;
        }

        setIsSaving(true);

        try {
            if (editingReview) {
                // Update existing review
                await reviewService.updateReview(editingReview.id, {
                    rating: reviewForm.rating,
                    title: reviewForm.title,
                    comment: reviewForm.content // Map 'content' to 'comment' for backend
                });
                toast.success('Cập nhật đánh giá thành công');
                await loadReviews(); // Reload reviews
            }

            setIsDialogOpen(false);
            resetForm();
        } catch (error) {
            console.error('Error saving review:', error);
            toast.error('Có lỗi xảy ra, vui lòng thử lại');
        } finally {
            setIsSaving(false);
        }
    };

    const handleDelete = async (reviewId: number) => {
        try {
            await reviewService.deleteReview(reviewId);
            toast.success('Xóa đánh giá thành công');
            await loadReviews(); // Reload reviews
        } catch (error) {
            console.error('Error deleting review:', error);
            toast.error('Có lỗi xảy ra, vui lòng thử lại');
        }
    };

    const handleImageUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
        const files = event.target.files;
        if (!files) return;

        const newImages: string[] = [];
        for (let i = 0; i < Math.min(files.length, 5 - reviewForm.images.length); i++) {
            const file = files[i];
            if (file.size > 5 * 1024 * 1024) {
                toast.error(`File ${file.name} quá lớn. Vui lòng chọn file nhỏ hơn 5MB`);
                continue;
            }
            // In real app, upload to server and get URL
            newImages.push(URL.createObjectURL(file));
        }

        setReviewForm(prev => ({
            ...prev,
            images: [...prev.images, ...newImages].slice(0, 5)
        }));
    };

    const removeImage = (index: number) => {
        setReviewForm(prev => ({
            ...prev,
            images: prev.images.filter((_, i) => i !== index)
        }));
    };

    const renderStars = (rating: number, size: 'sm' | 'md' = 'sm') => {
        const sizeClass = size === 'sm' ? 'h-4 w-4' : 'h-5 w-5';
        return (
            <div className="flex">
                {[1, 2, 3, 4, 5].map((star) => (
                    <Star
                        key={star}
                        className={`${sizeClass} ${star <= rating
                            ? 'text-yellow-400 fill-current'
                            : 'text-gray-300'
                            }`}
                    />
                ))}
            </div>
        );
    };

    const renderInteractiveStars = (rating: number, onRatingChange: (rating: number) => void) => {
        return (
            <div className="flex space-x-1">
                {[1, 2, 3, 4, 5].map((star) => (
                    <button
                        key={star}
                        type="button"
                        onClick={() => onRatingChange(star)}
                        className="hover:scale-110 transition-transform"
                    >
                        <Star
                            className={`h-6 w-6 ${star <= rating
                                ? 'text-yellow-400 fill-current'
                                : 'text-gray-300 hover:text-yellow-400'
                                }`}
                        />
                    </button>
                ))}
            </div>
        );
    };



    const getStatusBadge = (status: string) => {
        switch (status) {
            case 'approved':
                return <Badge variant="default" className="bg-green-100 text-green-800">Đã duyệt</Badge>;
            case 'pending':
                return <Badge variant="default" className="bg-yellow-100 text-yellow-800">Chờ duyệt</Badge>;
            case 'rejected':
                return <Badge variant="default" className="bg-red-100 text-red-800">Bị từ chối</Badge>;
            default:
                return null;
        }
    };

    if (isLoading) {
        return (
            <div className="space-y-6">
                <Card>
                    <CardHeader>
                        <div className="h-6 bg-gray-200 rounded w-32 animate-pulse"></div>
                        <div className="h-4 bg-gray-200 rounded w-48 animate-pulse"></div>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-4">
                            {[...Array(3)].map((_, i) => (
                                <div key={i} className="h-32 bg-gray-200 rounded animate-pulse"></div>
                            ))}
                        </div>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center space-x-2">
                        <MessageSquare className="h-6 w-6" />
                        <span>Đánh giá của tôi</span>
                    </CardTitle>
                    <CardDescription>
                        Quản lý các đánh giá sản phẩm bạn đã viết
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {/* Filters */}
                    <div className="flex flex-col md:flex-row gap-4 mb-6">
                        <div className="relative flex-1">
                            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                            <Input
                                placeholder="Tìm kiếm theo tên sản phẩm, tiêu đề..."
                                value={searchTerm}
                                onChange={(e) => setSearchTerm(e.target.value)}
                                className="pl-10"
                            />
                        </div>
                        <Select value={statusFilter} onValueChange={setStatusFilter}>
                            <SelectTrigger className="w-full md:w-48">
                                <SelectValue placeholder="Trạng thái" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Tất cả trạng thái</SelectItem>
                                <SelectItem value="pending">Chờ duyệt</SelectItem>
                                <SelectItem value="approved">Đã duyệt</SelectItem>
                                <SelectItem value="rejected">Bị từ chối</SelectItem>
                            </SelectContent>
                        </Select>
                        <Select value={ratingFilter} onValueChange={setRatingFilter}>
                            <SelectTrigger className="w-full md:w-48">
                                <SelectValue placeholder="Đánh giá" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Tất cả đánh giá</SelectItem>
                                <SelectItem value="5">5 sao</SelectItem>
                                <SelectItem value="4">4 sao</SelectItem>
                                <SelectItem value="3">3 sao</SelectItem>
                                <SelectItem value="2">2 sao</SelectItem>
                                <SelectItem value="1">1 sao</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>

                    {/* Reviews List */}
                    {filteredReviews.length === 0 ? (
                        <div className="text-center py-12">
                            <MessageSquare className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-900 mb-2">
                                {searchTerm || statusFilter !== 'all' || ratingFilter !== 'all'
                                    ? 'Không tìm thấy đánh giá'
                                    : 'Chưa có đánh giá nào'
                                }
                            </h3>
                            <p className="text-gray-500">
                                {searchTerm || statusFilter !== 'all' || ratingFilter !== 'all'
                                    ? 'Hãy thay đổi bộ lọc hoặc từ khóa tìm kiếm'
                                    : 'Đánh giá sản phẩm sau khi mua hàng để chia sẻ trải nghiệm'
                                }
                            </p>
                        </div>
                    ) : (
                        <div className="space-y-4">
                            {filteredReviews.map((review) => (
                                <Card key={review.id}>
                                    <CardContent className="pt-6">
                                        <div className="flex flex-col md:flex-row gap-4">
                                            {/* Product Info */}
                                            <div className="flex items-center space-x-4 md:w-1/3">
                                                <div className="w-16 h-16 bg-gray-100 rounded-lg flex-shrink-0 flex items-center justify-center">
                                                    <Package className="w-8 h-8 text-gray-400" />
                                                </div>
                                                <div className="flex-1 min-w-0">
                                                    <h3 className="font-medium text-gray-900 truncate">
                                                        Sản phẩm ID: {review.productId}
                                                    </h3>
                                                    {review.isVerifiedPurchase && (
                                                        <p className="text-sm text-green-600 font-medium">
                                                             Đã xác thực mua hàng
                                                        </p>
                                                    )}
                                                </div>
                                            </div>

                                            {/* Review Content */}
                                            <div className="flex-1">
                                                <div className="flex items-center justify-between mb-2">
                                                    <div className="flex items-center space-x-3">
                                                        {renderStars(review.rating)}
                                                        {review.isVerifiedPurchase && (
                                                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                                                                Đã xác thực
                                                            </span>
                                                        )}
                                                    </div>
                                                    <div className="text-sm text-gray-500">
                                                        {new Date(review.createdAt).toLocaleDateString('vi-VN')}
                                                    </div>
                                                </div>

                                                <h4 className="font-medium text-gray-900 mb-1">
                                                    {review.title}
                                                </h4>
                                                <p className="text-gray-700 text-sm mb-3 line-clamp-2">
                                                    {review.comment}
                                                </p>

                                                {/* Review Date */}
                                                <p className="text-sm text-gray-500 mb-3">
                                                    Ngày đánh giá: {new Date(review.createdAt).toLocaleDateString('vi-VN')}
                                                </p>

                                                {/* Review Actions */}
                                                <div className="flex items-center justify-between">
                                                    <div className="flex items-center space-x-4 text-sm text-gray-500">
                                                        {review.isVerifiedPurchase && (
                                                            <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                                                                <Package className="w-3 h-3 mr-1" />
                                                                Mua hàng đã xác thực
                                                            </span>
                                                        )}
                                                    </div>

                                                    <div className="flex items-center space-x-2">
                                                        <Dialog open={isDialogOpen && editingReview?.id === review.id} onOpenChange={(open) => {
                                                            if (!open) {
                                                                setIsDialogOpen(false);
                                                                resetForm();
                                                            }
                                                        }}>
                                                            <DialogTrigger asChild>
                                                                <Button
                                                                    variant="outline"
                                                                    size="sm"
                                                                    onClick={() => handleEdit(review)}
                                                                >
                                                                    <Edit3 className="h-4 w-4" />
                                                                </Button>
                                                            </DialogTrigger>
                                                            <DialogContent className="sm:max-w-[600px]">
                                                                <DialogHeader>
                                                                    <DialogTitle>Chỉnh sửa đánh giá</DialogTitle>
                                                                    <DialogDescription>
                                                                        Cập nhật đánh giá của bạn về sản phẩm
                                                                    </DialogDescription>
                                                                </DialogHeader>
                                                                <form onSubmit={handleSave} className="space-y-4">
                                                                    <div className="space-y-2">
                                                                        <Label>Đánh giá sao</Label>
                                                                        {renderInteractiveStars(reviewForm.rating, (rating) =>
                                                                            setReviewForm(prev => ({ ...prev, rating }))
                                                                        )}
                                                                    </div>

                                                                    <div className="space-y-2">
                                                                        <Label htmlFor="title">Tiêu đề đánh giá</Label>
                                                                        <Input
                                                                            id="title"
                                                                            value={reviewForm.title}
                                                                            onChange={(e) => setReviewForm(prev => ({ ...prev, title: e.target.value }))}
                                                                            placeholder="Nhập tiêu đề đánh giá"
                                                                            required
                                                                        />
                                                                    </div>

                                                                    <div className="space-y-2">
                                                                        <Label htmlFor="content">Nội dung đánh giá</Label>
                                                                        <Textarea
                                                                            id="content"
                                                                            value={reviewForm.content}
                                                                            onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setReviewForm(prev => ({ ...prev, content: e.target.value }))}
                                                                            placeholder="Chia sẻ trải nghiệm của bạn về sản phẩm..."
                                                                            rows={4}
                                                                            required
                                                                        />
                                                                    </div>

                                                                    <div className="space-y-2">
                                                                        <Label>Hình ảnh (tối đa 5 ảnh)</Label>
                                                                        <div className="flex flex-wrap gap-2">
                                                                            {reviewForm.images.map((image, index) => (
                                                                                <div key={index} className="relative">
                                                                                    <div className="w-20 h-20 bg-gray-100 rounded overflow-hidden">
                                                                                        <Image
                                                                                            src={image}
                                                                                            alt={`Review image ${index + 1}`}
                                                                                            width={80}
                                                                                            height={80}
                                                                                            className="w-full h-full object-cover"
                                                                                        />
                                                                                    </div>
                                                                                    <button
                                                                                        type="button"
                                                                                        onClick={() => removeImage(index)}
                                                                                        className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs hover:bg-red-600"
                                                                                    >
                                                                                        
                                                                                    </button>
                                                                                </div>
                                                                            ))}
                                                                            {reviewForm.images.length < 5 && (
                                                                                <label className="w-20 h-20 border-2 border-dashed border-gray-300 rounded flex items-center justify-center cursor-pointer hover:border-gray-400">
                                                                                    <Camera className="h-6 w-6 text-gray-400" />
                                                                                    <input
                                                                                        type="file"
                                                                                        accept="image/*"
                                                                                        multiple
                                                                                        className="hidden"
                                                                                        onChange={handleImageUpload}
                                                                                    />
                                                                                </label>
                                                                            )}
                                                                        </div>
                                                                        <p className="text-xs text-gray-500">
                                                                            JPG, PNG. Tối đa 5MB mỗi ảnh
                                                                        </p>
                                                                    </div>

                                                                    <DialogFooter>
                                                                        <Button
                                                                            type="button"
                                                                            variant="outline"
                                                                            onClick={() => setIsDialogOpen(false)}
                                                                        >
                                                                        Hủy
                                                                    </Button>
                                                                    <Button type="submit" disabled={isSaving}>
                                                                        {isSaving ? 'đang lưu...' : 'Cập nhật đánh giá'}
                                                                        </Button>
                                                                    </DialogFooter>
                                                                </form>
                                                            </DialogContent>
                                                        </Dialog>
                                                        <AlertDialog>
                                                            <AlertDialogTrigger asChild>
                                                                <Button variant="outline" size="sm">
                                                                    <Trash2 className="h-4 w-4" />
                                                                </Button>
                                                            </AlertDialogTrigger>
                                                            <AlertDialogContent>
                                                                <AlertDialogHeader>
                                                                <AlertDialogTitle>Xóa đánh giá</AlertDialogTitle>
                                                                <AlertDialogDescription>
                                                                    Bạn có chắc chắn muốn xóa đánh giá này? Hành động này không thể hoàn tác.
                                                                    </AlertDialogDescription>
                                                                </AlertDialogHeader>
                                                                <AlertDialogFooter>
                                                                    <AlertDialogCancel>Hủy</AlertDialogCancel>
                                                                    <AlertDialogAction
                                                                        onClick={() => handleDelete(review.id)}
                                                                        className="bg-red-600 hover:bg-red-700"
                                                                    >
                                                                        Xóa
                                                                    </AlertDialogAction>
                                                                </AlertDialogFooter>
                                                            </AlertDialogContent>
                                                        </AlertDialog>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>
                            ))}
                        </div>
                    )}

                    {/* Pagination */}
                    {totalPages > 1 && (
                        <div className="flex justify-center space-x-2 mt-6">
                            <Button
                                variant="outline"
                                onClick={() => setPage(p => Math.max(1, p - 1))}
                                disabled={page === 1}
                            >
                                Trc
                            </Button>

                            {[...Array(totalPages)].map((_, i) => {
                                const pageNumber = i + 1;
                                if (pageNumber === page || Math.abs(pageNumber - page) <= 2) {
                                    return (
                                        <Button
                                            key={pageNumber}
                                            variant={pageNumber === page ? "default" : "outline"}
                                            onClick={() => setPage(pageNumber)}
                                        >
                                            {pageNumber}
                                        </Button>
                                    );
                                }
                                return null;
                            })}

                            <Button
                                variant="outline"
                                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                                disabled={page === totalPages}
                            >
                                Sau
                            </Button>
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}