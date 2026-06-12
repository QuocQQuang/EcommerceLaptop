import api from '@/lib/admin-api';

import {
    Blog,
    BlogAnalytics,
    BlogAuthor,
    BlogCategory,
    BlogComment,
    BlogMedia,
    BlogMonthlyStat,
    BlogRecentActivity,
    BlogSearchParams,
    BlogStatus,
    BlogStatusType,
    BlogTag,
    BlogTopPost,
    CategorySearchParams,
    CommentSearchParams,
    CommentStatus,
    CreateBlogRequest,
    CreateCategoryRequest,
    CreateCommentRequest,
    CreateTagRequest,
    MediaUploadRequest,
    PaginatedResponse,
    TagSearchParams,
    UpdateBlogRequest,
    UpdateCategoryRequest,
    UpdateCommentStatusRequest,
    UpdateTagRequest
} from '@/types/api';

// Helper functions for mapping
const mapBlogAuthor = (author: any): BlogAuthor => ({
    id: author?.id ?? '',
    name: author?.name ?? ((`${author?.firstName ?? ''} ${author?.lastName ?? ''}`.trim()) || 'Unknown'),
    email: author?.email ?? '',
    firstName: author?.firstName ?? '',
    lastName: author?.lastName ?? '',
    displayName: author?.displayName ?? author?.name ?? '',
    avatarUrl: author?.profilePictureUrl ?? author?.avatarUrl ?? '',
    bio: author?.bio ?? '',
    profilePictureUrl: author?.profilePictureUrl ?? '',
    socialLinks: author?.socialLinks ?? {},
    isActive: author?.isActive ?? true,
    role: author?.role ?? '',
    postCount: author?.postCount ?? 0,
    totalViews: author?.totalViews ?? 0,
    totalComments: author?.totalComments ?? 0,
    followerCount: author?.followerCount ?? 0,
    averageRating: author?.averageRating ?? 0,
    createdAt: author?.createdAt ?? new Date().toISOString(),
    updatedAt: author?.updatedAt ?? author?.createdAt ?? new Date().toISOString(),
    lastActiveAt: author?.lastActiveAt ?? ''
});

const toStringId = (value: unknown): string => {
    if (typeof value === 'string') return value;
    if (typeof value === 'number') return value.toString();
    return '';
};

const deriveCommentStatus = (comment: any): CommentStatus => {
    if (comment?.status && Object.values(CommentStatus).includes(comment.status)) {
        return comment.status as CommentStatus;
    }

    if (comment?.isRejected) {
        return CommentStatus.Rejected;
    }

    if (comment?.isSpam) {
        return CommentStatus.Spam;
    }

    if (comment?.isApproved === true) {
        return CommentStatus.Approved;
    }

    return CommentStatus.Pending;
};

const mapBlogComment = (comment: any): BlogComment => {
    const status = deriveCommentStatus(comment);

    return {
        id: toStringId(comment?.id),
        blogPostId: toStringId(comment?.blogPostId ?? comment?.blogId),
        blogId: toStringId(comment?.blogId ?? comment?.blogPostId),
        content: comment?.content ?? '',
        authorName: comment?.authorName ?? '',
        authorEmail: comment?.authorEmail ?? '',
        authorWebsite: comment?.authorWebsite ?? undefined,
        status,
        isApproved: comment?.isApproved ?? status === CommentStatus.Approved,
        isRejected: comment?.isRejected ?? status === CommentStatus.Rejected,
        isSpam: comment?.isSpam ?? status === CommentStatus.Spam,
        createdAt: comment?.createdAt ?? new Date().toISOString(),
        updatedAt: comment?.updatedAt ?? comment?.createdAt ?? new Date().toISOString(),
        blog: comment?.blog || comment?.blogPost
            ? {
                id: toStringId(comment?.blog?.id ?? comment?.blogPost?.id ?? comment?.blogPostId ?? comment?.blogId),
                title: comment?.blog?.title ?? comment?.blogPost?.title ?? '',
                slug: comment?.blog?.slug ?? comment?.blogPost?.slug ?? ''
            }
            : undefined,
        replies: Array.isArray(comment?.replies) ? comment.replies.map(mapBlogComment) : undefined,
        parentId: comment?.parentId ? toStringId(comment.parentId) : undefined
    };
};

const mapPaginatedComments = (payload: any): PaginatedResponse<BlogComment> => {
    const source = payload?.data ?? payload ?? {};
    const rawItems = Array.isArray(source.items)
        ? source.items
        : Array.isArray(source.data)
            ? source.data
            : [];

    const page = Number(source.page ?? source.pageNumber ?? 1) || 1;
    const pageSize = Number(source.pageSize ?? source.page_size ?? (rawItems.length || 10)) || 10;
    const totalCount = Number(source.totalCount ?? source.total ?? rawItems.length) || rawItems.length;

    return {
        items: rawItems.map(mapBlogComment),
        totalCount,
        page,
        pageSize,
        totalPages: Number(source.totalPages ?? Math.ceil(totalCount / Math.max(pageSize, 1))) || 1,
        hasNextPage: Boolean(source.hasNextPage ?? page * pageSize < totalCount),
        hasPreviousPage: Boolean(source.hasPreviousPage ?? page > 1)
    };
};

const mapBlog = (blog: any): Blog => {
    const tags = Array.isArray(blog?.tags)
        ? blog.tags.map((tag: any) => mapBlogTag(tag))
        : Array.isArray(blog?.blogPostTags)
            ? blog.blogPostTags
                .map((postTag: any) => postTag?.blogTag ?? postTag?.tag)
                .filter(Boolean)
                .map((tag: any) => mapBlogTag(tag))
            : [];
    const tagIds = Array.isArray(blog?.tagIds) ? blog.tagIds.map(Number) : tags.map((tag: BlogTag) => tag.id);

    return {
        id: Number(blog?.id ?? 0),
        title: blog?.title ?? '',
        slug: blog?.slug ?? '',
        excerpt: blog?.excerpt ?? undefined,
        content: blog?.content ?? '',
        featuredImageUrl: blog?.featuredImageUrl ?? blog?.featuredImage ?? undefined,
        metaTitle: blog?.metaTitle ?? undefined,
        metaDescription: blog?.metaDescription ?? undefined,
        status: (blog?.status as BlogStatusType) ?? BlogStatus.Draft,
        publishedAt: blog?.publishedAt ?? undefined,
        isFeatured: Boolean(blog?.isFeatured),
        categoryId: blog?.categoryId ?? undefined,
        authorId: blog?.authorId ?? 0,
        author: blog?.author ? mapBlogAuthor(blog.author) : undefined,
        category: blog?.category ? mapBlogCategory(blog.category) : undefined,
        tags,
        tagIds,
        viewCount: blog?.viewCount ?? 0,
        commentCount: blog?.commentCount ?? (Array.isArray(blog?.comments) ? blog.comments.length : 0),
        likeCount: blog?.likeCount ?? 0,
        createdAt: blog?.createdAt ?? new Date().toISOString(),
        updatedAt: blog?.updatedAt ?? blog?.createdAt ?? new Date().toISOString()
    };
};

const mapBlogMonthlyStat = (stat: any): BlogMonthlyStat => ({
    month: stat?.month ?? stat?.label ?? stat?.period ?? '',
    views: Number(stat?.views ?? stat?.viewCount ?? stat?.totalViews ?? 0) || 0,
    posts: Number(stat?.posts ?? stat?.postCount ?? stat?.totalPosts ?? 0) || 0,
    comments: Number(stat?.comments ?? stat?.commentCount ?? stat?.totalComments ?? 0) || 0
});

const mapTopBlog = (blog: any): BlogTopPost => {
    const status = typeof blog?.status === 'string'
        ? blog.status
        : blog?.isPublished
            ? BlogStatus.Published
            : BlogStatus.Draft;

    return {
        id: toStringId(blog?.id ?? blog?.blogId ?? blog?.postId ?? blog?.slug ?? blog?.title ?? ''),
        title: blog?.title ?? '',
        slug: blog?.slug ?? blog?.blogSlug ?? undefined,
        viewCount: Number(blog?.viewCount ?? blog?.views ?? blog?.totalViews ?? 0) || 0,
        publishedAt: blog?.publishedAt ?? blog?.createdAt ?? new Date().toISOString(),
        status,
        commentCount: typeof blog?.commentCount === 'number' ? blog.commentCount : Number(blog?.comments ?? blog?.totalComments ?? 0) || 0,
        likeCount: typeof blog?.likeCount === 'number' ? blog.likeCount : Number(blog?.likes ?? blog?.totalLikes ?? 0) || 0
    };
};

const mapRecentActivity = (activity: any): BlogRecentActivity => ({
    type: activity?.type ?? activity?.eventType ?? 'unknown',
    title: activity?.title ?? activity?.blogTitle ?? activity?.commentPreview ?? '',
    timestamp: activity?.timestamp ?? activity?.createdAt ?? activity?.date ?? new Date().toISOString(),
    author: activity?.author ?? activity?.userName ?? activity?.authorName ?? undefined,
    blogId: toStringId(activity?.blogId ?? activity?.postId ?? activity?.blogPostId ?? activity?.entityId ?? ''),
    commentId: activity?.commentId ? toStringId(activity.commentId) : undefined,
    metadata: typeof activity?.metadata === 'object' && activity?.metadata !== null ? activity.metadata : undefined
});

const mapBlogTag = (tag: any): BlogTag => {
    const id = Number(tag?.id ?? tag?.tagId ?? tag?.blogTagId ?? 0) || 0;

    return {
        id,
        name: tag?.name ?? '',
        slug: tag?.slug ?? tag?.name ?? '',
        description: tag?.description ?? undefined,
        color: tag?.color ?? '#6B7280',
        isActive: Boolean(tag?.isActive ?? true),
        createdAt: tag?.createdAt ?? new Date().toISOString(),
        updatedAt: tag?.updatedAt ?? tag?.createdAt ?? new Date().toISOString(),
        postCount: typeof tag?.postCount === 'number'
            ? tag.postCount
            : Array.isArray(tag?.blogPostTags)
                ? tag.blogPostTags.length
                : undefined
    };
};

const mapBlogCategory = (category: any): BlogCategory => {
    const id = Number(category?.id ?? category?.categoryId ?? 0) || 0;

    return {
        id,
        name: category?.name ?? '',
        slug: category?.slug ?? category?.name ?? '',
        description: category?.description ?? undefined,
        postCount: typeof category?.postCount === 'number'
            ? category.postCount
            : Array.isArray(category?.blogPosts)
                ? category.blogPosts.length
                : undefined,
        metaTitle: category?.metaTitle ?? undefined,
        metaDescription: category?.metaDescription ?? undefined,
        isActive: Boolean(category?.isActive ?? true),
        sortOrder: Number(category?.sortOrder ?? 0) || 0,
        parentId: typeof category?.parentId === 'number' ? category.parentId : undefined,
        createdAt: category?.createdAt ?? new Date().toISOString(),
        updatedAt: category?.updatedAt ?? category?.createdAt ?? new Date().toISOString()
    };
};

const mapBlogMedia = (media: any): BlogMedia => {
    const id = toStringId(media?.id ?? media?.mediaId ?? media?.fileId ?? media?.sourceId ?? media?.key ?? '');
    const filename = media?.filename ?? media?.name ?? media?.originalFileName ?? '';
    const url = media?.url ?? media?.fileUrl ?? media?.downloadUrl ?? media?.publicUrl ?? media?.path ?? '';
    const mimeType = media?.mimeType ?? media?.contentType ?? media?.type ?? 'application/octet-stream';
    const fileSize = Number(media?.fileSize ?? media?.size ?? media?.length ?? 0) || 0;

    return {
        id,
        filename,
        url,
        mimeType,
        fileSize,
        altText: media?.altText ?? media?.alt ?? media?.description ?? undefined,
        width: typeof media?.width === 'number' ? media.width : undefined,
        height: typeof media?.height === 'number' ? media.height : undefined,
        tags: Array.isArray(media?.tags) ? media.tags.map((tag: any) => String(tag)) : undefined,
        checksum: media?.checksum ?? media?.hash ?? undefined,
        storageProvider: media?.storageProvider ?? media?.provider ?? media?.storage ?? undefined,
        createdAt: media?.createdAt ?? media?.uploadedAt ?? new Date().toISOString(),
        updatedAt: media?.updatedAt ?? media?.modifiedAt ?? media?.createdAt ?? undefined,
        metadata: typeof media?.metadata === 'object' && media?.metadata !== null ? media.metadata : undefined
    };
};

const mapBlogAnalytics = (payload: any): BlogAnalytics => {
    const source = payload?.data ?? payload ?? {};

    const totalPosts = Number(source.totalPosts ?? source.totalBlogs ?? source.total ?? 0) || 0;
    const totalBlogs = Number(source.totalBlogs ?? source.blogCount ?? totalPosts) || totalPosts;
    const publishedPosts = Number(source.publishedPosts ?? source.publishedBlogs ?? source.published ?? 0) || 0;
    const draftPosts = Number(source.draftPosts ?? source.draftBlogs ?? source.draft ?? 0) || 0;
    const scheduledPosts = Number(source.scheduledPosts ?? source.scheduledBlogs ?? source.scheduled ?? 0) || 0;
    const archivedPosts = Number(source.archivedPosts ?? source.archivedBlogs ?? source.archived ?? 0) || 0;
    const totalCategories = Number(source.totalCategories ?? source.categories ?? 0) || 0;
    const totalComments = Number(source.totalComments ?? source.comments ?? 0) || 0;
    const pendingComments = Number(source.pendingComments ?? source.pending ?? 0) || 0;
    const totalViews = Number(source.totalViews ?? source.views ?? 0) || 0;
    const totalLikes = Number(source.totalLikes ?? source.likes ?? 0) || 0;

    const monthlyStats = Array.isArray(source.monthlyStats)
        ? source.monthlyStats.map(mapBlogMonthlyStat)
        : undefined;

    const topBlogs = Array.isArray(source.topBlogs)
        ? source.topBlogs.map(mapTopBlog)
        : undefined;

    const recentActivity = Array.isArray(source.recentActivity)
        ? source.recentActivity.map(mapRecentActivity)
        : undefined;

    const averageViews = Number(source.averageViews ?? source.avgViews ?? NaN);
    const averageComments = Number(source.averageComments ?? source.avgComments ?? NaN);
    const engagementRate = Number(source.engagementRate ?? source.engagement ?? NaN);
    const trendingTags = Array.isArray(source.trendingTags)
        ? source.trendingTags.map(String)
        : undefined;

    return {
        totalPosts,
        totalBlogs,
        publishedPosts,
        publishedBlogs: Number(source.publishedBlogs ?? publishedPosts) || publishedPosts,
        draftPosts,
        draftBlogs: Number(source.draftBlogs ?? draftPosts) || draftPosts,
        scheduledPosts,
        scheduledBlogs: Number(source.scheduledBlogs ?? scheduledPosts) || (scheduledPosts ?? 0),
        archivedPosts,
        archivedBlogs: Number(source.archivedBlogs ?? archivedPosts) || (archivedPosts ?? 0),
        totalCategories,
        totalComments,
        pendingComments,
        totalViews,
        totalLikes,
        averageViews: Number.isFinite(averageViews) ? averageViews : undefined,
        averageComments: Number.isFinite(averageComments) ? averageComments : undefined,
        engagementRate: Number.isFinite(engagementRate) ? engagementRate : undefined,
        monthlyStats,
        topBlogs,
        recentActivity,
        trendingTags
    };
};

export const blogService = {
    // === BLOG OPERATIONS ===

    /**
     * Get paginated list of blog posts
     */
    async getBlogs(params: BlogSearchParams = {}): Promise<PaginatedResponse<Blog>> {
        const queryParams: Record<string, unknown> = {
            pageNumber: params.page ?? params.pageNumber ?? 1,
            pageSize: params.pageSize ?? 10
        };

        if (params.categoryId !== undefined) {
            queryParams.categoryId = params.categoryId;
        }

        const searchTerm = params.search ?? params.searchTerm;
        if (searchTerm) {
            queryParams.searchTerm = searchTerm;
        }

        if (params.status) {
            queryParams.status = params.status;
        }

        if (typeof params.isPublished === 'boolean') {
            queryParams.isPublished = params.isPublished;
        }

        if (typeof params.isFeatured === 'boolean') {
            queryParams.isFeatured = params.isFeatured;
        }

        if (params.authorId !== undefined && params.authorId !== '') {
            queryParams.authorId = params.authorId;
        }

        if (params.sortBy) {
            queryParams.sortBy = params.sortBy;
        }

        if (params.sortOrder) {
            queryParams.sortOrder = params.sortOrder;
        }

        const { data } = await api.get('/blog/posts', { params: queryParams });
        const payload = data.data || data || {};
        const rawItems = Array.isArray(payload.items)
            ? payload.items
            : Array.isArray(payload.data)
                ? payload.data
                : [];

        const totalCount = Number(payload.totalCount ?? payload.total ?? rawItems.length) || 0;
        const page = Number(payload.page ?? payload.pageNumber ?? params.pageNumber ?? 1) || 1;
        const resolvedPageSize = Number(payload.pageSize ?? params.pageSize ?? (rawItems.length || 10));
        const pageSize = resolvedPageSize > 0 ? resolvedPageSize : 10;
        const totalPages = Number(payload.totalPages ?? Math.ceil(totalCount / Math.max(pageSize, 1))) || (totalCount === 0 ? 0 : Math.ceil(totalCount / Math.max(pageSize, 1)));

        return {
            items: rawItems.map(mapBlog),
            totalCount,
            page,
            pageSize,
            totalPages,
            hasNextPage: Boolean(payload.hasNextPage ?? page * pageSize < totalCount),
            hasPreviousPage: Boolean(payload.hasPreviousPage ?? page > 1)
        };
    },

    /**
     * Get a single blog post by ID
     */
    async getBlogById(id: number | string): Promise<Blog> {
        const { data } = await api.get(`/blog/posts/${id}`);
        const payload = data.data || data;
        return mapBlog(payload);
    },

    /**
     * Get a single blog post by slug
     */
    async getBlogBySlug(slug: string): Promise<Blog> {
        const { data } = await api.get(`/blog/posts/slug/${slug}`);
        const payload = data.data || data;
        return mapBlog(payload);
    },

    /**
     * Create a new blog post
     */
    async createBlog(blog: CreateBlogRequest): Promise<Blog> {
        const backendData = {
            title: blog.title,
            excerpt: blog.excerpt,
            content: blog.content,
            featuredImageUrl: blog.featuredImageUrl, // Match controller expecting FeaturedImageUrl
            metaTitle: blog.metaTitle,
            metaDescription: blog.metaDescription,
            isPublished: blog.isPublished ?? false,
            isFeatured: blog.isFeatured ?? false,
            categoryId: blog.categoryId,
            tagIds: blog.tagIds?.map(Number) // Convert to numbers
        };

        const { data } = await api.post('/blog/posts', backendData);
        const payload = data.data || data;
        return mapBlog(payload);
    },

    /**
     * Update an existing blog post
     */
    async updateBlog(id: number | string, blog: Partial<UpdateBlogRequest>): Promise<Blog> {
        const backendData = {
            title: blog.title,
            excerpt: blog.excerpt,
            content: blog.content,
            featuredImageUrl: blog.featuredImageUrl, // backend expects FeaturedImageUrl on update
            metaTitle: blog.metaTitle,
            metaDescription: blog.metaDescription,
            isPublished: blog.isPublished,
            isFeatured: blog.isFeatured,
            categoryId: blog.categoryId,
            tagIds: blog.tagIds?.map(Number),
            status: blog.status,
            slug: (blog as any).slug,
        };
        const { data } = await api.patch(`/blog/posts/${id}`, backendData);
        const payload = data.data || data;
        return mapBlog(payload);
    },

    /**
     * Delete a blog post
     */
    async deleteBlog(id: number | string): Promise<void> {
        await api.delete(`/blog/posts/${id}`);
    },

    /**
     * Publish a blog post
     */
    async publishBlog(id: number | string): Promise<Blog> {
        const { data } = await api.patch(`/blog/posts/${id}/publish`);
        const payload = data.data || data;
        return mapBlog(payload);
    },

    /**
     * Unpublish a blog post
     */
    async unpublishBlog(id: number | string): Promise<Blog> {
        const { data } = await api.patch(`/blog/posts/${id}/unpublish`);
        const payload = data.data || data;
        return mapBlog(payload);
    },

    /**
     * Get blog statistics
     */
    async getBlogAnalytics(): Promise<BlogAnalytics> {
        const { data } = await api.get('/blog/statistics');
        return mapBlogAnalytics(data);
    },

    // === MEDIA OPERATIONS ===

    async getAllMedia(): Promise<BlogMedia[]> {
        const { data } = await api.get('/blog/media');
        const payload = data?.data ?? data ?? [];
        const items = Array.isArray(payload.items) ? payload.items : Array.isArray(payload) ? payload : [];
        return items.map(mapBlogMedia);
    },

    async uploadMedia(request: MediaUploadRequest): Promise<BlogMedia> {
        const formData = new FormData();
        formData.append('file', request.file);

        if (request.alt) {
            formData.append('altText', request.alt);
        }

        if (request.description) {
            formData.append('description', request.description);
        }

        if (request.folder) {
            formData.append('folder', request.folder);
        }

        if (Array.isArray(request.tags)) {
            request.tags.forEach(tag => formData.append('tags', tag));
        }

        if (request.metadata && Object.keys(request.metadata).length > 0) {
            formData.append('metadata', JSON.stringify(request.metadata));
        }

        const { data } = await api.post('/blog/media', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            }
        });

        const payload = data?.data ?? data ?? {};
        return mapBlogMedia(payload);
    },

    async deleteMedia(id: string): Promise<void> {
        await api.delete(`/blog/media/${id}`);
    },

    // === CATEGORY OPERATIONS ===

    /**
     * Get all blog categories
     */
    async getCategories(params: CategorySearchParams = {}): Promise<BlogCategory[]> {
        const queryParams = { activeOnly: params.activeOnly !== false };
        const { data } = await api.get('/blog/categories', { params: queryParams });
        const payload = data?.data ?? data ?? [];
        const items = Array.isArray(payload?.items)
            ? payload.items
            : Array.isArray(payload)
                ? payload
                : [];
        return items.map(mapBlogCategory);
    },

    /**
     * Get all categories for dropdowns
     */
    async getAllCategories(): Promise<BlogCategory[]> {
        const { data } = await api.get('/blog/categories/all');
        const payload = data?.data ?? data ?? [];
        const items = Array.isArray(payload?.items)
            ? payload.items
            : Array.isArray(payload)
                ? payload
                : [];
        return items.map(mapBlogCategory);
    },

    /**
     * Get category by ID
     */
    async getCategoryById(id: number): Promise<BlogCategory> {
        const { data } = await api.get(`/blog/categories/${id}`);
        const payload = data?.data ?? data ?? {};
        return mapBlogCategory(payload);
    },

    /**
     * Create a new category
     */
    async createCategory(category: CreateCategoryRequest): Promise<BlogCategory> {
        // Backend generates slug if not provided
        const backendData = {
            name: category.name,
            slug: category.slug,
            description: category.description,
            metaTitle: category.metaTitle,
            metaDescription: category.metaDescription,
            isActive: category.isActive !== false,
            sortOrder: category.sortOrder || 0
        };
        const { data } = await api.post('/blog/categories', backendData);
        const payload = data?.data ?? data ?? {};
        return mapBlogCategory(payload);
    },

    /**
     * Update a category
     */
    async updateCategory(id: number, category: Partial<UpdateCategoryRequest>): Promise<BlogCategory> {
        const backendData = {
            name: category.name,
            slug: category.slug,
            description: category.description,
            metaTitle: category.metaTitle,
            metaDescription: category.metaDescription,
            isActive: category.isActive,
            sortOrder: category.sortOrder
        };
        const { data } = await api.put(`/blog/categories/${id}`, backendData);
        const payload = data?.data ?? data ?? {};
        return mapBlogCategory(payload);
    },

    /**
     * Delete a category
     */
    async deleteCategory(id: number): Promise<void> {
        await api.delete(`/blog/categories/${id}`);
    },

    // === TAG OPERATIONS ===

    /**
     * Get all blog tags
     */
    async getTags(params: TagSearchParams = {}): Promise<BlogTag[]> {
        const queryParams = { activeOnly: params.activeOnly !== false };
        const { data } = await api.get('/BlogTags', { params: queryParams });
        const payload = data?.data ?? data ?? [];
        const items = Array.isArray(payload?.items)
            ? payload.items
            : Array.isArray(payload)
                ? payload
                : [];
        return items.map(mapBlogTag);
    },

    /**
     * Get all tags for dropdowns/autocomplete
     */
    async getAllTags(): Promise<BlogTag[]> {
        const { data } = await api.get('/BlogTags/all');
        const payload = data?.data ?? data ?? [];
        const items = Array.isArray(payload?.items)
            ? payload.items
            : Array.isArray(payload)
                ? payload
                : [];
        return items.map(mapBlogTag);
    },

    /**
     * Get tag by ID
     */
    async getTagById(id: number): Promise<BlogTag> {
        const { data } = await api.get(`/BlogTags/${id}`);
        const payload = data?.data ?? data ?? {};
        return mapBlogTag(payload);
    },

    /**
     * Create a new tag
     */
    async createTag(tag: CreateTagRequest): Promise<BlogTag> {
        // Backend generates slug if not provided
        const backendData = {
            name: tag.name,
            slug: tag.slug,
            description: tag.description,
            color: tag.color || '#6B7280',
            isActive: tag.isActive !== false
        };
        const { data } = await api.post('/BlogTags', backendData);
        const payload = data?.data ?? data ?? {};
        return mapBlogTag(payload);
    },

    /**
     * Update a tag
     */
    async updateTag(id: number, tag: Partial<UpdateTagRequest>): Promise<BlogTag> {
        const backendData = {
            name: tag.name,
            slug: tag.slug,
            description: tag.description,
            color: tag.color ?? '#6B7280',
            isActive: tag.isActive
        };
        const { data } = await api.put(`/BlogTags/${id}`, backendData);
        const payload = data?.data ?? data ?? {};
        return mapBlogTag(payload);
    },

    /**
     * Delete a tag
     */
    async deleteTag(id: number): Promise<void> {
        await api.delete(`/BlogTags/${id}`);
    },

    // === COMMENT OPERATIONS ===

    /**
     * Get comments for a specific blog post.
     */
    async getCommentsByBlogId(
        blogPostId: number | string,
        pageNumber = 1,
        pageSize = 10,
        status: CommentStatus | 'all' = 'all'
    ): Promise<PaginatedResponse<BlogComment>> {
        const queryParams: Record<string, unknown> = {
            pageNumber,
            pageSize
        };

        if (status === CommentStatus.Approved) {
            queryParams.isApproved = true;
        } else if (status === CommentStatus.Pending) {
            queryParams.isApproved = false;
        }

        const { data } = await api.get(`/blog/posts/${blogPostId}/comments`, { params: queryParams });
        return mapPaginatedComments(data);
    },

    /**
     * Global comment search for admin screens. Falls back to per-post fetch when aggregate endpoint is unavailable.
     */
    async searchComments(params: CommentSearchParams = {}): Promise<PaginatedResponse<BlogComment>> {
        try {
            const queryParams: Record<string, unknown> = {
                pageNumber: params.pageNumber ?? 1,
                pageSize: params.pageSize ?? 10,
                search: params.search
            };

            if (params.status && params.status !== 'all') {
                queryParams.status = params.status;
            }

            if (params.blogId) {
                queryParams.blogId = params.blogId;
            }

            const { data } = await api.get('/blog/comments', { params: queryParams });
            return mapPaginatedComments(data);
        } catch (error) {
            if (params.blogId) {
                return this.getCommentsByBlogId(
                    params.blogId,
                    params.pageNumber ?? 1,
                    params.pageSize ?? 10,
                    params.status ?? 'all'
                );
            }

            console.warn('Aggregate comment search endpoint unavailable. Returning empty result.', error);

            const page = params.pageNumber ?? 1;
            const pageSize = params.pageSize ?? 10;
            return {
                items: [],
                totalCount: 0,
                page,
                pageSize,
                totalPages: 0,
                hasNextPage: false,
                hasPreviousPage: page > 1
            };
        }
    },

    /**
     * Create a new comment for a blog post.
     */
    async createComment(request: CreateCommentRequest): Promise<BlogComment> {
        if (!request.blogId) {
            throw new Error('blogId is required to create a comment');
        }

        const backendPayload = {
            content: request.content,
            authorName: request.authorName,
            authorEmail: request.authorEmail,
            authorWebsite: request.authorWebsite
        };

        const { data } = await api.post(`/blog/posts/${request.blogId}/comments`, backendPayload);
        const payload = data.data || data;
        return mapBlogComment(payload);
    },

    /**
     * Update comment status (approve / reject / spam) for moderation flows.
     */
    async updateCommentStatus(commentId: string, update: UpdateCommentStatusRequest): Promise<void> {
        if (update.status === CommentStatus.Approved) {
            await api.patch(`/blog/comments/${commentId}/approve`);
            return;
        }

        if (update.status === CommentStatus.Rejected) {
            await api.delete(`/blog/comments/${commentId}`);
            return;
        }

        if (update.status === CommentStatus.Spam) {
            await api.delete(`/blog/comments/${commentId}`);
            return;
        }
    },

    /**
     * Delete a comment permanently.
     */
    async deleteComment(id: number | string): Promise<void> {
        await api.delete(`/blog/comments/${id}`);
    },

    // === LIKE OPERATIONS ===

    async likeBlog(id: number | string): Promise<number> {
        const { data } = await api.post(`/blog/posts/${id}/like`);
        const payload = data?.data ?? data ?? {};
        return Number(payload.likeCount ?? payload.count ?? 0) || 0;
    },

    async unlikeBlog(id: number | string): Promise<number> {
        const { data } = await api.post(`/blog/posts/${id}/unlike`);
        const payload = data?.data ?? data ?? {};
        return Number(payload.likeCount ?? payload.count ?? 0) || 0;
    },

    // === UTILITY METHODS ===

    /**
     * Generate slug from title (client-side utility)
     */
    generateSlug(title: string): string {
        return title
            .toLowerCase()
            .replace(/[^a-z0-9\s-]/g, '')
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-')
            .replace(/^-|-$/g, '')
            .trim();
    },

    /**
     * Get reading time estimate for content
     */
    calculateReadingTime(content: string): number {
        const wordsPerMinute = 200;
        const textLength = content.replace(/<[^>]*>/g, '').split(/\s+/).length;
        return Math.ceil(textLength / wordsPerMinute);
    }
};
