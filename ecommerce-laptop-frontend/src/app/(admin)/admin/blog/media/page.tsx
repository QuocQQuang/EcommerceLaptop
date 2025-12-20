'use client';

import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/components/ui/alert-dialog';
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
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { useToast } from '@/hooks/use-toast';
import { cn } from '@/lib/utils';
import { blogService } from '@/services/blogService';
import { BlogMedia, MediaUploadRequest } from '@/types/api';
import {
    Copy,
    Download,
    Eye,
    FileImage,
    Loader2,
    Search,
    Trash2,
    Upload,
    X
} from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';

interface MediaPageState {
    media: BlogMedia[];
    loading: boolean;
    searchTerm: string;
    typeFilter: string;
    selectedMedia: BlogMedia | null;
    previewModalOpen: boolean;
    uploadModalOpen: boolean;
    deletingMedia: BlogMedia | null;
    uploading: boolean;
    bulkSelectMode: boolean;
    selectedMediaItems: Set<string>;
    bulkDeleting: boolean;
    uploadProgress: number;
}

interface UploadFile {
    file: File;
    preview?: string;
}

export default function MediaPage() {
    const { toast } = useToast();
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [state, setState] = useState<MediaPageState>({
        media: [],
        loading: true,
        searchTerm: '',
        typeFilter: 'all',
        selectedMedia: null,
        previewModalOpen: false,
        uploadModalOpen: false,
        deletingMedia: null,
        uploading: false,
        bulkSelectMode: false,
        selectedMediaItems: new Set(),
        bulkDeleting: false,
        uploadProgress: 0,
    });

    const [uploadFiles, setUploadFiles] = useState<UploadFile[]>([]);

    // Load media
    const loadMedia = useCallback(async () => {
        try {
            setState(prev => ({ ...prev, loading: true }));
            const media = await blogService.getAllMedia();
            setState(prev => ({ ...prev, media, loading: false }));
        } catch (error) {
            console.error('Failed to load media:', error);
            setState(prev => ({ ...prev, loading: false }));
        }
    }, []);

    useEffect(() => {
        loadMedia();
    }, [loadMedia]);

    // Filter media
    const filteredMedia = state.media.filter(item => {
        const matchesSearch = state.searchTerm === '' ||
            item.filename.toLowerCase().includes(state.searchTerm.toLowerCase()) ||
            (item.altText || '').toLowerCase().includes(state.searchTerm.toLowerCase());

        const matchesType = state.typeFilter === 'all' ||
            item.mimeType.startsWith(state.typeFilter);

        return matchesSearch && matchesType;
    });

    // Handle file selection
    const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
        const files = Array.from(event.target.files || []);

        const validFiles = files.filter(file => {
            // Check file type
            if (!file.type.startsWith('image/')) {
                toast({
                    title: 'Li nh dng',
                    description: `File ${file.name} khng phi l hnh nh`,
                    variant: 'destructive',
                });
                return false;
            }

            // Check file size (10MB limit)
            if (file.size > 10 * 1024 * 1024) {
                toast({
                    title: 'File qu ln',
                    description: `File ${file.name} vt qu 10MB`,
                    variant: 'destructive',
                });
                return false;
            }

            return true;
        });

        const uploadFiles: UploadFile[] = validFiles.map(file => ({
            file,
            preview: URL.createObjectURL(file),
        }));

        setUploadFiles(uploadFiles);
        setState(prev => ({ ...prev, uploadModalOpen: true }));
    };

    // Upload files
    const uploadFiles_action = async () => {
        if (uploadFiles.length === 0) return;

        try {
            setState(prev => ({ ...prev, uploading: true, uploadProgress: 0 }));

            for (let i = 0; i < uploadFiles.length; i++) {
                const { file } = uploadFiles[i];

                // Create upload request
                const uploadRequest: MediaUploadRequest = {
                    file: file,
                    alt: file.name.replace(/\.[^/.]+$/, '')
                };

                // Upload file
                await blogService.uploadMedia(uploadRequest);

                // Update progress
                setState(prev => ({
                    ...prev,
                    uploadProgress: ((i + 1) / uploadFiles.length) * 100
                }));
            }

            // Cleanup and reload
            uploadFiles.forEach(({ preview }) => {
                if (preview) URL.revokeObjectURL(preview);
            });
            setUploadFiles([]);

            setState(prev => ({ ...prev, uploadModalOpen: false }));
            await loadMedia();

            toast({
                title: 'Upload thnh cng',
                description: ` upload ${uploadFiles.length} file`,
            });
        } catch (error) {
            console.error('Failed to upload files:', error);
            toast({
                title: 'Li upload',
                description: 'Khng th upload files',
                variant: 'destructive',
            });
        } finally {
            setState(prev => ({ ...prev, uploading: false, uploadProgress: 0 }));
        }
    };

    // Remove upload file
    const removeUploadFile = (index: number) => {
        setUploadFiles(prev => {
            const newFiles = [...prev];
            const removed = newFiles.splice(index, 1)[0];
            if (removed.preview) {
                URL.revokeObjectURL(removed.preview);
            }
            return newFiles;
        });
    };

    // Toggle bulk select mode
    const toggleBulkSelectMode = () => {
        setState(prev => ({
            ...prev,
            bulkSelectMode: !prev.bulkSelectMode,
            selectedMediaItems: new Set(),
        }));
    };

    // Toggle media selection
    const toggleMediaSelection = (mediaId: string) => {
        setState(prev => {
            const newSelectedItems = new Set(prev.selectedMediaItems);
            if (newSelectedItems.has(mediaId)) {
                newSelectedItems.delete(mediaId);
            } else {
                newSelectedItems.add(mediaId);
            }
            return { ...prev, selectedMediaItems: newSelectedItems };
        });
    };

    // Select all media
    const selectAllMedia = () => {
        setState(prev => ({
            ...prev,
            selectedMediaItems: new Set(filteredMedia.map(item => item.id)),
        }));
    };

    // Deselect all media
    const deselectAllMedia = () => {
        setState(prev => ({
            ...prev,
            selectedMediaItems: new Set(),
        }));
    };

    // Copy URL to clipboard
    const copyUrl = async (url: string) => {
        try {
            await navigator.clipboard.writeText(url);
            toast({
                title: ' sao chp',
                description: 'URL  c sao chp vo clipboard',
            });
        } catch (error) {
            console.error('Failed to copy URL:', error);
        }
    };

    // Download media
    const downloadMedia = (media: BlogMedia) => {
        const link = document.createElement('a');
        link.href = media.url;
        link.download = media.filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    };

    // Delete media
    const deleteMedia = async (media: BlogMedia) => {
        try {
            await blogService.deleteMedia(media.id);
            await loadMedia();
            setState(prev => ({ ...prev, deletingMedia: null }));

            toast({
                title: ' xa',
                description: ` xa file ${media.filename}`,
            });
        } catch (error) {
            console.error('Failed to delete media:', error);
            toast({
                title: 'Li xa file',
                description: 'Khng th xa file',
                variant: 'destructive',
            });
        }
    };

    // Bulk delete media
    const bulkDeleteMedia = async () => {
        try {
            setState(prev => ({ ...prev, bulkDeleting: true }));

            const deletePromises = Array.from(state.selectedMediaItems).map(mediaId =>
                blogService.deleteMedia(mediaId)
            );

            await Promise.all(deletePromises);
            await loadMedia();

            setState(prev => ({
                ...prev,
                bulkSelectMode: false,
                selectedMediaItems: new Set(),
            }));

            toast({
                title: ' xa',
                description: ` xa ${state.selectedMediaItems.size} files`,
            });
        } catch (error) {
            console.error('Failed to bulk delete media:', error);
            toast({
                title: 'Li xa files',
                description: 'Khng th xa files',
                variant: 'destructive',
            });
        } finally {
            setState(prev => ({ ...prev, bulkDeleting: false }));
        }
    };

    // Open preview modal
    const openPreviewModal = (media: BlogMedia) => {
        setState(prev => ({
            ...prev,
            selectedMedia: media,
            previewModalOpen: true,
        }));
    };

    // Close preview modal
    const closePreviewModal = () => {
        setState(prev => ({
            ...prev,
            selectedMedia: null,
            previewModalOpen: false,
        }));
    };

    // Format file size
    const formatFileSize = (bytes: number) => {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    };

    // Get media type display name
    const getMediaTypeDisplay = (mimeType: string) => {
        if (mimeType.startsWith('image/')) return 'Hnh nh';
        if (mimeType.startsWith('video/')) return 'Video';
        if (mimeType.startsWith('audio/')) return 'Audio';
        return 'Khc';
    };

    const imageCount = state.media.filter(m => m.mimeType.startsWith('image/')).length;
    const videoCount = state.media.filter(m => m.mimeType.startsWith('video/')).length;
    const totalSize = state.media.reduce((sum, m) => sum + m.fileSize, 0);

    return (
        <div className="p-6 max-w-6xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Th vin Media</h1>
                    <p className="text-gray-500">Qun l hnh nh v files cho blog</p>
                </div>
                <div className="flex items-center gap-2">
                    {state.bulkSelectMode ? (
                        <>
                            <Button variant="outline" onClick={toggleBulkSelectMode}>
                                Hy
                            </Button>
                            {state.selectedMediaItems.size > 0 && (
                                <Button
                                    variant="destructive"
                                    onClick={bulkDeleteMedia}
                                    disabled={state.bulkDeleting}
                                >
                                    {state.bulkDeleting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                                    Xa ({state.selectedMediaItems.size})
                                </Button>
                            )}
                        </>
                    ) : (
                        <>
                            {filteredMedia.length > 0 && (
                                <Button variant="outline" onClick={toggleBulkSelectMode}>
                                    Chn nhiu
                                </Button>
                            )}
                            <Button onClick={() => fileInputRef.current?.click()}>
                                <Upload className="w-4 h-4 mr-2" />
                                Upload Media
                            </Button>
                        </>
                    )}
                </div>
            </div>

            {/* Hidden file input */}
            <input
                ref={fileInputRef}
                type="file"
                multiple
                accept="image/*,video/*,audio/*"
                onChange={handleFileSelect}
                className="hidden"
            />

            {/* Stats */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                <Card>
                    <CardContent className="pt-6">
                        <div className="text-2xl font-bold text-gray-900">{state.media.length}</div>
                        <p className="text-sm text-gray-500">Tng files</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardContent className="pt-6">
                        <div className="text-2xl font-bold text-blue-600">{imageCount}</div>
                        <p className="text-sm text-gray-500">Hnh nh</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardContent className="pt-6">
                        <div className="text-2xl font-bold text-green-600">{videoCount}</div>
                        <p className="text-sm text-gray-500">Video</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardContent className="pt-6">
                        <div className="text-2xl font-bold text-purple-600">{formatFileSize(totalSize)}</div>
                        <p className="text-sm text-gray-500">Dung lng</p>
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
                                placeholder="Tm kim files..."
                                className="pl-10"
                            />
                        </div>

                        <Select
                            value={state.typeFilter}
                            onValueChange={(value) =>
                                setState(prev => ({ ...prev, typeFilter: value }))
                            }
                        >
                            <SelectTrigger className="w-48">
                                <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Tt c loi</SelectItem>
                                <SelectItem value="image">Hnh nh</SelectItem>
                                <SelectItem value="video">Video</SelectItem>
                                <SelectItem value="audio">Audio</SelectItem>
                            </SelectContent>
                        </Select>

                        {state.bulkSelectMode && filteredMedia.length > 0 && (
                            <div className="flex items-center gap-2">
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={selectAllMedia}
                                    disabled={state.selectedMediaItems.size === filteredMedia.length}
                                >
                                    Chn tt c
                                </Button>
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={deselectAllMedia}
                                    disabled={state.selectedMediaItems.size === 0}
                                >
                                    B chn
                                </Button>
                            </div>
                        )}
                    </div>
                </CardContent>
            </Card>

            {/* Media Grid */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <FileImage className="w-5 h-5" />
                        Th vin Media ({filteredMedia.length})
                    </CardTitle>
                    {state.selectedMediaItems.size > 0 && (
                        <CardDescription>
                             chn {state.selectedMediaItems.size} files
                        </CardDescription>
                    )}
                </CardHeader>
                <CardContent>
                    {state.loading ? (
                        <div className="flex items-center justify-center h-64">
                            <Loader2 className="w-8 h-8 animate-spin" />
                        </div>
                    ) : filteredMedia.length === 0 ? (
                        <div className="text-center py-12">
                            <FileImage className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-900 mb-2">
                                {state.searchTerm || state.typeFilter !== 'all'
                                    ? 'Khng tm thy file'
                                    : 'Th vin trng'
                                }
                            </h3>
                            <p className="text-gray-500 mb-4">
                                {state.searchTerm || state.typeFilter !== 'all'
                                    ? 'Th thay i b lc hoc t kha tm kim'
                                    : 'Upload files u tin  bt u'
                                }
                            </p>
                            {!state.searchTerm && state.typeFilter === 'all' && (
                                <Button onClick={() => fileInputRef.current?.click()}>
                                    <Upload className="w-4 h-4 mr-2" />
                                    Upload files u tin
                                </Button>
                            )}
                        </div>
                    ) : (
                        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
                            {filteredMedia.map(media => (
                                <div
                                    key={media.id}
                                    className={cn(
                                        "group relative border rounded-lg overflow-hidden bg-white hover:shadow-md transition-shadow",
                                        state.selectedMediaItems.has(media.id) && "ring-2 ring-blue-500"
                                    )}
                                >
                                    {state.bulkSelectMode && (
                                        <div className="absolute top-2 left-2 z-10">
                                            <input
                                                type="checkbox"
                                                checked={state.selectedMediaItems.has(media.id)}
                                                onChange={() => toggleMediaSelection(media.id)}
                                                className="w-4 h-4 rounded"
                                            />
                                        </div>
                                    )}

                                    <div
                                        className="aspect-square bg-gray-100 flex items-center justify-center cursor-pointer"
                                        onClick={() => openPreviewModal(media)}
                                    >
                                        {media.mimeType.startsWith('image/') ? (
                                            <img
                                                src={media.url}
                                                alt={media.altText || media.filename}
                                                className="w-full h-full object-cover"
                                            />
                                        ) : (
                                            <FileImage className="w-12 h-12 text-gray-400" />
                                        )}

                                        <div className="absolute inset-0 bg-black/0 group-hover:bg-black/20 transition-colors flex items-center justify-center">
                                            <div className="opacity-0 group-hover:opacity-100 transition-opacity">
                                                <Button size="sm" variant="secondary">
                                                    <Eye className="w-4 h-4" />
                                                </Button>
                                            </div>
                                        </div>
                                    </div>

                                    <div className="p-3">
                                        <div className="text-sm font-medium truncate mb-1">
                                            {media.filename}
                                        </div>
                                        <div className="flex items-center justify-between text-xs text-gray-500">
                                            <span>{getMediaTypeDisplay(media.mimeType)}</span>
                                            <span>{formatFileSize(media.fileSize)}</span>
                                        </div>

                                        <div className="flex items-center gap-1 mt-2">
                                            <Button
                                                size="sm"
                                                variant="ghost"
                                                onClick={() => copyUrl(media.url)}
                                                className="h-6 px-2"
                                            >
                                                <Copy className="w-3 h-3" />
                                            </Button>
                                            <Button
                                                size="sm"
                                                variant="ghost"
                                                onClick={() => downloadMedia(media)}
                                                className="h-6 px-2"
                                            >
                                                <Download className="w-3 h-3" />
                                            </Button>
                                            <Button
                                                size="sm"
                                                variant="ghost"
                                                onClick={() => setState(prev => ({ ...prev, deletingMedia: media }))}
                                                className="h-6 px-2 text-red-600 hover:text-red-700"
                                            >
                                                <Trash2 className="w-3 h-3" />
                                            </Button>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Upload Modal */}
            <Dialog open={state.uploadModalOpen} onOpenChange={(open) => {
                if (!open && !state.uploading) {
                    setState(prev => ({ ...prev, uploadModalOpen: false }));
                    setUploadFiles([]);
                }
            }}>
                <DialogContent className="sm:max-w-2xl">
                    <DialogHeader>
                        <DialogTitle>Upload Media Files</DialogTitle>
                        <DialogDescription>
                            Upload hnh nh v cc files media cho blog
                        </DialogDescription>
                    </DialogHeader>

                    <div className="space-y-4 max-h-96 overflow-y-auto">
                        {uploadFiles.map((uploadFile, index) => (
                            <div key={index} className="flex items-center gap-3 p-3 border rounded-lg">
                                <div className="w-16 h-16 bg-gray-100 rounded-lg flex items-center justify-center overflow-hidden flex-shrink-0">
                                    {uploadFile.preview ? (
                                        <img
                                            src={uploadFile.preview}
                                            alt={uploadFile.file.name}
                                            className="w-full h-full object-cover"
                                        />
                                    ) : (
                                        <FileImage className="w-8 h-8 text-gray-400" />
                                    )}
                                </div>

                                <div className="flex-1 min-w-0">
                                    <div className="font-medium truncate">{uploadFile.file.name}</div>
                                    <div className="text-sm text-gray-500">
                                        {formatFileSize(uploadFile.file.size)}  {uploadFile.file.type}
                                    </div>
                                </div>

                                {!state.uploading && (
                                    <Button
                                        size="sm"
                                        variant="ghost"
                                        onClick={() => removeUploadFile(index)}
                                    >
                                        <X className="w-4 h-4" />
                                    </Button>
                                )}
                            </div>
                        ))}

                        {state.uploading && (
                            <div className="space-y-2">
                                <div className="flex justify-between text-sm">
                                    <span>ang upload...</span>
                                    <span>{Math.round(state.uploadProgress)}%</span>
                                </div>
                                <div className="w-full bg-gray-200 rounded-full h-2">
                                    <div
                                        className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                                        style={{ width: `${state.uploadProgress}%` }}
                                    />
                                </div>
                            </div>
                        )}
                    </div>

                    <DialogFooter>
                        <Button
                            variant="outline"
                            onClick={() => {
                                setState(prev => ({ ...prev, uploadModalOpen: false }));
                                setUploadFiles([]);
                            }}
                            disabled={state.uploading}
                        >
                            Hy
                        </Button>
                        <Button
                            onClick={uploadFiles_action}
                            disabled={state.uploading || uploadFiles.length === 0}
                        >
                            {state.uploading && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                            Upload ({uploadFiles.length} files)
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* Preview Modal */}
            <Dialog open={state.previewModalOpen} onOpenChange={(open) => !open && closePreviewModal()}>
                <DialogContent className="sm:max-w-2xl">
                    {state.selectedMedia && (
                        <>
                            <DialogHeader>
                                <DialogTitle>{state.selectedMedia.filename}</DialogTitle>
                                <DialogDescription>
                                    {getMediaTypeDisplay(state.selectedMedia.mimeType)}  {formatFileSize(state.selectedMedia.fileSize)}
                                </DialogDescription>
                            </DialogHeader>

                            <div className="space-y-4">
                                <div className="bg-gray-50 rounded-lg p-4 flex items-center justify-center">
                                    {state.selectedMedia.mimeType.startsWith('image/') ? (
                                        <img
                                            src={state.selectedMedia.url}
                                            alt={state.selectedMedia.altText || state.selectedMedia.filename}
                                            className="max-w-full max-h-64 object-contain"
                                        />
                                    ) : (
                                        <FileImage className="w-24 h-24 text-gray-400" />
                                    )}
                                </div>

                                <div className="grid grid-cols-2 gap-4 text-sm">
                                    <div>
                                        <Label>URL</Label>
                                        <div className="flex items-center gap-2 mt-1">
                                            <Input
                                                value={state.selectedMedia.url}
                                                readOnly
                                                className="text-xs"
                                            />
                                            <Button
                                                size="sm"
                                                onClick={() => copyUrl(state.selectedMedia!.url)}
                                            >
                                                <Copy className="w-4 h-4" />
                                            </Button>
                                        </div>
                                    </div>
                                    <div>
                                        <Label>Alt Text</Label>
                                        <Input
                                            value={state.selectedMedia.altText || ''}
                                            readOnly
                                            className="mt-1"
                                        />
                                    </div>
                                </div>

                                <div className="text-xs text-gray-500">
                                    Upload: {new Date(state.selectedMedia.createdAt).toLocaleString('vi-VN')}
                                </div>
                            </div>

                            <DialogFooter>
                                <Button variant="outline" onClick={() => downloadMedia(state.selectedMedia!)}>
                                    <Download className="w-4 h-4 mr-2" />
                                    Download
                                </Button>
                                <Button
                                    variant="destructive"
                                    onClick={() => setState(prev => ({ ...prev, deletingMedia: state.selectedMedia, previewModalOpen: false }))}
                                >
                                    <Trash2 className="w-4 h-4 mr-2" />
                                    Xa
                                </Button>
                            </DialogFooter>
                        </>
                    )}
                </DialogContent>
            </Dialog>

            {/* Delete Confirmation */}
            <AlertDialog
                open={!!state.deletingMedia}
                onOpenChange={(open) => !open && setState(prev => ({ ...prev, deletingMedia: null }))}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Xa file media</AlertDialogTitle>
                        <AlertDialogDescription>
                            Bn c chc chn mun xa file "{state.deletingMedia?.filename}"?
                            Hnh ng ny khng th hon tc.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel>Hy</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={() => state.deletingMedia && deleteMedia(state.deletingMedia)}
                            className="bg-red-600 hover:bg-red-700"
                        >
                            Xa file
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </div>
    );
}