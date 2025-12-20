'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs';
import { 
  Plus,
  Search,
  Filter,
  MoreHorizontal,
  Edit,
  Trash2,
  Eye,
  FileText,
  Image,
  Tag,
  Calendar,
  Users,
  BookOpen,
  MessageSquare,
  ExternalLink,
  CheckCircle,
  XCircle,
  Clock,
  AlertTriangle
} from 'lucide-react';
import { 
  useAdminAuth,
  PermissionGuard 
} from '@/contexts/AdminAuthContext';
import { PERMISSIONS } from '@/lib/admin-api';

interface BlogPost {
  id: number;
  title: string;
  slug: string;
  excerpt: string;
  content: string;
  featuredImage?: string;
  status: 'draft' | 'published' | 'scheduled' | 'archived';
  author: {
    id: number;
    name: string;
    email: string;
  };
  category: {
    id: number;
    name: string;
    slug: string;
  };
  tags: Array<{
    id: number;
    name: string;
    slug: string;
  }>;
  seo: {
    title?: string;
    description?: string;
    keywords?: string;
  };
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
  viewCount: number;
  commentCount: number;
}

interface ContentCategory {
  id: number;
  name: string;
  slug: string;
  description?: string;
  postCount: number;
  isActive: boolean;
  createdAt: string;
}

interface ContentTag {
  id: number;
  name: string;
  slug: string;
  postCount: number;
  createdAt: string;
}

// Mock API calls - Replace with real API
const useBlogPostsQuery = () => {
  return useQuery({
    queryKey: ['admin', 'blog-posts'],
    queryFn: async (): Promise<{ posts: BlogPost[]; totalCount: number }> => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // Mock data - replace with real API call
      const posts: BlogPost[] = [
        {
          id: 1,
          title: 'Top 10 laptop gaming tt nht nm 2025',
          slug: 'top-10-laptop-gaming-tot-nhat-2025',
          excerpt: 'Khm ph nhng chic laptop gaming mnh m nht vi hiu sut vt tri v thit k n tng.',
          content: 'Ni dung chi tit v laptop gaming...',
          featuredImage: 'https://example.com/gaming-laptops.jpg',
          status: 'published',
          author: {
            id: 1,
            name: 'Nguyn Vn A',
            email: 'author@example.com'
          },
          category: {
            id: 1,
            name: 'nh gi sn phm',
            slug: 'danh-gia-san-pham'
          },
          tags: [
            { id: 1, name: 'Gaming', slug: 'gaming' },
            { id: 2, name: 'Laptop', slug: 'laptop' },
            { id: 3, name: 'Review', slug: 'review' }
          ],
          seo: {
            title: 'Top 10 laptop gaming tt nht 2025 | TechReview',
            description: 'Danh sch laptop gaming tt nht nm 2025 vi nh gi chi tit v hiu sut v gi c.',
            keywords: 'laptop gaming, gaming laptop 2025, laptop chi game'
          },
          publishedAt: '2025-01-22T10:00:00Z',
          createdAt: '2025-01-20T14:30:00Z',
          updatedAt: '2025-01-22T09:45:00Z',
          viewCount: 2543,
          commentCount: 18
        },
        {
          id: 2,
          title: 'Hng dn chn laptop vn phng ph hp',
          slug: 'huong-dan-chon-laptop-van-phong-phu-hop',
          excerpt: 'Nhng tiu ch quan trng khi la chn laptop cho cng vic vn phng hiu qu.',
          content: 'Ni dung hng dn chi tit...',
          status: 'draft',
          author: {
            id: 2,
            name: 'Trn Th B',
            email: 'editor@example.com'
          },
          category: {
            id: 2,
            name: 'Hng dn',
            slug: 'huong-dan'
          },
          tags: [
            { id: 2, name: 'Laptop', slug: 'laptop' },
            { id: 4, name: 'Vn phng', slug: 'van-phong' },
            { id: 5, name: 'Hng dn', slug: 'huong-dan' }
          ],
          seo: {
            title: 'Cch chn laptop vn phng tt nht',
            description: 'Hng dn chi tit cch chn laptop ph hp cho cng vic vn phng.',
            keywords: 'laptop vn phng, chn laptop, laptop lm vic'
          },
          createdAt: '2025-01-22T08:15:00Z',
          updatedAt: '2025-01-22T14:20:00Z',
          viewCount: 0,
          commentCount: 0
        }
      ];

      return { posts, totalCount: posts.length };
    },
    staleTime: 30 * 1000,
  });
};

const useCategoriesQuery = () => {
  return useQuery({
    queryKey: ['admin', 'content-categories'],
    queryFn: async (): Promise<ContentCategory[]> => {
      await new Promise(resolve => setTimeout(resolve, 800));
      
      return [
        {
          id: 1,
          name: 'nh gi sn phm',
          slug: 'danh-gia-san-pham',
          description: 'nh gi chi tit v cc sn phm laptop',
          postCount: 15,
          isActive: true,
          createdAt: '2025-01-01T00:00:00Z'
        },
        {
          id: 2,
          name: 'Hng dn',
          slug: 'huong-dan',
          description: 'Cc bi hng dn s dng v chn mua',
          postCount: 8,
          isActive: true,
          createdAt: '2025-01-01T00:00:00Z'
        },
        {
          id: 3,
          name: 'Tin tc cng ngh',
          slug: 'tin-tuc-cong-nghe',
          description: 'Cp nht tin tc mi nht v cng ngh',
          postCount: 12,
          isActive: true,
          createdAt: '2025-01-01T00:00:00Z'
        }
      ];
    },
    staleTime: 5 * 60 * 1000,
  });
};

const useTagsQuery = () => {
  return useQuery({
    queryKey: ['admin', 'content-tags'],
    queryFn: async (): Promise<ContentTag[]> => {
      await new Promise(resolve => setTimeout(resolve, 600));
      
      return [
        { id: 1, name: 'Gaming', slug: 'gaming', postCount: 12, createdAt: '2025-01-01T00:00:00Z' },
        { id: 2, name: 'Laptop', slug: 'laptop', postCount: 28, createdAt: '2025-01-01T00:00:00Z' },
        { id: 3, name: 'Review', slug: 'review', postCount: 15, createdAt: '2025-01-01T00:00:00Z' },
        { id: 4, name: 'Vn phng', slug: 'van-phong', postCount: 8, createdAt: '2025-01-01T00:00:00Z' },
        { id: 5, name: 'Hng dn', slug: 'huong-dan', postCount: 10, createdAt: '2025-01-01T00:00:00Z' }
      ];
    },
    staleTime: 5 * 60 * 1000,
  });
};

const useDeletePostMutation = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async (postId: number) => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      console.log('Deleting post:', postId);
      return { success: true };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'blog-posts'] });
    }
  });
};

// Vietnamese formatting utilities
const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateString));
};

const formatNumber = (num: number) => {
  return new Intl.NumberFormat('vi-VN').format(num);
};

const getStatusBadge = (status: string) => {
  const statusConfig = {
    draft: { label: 'Nhp', variant: 'outline' as const, icon: Clock },
    published: { label: ' xut bn', variant: 'default' as const, icon: CheckCircle },
    scheduled: { label: ' ln lch', variant: 'secondary' as const, icon: Calendar },
    archived: { label: 'Lu tr', variant: 'destructive' as const, icon: XCircle }
  };

  const config = statusConfig[status as keyof typeof statusConfig];
  const Icon = config?.icon || Clock;

  return (
    <Badge variant={config?.variant || 'outline'} className="flex items-center gap-1">
      <Icon className="h-3 w-3" />
      {config?.label || status}
    </Badge>
  );
};

export default function ContentManagementPage() {
  const router = useRouter();
  const { user } = useAdminAuth();
  
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [selectedPost, setSelectedPost] = useState<BlogPost | null>(null);
  const [activeTab, setActiveTab] = useState('posts');

  const { 
    data: postsData, 
    isLoading: postsLoading, 
    error: postsError, 
    refetch: refetchPosts 
  } = useBlogPostsQuery();

  const { 
    data: categories, 
    isLoading: categoriesLoading 
  } = useCategoriesQuery();

  const { 
    data: tags, 
    isLoading: tagsLoading 
  } = useTagsQuery();

  const deletePostMutation = useDeletePostMutation();

  // Filter posts based on search and filters
  const filteredPosts = postsData?.posts?.filter(post => {
    const matchesSearch = post.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         post.excerpt.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         post.author.name.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesStatus = statusFilter === 'all' || post.status === statusFilter;
    const matchesCategory = categoryFilter === 'all' || post.category.id.toString() === categoryFilter;
    
    return matchesSearch && matchesStatus && matchesCategory;
  }) || [];

  const handleDeletePost = async () => {
    if (!selectedPost) return;

    try {
      await deletePostMutation.mutateAsync(selectedPost.id);
      setShowDeleteDialog(false);
      setSelectedPost(null);
    } catch (error) {
      console.error('Error deleting post:', error);
    }
  };

  if (postsError) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <AlertTriangle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold">C li xy ra</h3>
          <p className="text-muted-foreground mb-4">
            Khng th ti ni dung. Vui lng th li.
          </p>
          <Button onClick={() => refetchPosts()}>Th li</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Qun l ni dung</h1>
          <p className="text-muted-foreground">
            Qun l blog, bi vit v ni dung website
          </p>
        </div>
        
        <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
          <Link href="/content/posts/add">
            <Button>
              <Plus className="h-4 w-4 mr-2" />
              Thm bi vit
            </Button>
          </Link>
        </PermissionGuard>
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="posts" className="flex items-center gap-2">
            <FileText className="h-4 w-4" />
            Bi vit
          </TabsTrigger>
          <TabsTrigger value="categories" className="flex items-center gap-2">
            <BookOpen className="h-4 w-4" />
            Danh mc
          </TabsTrigger>
          <TabsTrigger value="tags" className="flex items-center gap-2">
            <Tag className="h-4 w-4" />
            Tags
          </TabsTrigger>
          <TabsTrigger value="media" className="flex items-center gap-2">
            <Image className="h-4 w-4" />
            Th vin media
          </TabsTrigger>
        </TabsList>

        {/* Posts Tab */}
        <TabsContent value="posts" className="space-y-6">
          {/* Filters */}
          <Card>
            <CardHeader>
              <CardTitle>B lc bi vit</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex flex-col sm:flex-row gap-4">
                <div className="flex-1">
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
                    <Input
                      placeholder="Tm kim tiu , ni dung hoc tc gi..."
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                      className="pl-10"
                    />
                  </div>
                </div>
                
                <div className="flex gap-2">
                  <Select value={statusFilter} onValueChange={setStatusFilter}>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="Trng thi" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">Tt c trng thi</SelectItem>
                      <SelectItem value="published"> xut bn</SelectItem>
                      <SelectItem value="draft">Nhp</SelectItem>
                      <SelectItem value="scheduled"> ln lch</SelectItem>
                      <SelectItem value="archived">Lu tr</SelectItem>
                    </SelectContent>
                  </Select>

                  <Select value={categoryFilter} onValueChange={setCategoryFilter}>
                    <SelectTrigger className="w-40">
                      <SelectValue placeholder="Danh mc" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">Tt c danh mc</SelectItem>
                      {categories?.map((category) => (
                        <SelectItem key={category.id} value={category.id.toString()}>
                          {category.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Posts Table */}
          <Card>
            <CardHeader>
              <CardTitle>
                Danh sch bi vit ({filteredPosts.length})
              </CardTitle>
            </CardHeader>
            <CardContent>
              {postsLoading ? (
                <div className="space-y-3">
                  {[...Array(5)].map((_, i) => (
                    <div key={i} className="h-16 bg-muted animate-pulse rounded" />
                  ))}
                </div>
              ) : filteredPosts.length === 0 ? (
                <div className="text-center py-12">
                  <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                  <h3 className="text-lg font-semibold mb-2">Cha c bi vit no</h3>
                  <p className="text-muted-foreground mb-4">
                    {searchTerm || statusFilter !== 'all' || categoryFilter !== 'all'
                      ? 'Khng tm thy bi vit no ph hp vi b lc.'
                      : 'Bt u to bi vit u tin ca bn.'}
                  </p>
                  {(!searchTerm && statusFilter === 'all' && categoryFilter === 'all') && (
                    <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                      <Link href="/content/posts/add">
                        <Button>
                          <Plus className="h-4 w-4 mr-2" />
                          Thm bi vit
                        </Button>
                      </Link>
                    </PermissionGuard>
                  )}
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Tiu </TableHead>
                        <TableHead>Tc gi</TableHead>
                        <TableHead>Danh mc</TableHead>
                        <TableHead>Trng thi</TableHead>
                        <TableHead>Lt xem</TableHead>
                        <TableHead>Ngy to</TableHead>
                        <TableHead className="text-right">Thao tc</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {filteredPosts.map((post) => (
                        <TableRow key={post.id}>
                          <TableCell>
                            <div className="max-w-md">
                              <div className="font-medium line-clamp-1">{post.title}</div>
                              <div className="text-sm text-muted-foreground line-clamp-2">
                                {post.excerpt}
                              </div>
                              <div className="flex gap-1 mt-1">
                                {post.tags.slice(0, 3).map((tag) => (
                                  <Badge key={tag.id} variant="secondary" className="text-xs">
                                    {tag.name}
                                  </Badge>
                                ))}
                                {post.tags.length > 3 && (
                                  <Badge variant="outline" className="text-xs">
                                    +{post.tags.length - 3}
                                  </Badge>
                                )}
                              </div>
                            </div>
                          </TableCell>
                          <TableCell>
                            <div>
                              <div className="font-medium">{post.author.name}</div>
                              <div className="text-sm text-muted-foreground">
                                {post.author.email}
                              </div>
                            </div>
                          </TableCell>
                          <TableCell>
                            <Badge variant="outline">{post.category.name}</Badge>
                          </TableCell>
                          <TableCell>
                            {getStatusBadge(post.status)}
                            {post.publishedAt && (
                              <div className="text-xs text-muted-foreground mt-1">
                                {formatDate(post.publishedAt)}
                              </div>
                            )}
                          </TableCell>
                          <TableCell>
                            <div className="flex items-center gap-4 text-sm">
                              <div className="flex items-center gap-1">
                                <Eye className="h-4 w-4 text-muted-foreground" />
                                {formatNumber(post.viewCount)}
                              </div>
                              <div className="flex items-center gap-1">
                                <MessageSquare className="h-4 w-4 text-muted-foreground" />
                                {formatNumber(post.commentCount)}
                              </div>
                            </div>
                          </TableCell>
                          <TableCell>
                            <div className="text-sm">
                              {formatDate(post.createdAt)}
                            </div>
                          </TableCell>
                          <TableCell className="text-right">
                            <DropdownMenu>
                              <DropdownMenuTrigger asChild>
                                <Button variant="ghost" className="h-8 w-8 p-0">
                                  <MoreHorizontal className="h-4 w-4" />
                                </Button>
                              </DropdownMenuTrigger>
                              <DropdownMenuContent align="end">
                                <DropdownMenuLabel>Thao tc</DropdownMenuLabel>
                                
                                <PermissionGuard permission={PERMISSIONS.PRODUCTS_READ}>
                                  <DropdownMenuItem
                                    onClick={() => router.push(`/content/posts/${post.id}`)}
                                  >
                                    <Eye className="h-4 w-4 mr-2" />
                                    Xem chi tit
                                  </DropdownMenuItem>

                                  {post.status === 'published' && (
                                    <DropdownMenuItem asChild>
                                      <a 
                                        href={`/blog/${post.slug}`} 
                                        target="_blank" 
                                        rel="noopener noreferrer"
                                      >
                                        <ExternalLink className="h-4 w-4 mr-2" />
                                        Xem trn web
                                      </a>
                                    </DropdownMenuItem>
                                  )}
                                </PermissionGuard>

                                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                                  <DropdownMenuItem
                                    onClick={() => router.push(`/content/posts/${post.id}/edit`)}
                                  >
                                    <Edit className="h-4 w-4 mr-2" />
                                    Chnh sa
                                  </DropdownMenuItem>

                                  <DropdownMenuSeparator />

                                  <DropdownMenuItem
                                    className="text-red-600"
                                    onClick={() => {
                                      setSelectedPost(post);
                                      setShowDeleteDialog(true);
                                    }}
                                  >
                                    <Trash2 className="h-4 w-4 mr-2" />
                                    Xa
                                  </DropdownMenuItem>
                                </PermissionGuard>
                              </DropdownMenuContent>
                            </DropdownMenu>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Categories Tab */}
        <TabsContent value="categories" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                Danh mc bi vit
                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                  <Button size="sm">
                    <Plus className="h-4 w-4 mr-2" />
                    Thm danh mc
                  </Button>
                </PermissionGuard>
              </CardTitle>
            </CardHeader>
            <CardContent>
              {categoriesLoading ? (
                <div className="space-y-3">
                  {[...Array(3)].map((_, i) => (
                    <div key={i} className="h-16 bg-muted animate-pulse rounded" />
                  ))}
                </div>
              ) : (
                <div className="space-y-4">
                  {categories?.map((category) => (
                    <div key={category.id} className="flex items-center justify-between p-4 border rounded-lg">
                      <div className="flex-1">
                        <div className="font-medium">{category.name}</div>
                        <div className="text-sm text-muted-foreground">
                          {category.description}
                        </div>
                        <div className="text-xs text-muted-foreground mt-1">
                          {formatNumber(category.postCount)} bi vit  To {formatDate(category.createdAt)}
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge variant={category.isActive ? 'default' : 'secondary'}>
                          {category.isActive ? 'Hot ng' : 'Tm dng'}
                        </Badge>
                        <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button variant="ghost" size="sm">
                                <MoreHorizontal className="h-4 w-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent>
                              <DropdownMenuItem>
                                <Edit className="h-4 w-4 mr-2" />
                                Chnh sa
                              </DropdownMenuItem>
                              <DropdownMenuItem className="text-red-600">
                                <Trash2 className="h-4 w-4 mr-2" />
                                Xa
                              </DropdownMenuItem>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </PermissionGuard>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Tags Tab */}
        <TabsContent value="tags" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                Tags bi vit
                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                  <Button size="sm">
                    <Plus className="h-4 w-4 mr-2" />
                    Thm tag
                  </Button>
                </PermissionGuard>
              </CardTitle>
            </CardHeader>
            <CardContent>
              {tagsLoading ? (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {[...Array(6)].map((_, i) => (
                    <div key={i} className="h-16 bg-muted animate-pulse rounded" />
                  ))}
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {tags?.map((tag) => (
                    <div key={tag.id} className="p-4 border rounded-lg">
                      <div className="flex items-center justify-between mb-2">
                        <Badge variant="outline" className="font-medium">
                          #{tag.name}
                        </Badge>
                        <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button variant="ghost" size="sm">
                                <MoreHorizontal className="h-4 w-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent>
                              <DropdownMenuItem>
                                <Edit className="h-4 w-4 mr-2" />
                                Chnh sa
                              </DropdownMenuItem>
                              <DropdownMenuItem className="text-red-600">
                                <Trash2 className="h-4 w-4 mr-2" />
                                Xa
                              </DropdownMenuItem>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </PermissionGuard>
                      </div>
                      <div className="text-sm text-muted-foreground">
                        {formatNumber(tag.postCount)} bi vit
                      </div>
                      <div className="text-xs text-muted-foreground">
                        To {formatDate(tag.createdAt)}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Media Tab */}
        <TabsContent value="media" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                Th vin media
                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                  <Button size="sm">
                    <Plus className="h-4 w-4 mr-2" />
                    Upload media
                  </Button>
                </PermissionGuard>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-center py-12">
                <Image className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-semibold mb-2">Th vin media trng</h3>
                <p className="text-muted-foreground mb-4">
                  Upload hnh nh v video  s dng trong bi vit
                </p>
                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                  <Button>
                    <Plus className="h-4 w-4 mr-2" />
                    Upload media u tin
                  </Button>
                </PermissionGuard>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Delete Confirmation Dialog */}
      <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xc nhn xa bi vit</DialogTitle>
            <DialogDescription>
              Bn c chc chn mun xa bi vit &quot;{selectedPost?.title}&quot;?
              Hnh ng ny khng th hon tc.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button 
              variant="outline" 
              onClick={() => setShowDeleteDialog(false)}
            >
              Hy b
            </Button>
            <Button 
              variant="destructive" 
              onClick={handleDeletePost}
              disabled={deletePostMutation.isPending}
            >
              {deletePostMutation.isPending ? 'ang xa...' : 'Xa bi vit'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}