'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { ProductCard } from '@/components/molecules/ProductCard';
import { SearchBar } from '@/components/molecules/SearchBar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Checkbox } from '@/components/ui/checkbox';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Slider } from '@/components/ui/slider';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { Brand, brandService } from '@/services/brandService';
import { Category, categoryService } from '@/services/categoryService';
import { productService } from '@/services/productService';
import { PaginatedResponse, Product, ProductSearchParams } from '@/types/api';
import { Filter, Grid, List, SlidersHorizontal, X } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';

function ProductsContent() {
    const { selectedCurrency } = useCurrencyContext();
    const searchParams = useSearchParams();
    const [products, setProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState(true);
    const [totalPages, setTotalPages] = useState(0);
    const [totalCount, setTotalCount] = useState(0);
    const [currentPage, setCurrentPage] = useState(1);
    const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');
    const [showFilters, setShowFilters] = useState(false);

    // Filter states
    const [searchQuery, setSearchQuery] = useState('');
    const [selectedBrands, setSelectedBrands] = useState<string[]>([]);
    const [selectedCategories, setSelectedCategories] = useState<string[]>([]);
    const [priceRange, setPriceRange] = useState([0, 100000000]);
    const [sortBy, setSortBy] = useState<'newest' | 'price_asc' | 'price_desc' | 'name' | 'rating'>('newest');

    // Available filter options from API
    const [brands, setBrands] = useState<Brand[]>([]);
    const [categories, setCategories] = useState<Category[]>([]);
    const [filtersLoading, setFiltersLoading] = useState(true);

    // Load search parameters from URL
    useEffect(() => {
        const category = searchParams?.get('category');
        const search = searchParams?.get('search');
        const brand = searchParams?.get('brand');

        if (category) {
            setSelectedCategories([category]);
        }
        if (search) {
            setSearchQuery(search);
        }
        if (brand) {
            setSelectedBrands([brand]);
        }
    }, [searchParams]);

    // Fetch brands and categories
    useEffect(() => {
        const fetchFilters = async () => {
            setFiltersLoading(true);
            try {
                const [brandsData, categoriesData] = await Promise.all([
                    brandService.getBrandsWithCounts(),
                    categoryService.getCategoriesWithProductCount()
                ]);
                setBrands((brandsData || []).filter(b => (b.productCount || 0) > 0));
                setCategories((categoriesData || []).filter(c => (c.productCount || 0) > 0));
            } catch (error) {
                console.error('Failed to fetch filter options:', error);
            } finally {
                setFiltersLoading(false);
            }
        };

        fetchFilters();
    }, []);

    // Fetch products
    useEffect(() => {
        const fetchProducts = async () => {
            setLoading(true);
            try {
                const params: ProductSearchParams = {
                    page: currentPage,
                    pageSize: 12,
                    search: searchQuery || undefined,
                    brand: selectedBrands.length > 0 ? selectedBrands.join(',') : undefined,
                    category: selectedCategories.length > 0 ? selectedCategories.join(',') : undefined,
                    minPrice: priceRange[0] > 0 ? priceRange[0] : undefined,
                    maxPrice: priceRange[1] < 100000000 ? priceRange[1] : undefined,
                    sortBy
                };

                const response: PaginatedResponse<Product> = await productService.getProducts(params);
                // Prioritize in-stock products while preserving backend sort within each group
                const inStock = response.items.filter(p => (p.stockQuantity ?? p.inventory?.availableQuantity ?? 0) > 0);
                const outOfStock = response.items.filter(p => (p.stockQuantity ?? p.inventory?.availableQuantity ?? 0) <= 0);
                setProducts([...inStock, ...outOfStock]);
                setTotalPages(response.totalPages);
                setTotalCount(response.totalCount);
            } catch (error) {
                console.error('Failed to fetch products:', error);
            } finally {
                setLoading(false);
            }
        };

        fetchProducts();
    }, [currentPage, searchQuery, selectedBrands, selectedCategories, priceRange, sortBy]);

    // Handle filter changes
    const handleBrandChange = (brand: string, checked: boolean) => {
        if (checked) {
            setSelectedBrands([...selectedBrands, brand]);
        } else {
            setSelectedBrands(selectedBrands.filter(b => b !== brand));
        }
        setCurrentPage(1);
    };

    const handleCategoryChange = (category: string, checked: boolean) => {
        if (checked) {
            setSelectedCategories([...selectedCategories, category]);
        } else {
            setSelectedCategories(selectedCategories.filter(c => c !== category));
        }
        setCurrentPage(1);
    };

    const handlePriceRangeChange = (value: number[]) => {
        setPriceRange(value);
    };

    const clearAllFilters = () => {
        setSelectedBrands([]);
        setSelectedCategories([]);
        setPriceRange([0, 100000000]);
        setSearchQuery('');
        setCurrentPage(1);
    };

    const formatPrice = (price: number) => {
        return formatCurrencyPrice(price, selectedCurrency);
    };

    const activeFiltersCount = selectedBrands.length + selectedCategories.length +
        (priceRange[0] > 0 || priceRange[1] < 100000000 ? 1 : 0) +
        (searchQuery ? 1 : 0);

    return (
        <div className="container mx-auto px-4 py-8">
            {/* Header */}
            <div className="mb-8">
                <h1 className="text-3xl font-bold text-gray-900 mb-4">
                    Danh Sách Sản Phẩm
                    {totalCount > 0 && (
                        <span className="text-lg font-normal text-gray-600 ml-2">
                            ({totalCount} sản phẩm)
                        </span>
                    )}
                </h1>

                {/* Search and Controls */}
                <div className="flex flex-col lg:flex-row gap-4 items-start lg:items-center justify-between">
                    <div className="w-full lg:w-96">
                        <SearchBar
                            value={searchQuery}
                            onChange={setSearchQuery}
                            placeholder="Tìm kiếm sản phẩm..."
                        />
                    </div>

                    <div className="flex items-center gap-2">
                        {/* Sort */}
                        <Select value={sortBy} onValueChange={(value: 'newest' | 'price_asc' | 'price_desc' | 'name' | 'rating') => setSortBy(value)}>
                            <SelectTrigger className="w-48">
                                <SelectValue placeholder="Sắp xếp theo..." />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="newest">Mới nhất</SelectItem>
                                <SelectItem value="price_asc">Giá tăng dần</SelectItem>
                                <SelectItem value="price_desc">Giá giảm dần</SelectItem>
                                <SelectItem value="name">Tên A-Z</SelectItem>
                                <SelectItem value="rating">Đánh giá cao</SelectItem>
                            </SelectContent>
                        </Select>

                        {/* View Mode */}
                        <div className="flex items-center border rounded-lg">
                            <Button
                                variant={viewMode === 'grid' ? 'default' : 'ghost'}
                                size="sm"
                                onClick={() => setViewMode('grid')}
                                className="rounded-r-none"
                            >
                                <Grid className="w-4 h-4" />
                            </Button>
                            <Button
                                variant={viewMode === 'list' ? 'default' : 'ghost'}
                                size="sm"
                                onClick={() => setViewMode('list')}
                                className="rounded-l-none"
                            >
                                <List className="w-4 h-4" />
                            </Button>
                        </div>

                        {/* Filter Toggle */}
                        <Button
                            variant="outline"
                            onClick={() => setShowFilters(!showFilters)}
                            className="lg:hidden"
                        >
                            <SlidersHorizontal className="w-4 h-4 mr-2" />
                            Lọc
                            {activeFiltersCount > 0 && (
                                <Badge variant="secondary" className="ml-2">
                                    {activeFiltersCount}
                                </Badge>
                            )}
                        </Button>
                    </div>
                </div>

                {/* Active Filters */}
                {activeFiltersCount > 0 && (
                    <div className="flex flex-wrap items-center gap-2 mt-4">
                        <span className="text-sm text-gray-600">Bộ lọc:</span>

                        {selectedBrands.map(brand => (
                            <Badge key={brand} variant="secondary" className="flex items-center gap-1">
                                {brand}
                                <X
                                    className="w-3 h-3 cursor-pointer"
                                    onClick={() => handleBrandChange(brand, false)}
                                />
                            </Badge>
                        ))}

                        {selectedCategories.map(category => (
                            <Badge key={category} variant="secondary" className="flex items-center gap-1">
                                {category}
                                <X
                                    className="w-3 h-3 cursor-pointer"
                                    onClick={() => handleCategoryChange(category, false)}
                                />
                            </Badge>
                        ))}

                        {(priceRange[0] > 0 || priceRange[1] < 100000000) && (
                            <Badge variant="secondary" className="flex items-center gap-1">
                                {formatPrice(priceRange[0])} - {formatPrice(priceRange[1])}
                                <X
                                    className="w-3 h-3 cursor-pointer"
                                    onClick={() => setPriceRange([0, 100000000])}
                                />
                            </Badge>
                        )}

                        {searchQuery && (
                            <Badge variant="secondary" className="flex items-center gap-1">
                                &quot;{searchQuery}&quot;
                                <X
                                    className="w-3 h-3 cursor-pointer"
                                    onClick={() => setSearchQuery('')}
                                />
                            </Badge>
                        )}

                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={clearAllFilters}
                            className="text-red-600 hover:text-red-700"
                        >
                            Xóa tất cả
                        </Button>
                    </div>
                )}
            </div>

            <div className="flex flex-col lg:flex-row gap-8">
                {/* Sidebar Filters */}
                <aside className={`lg:w-64 space-y-6 ${showFilters ? 'block' : 'hidden lg:block'}`}>
                    <div className="bg-white p-6 rounded-lg shadow-sm border">
                        <div className="flex items-center justify-between mb-4">
                            <h3 className="font-semibold text-gray-900">Bộ Lọc</h3>
                            <Filter className="w-4 h-4 text-gray-500" />
                        </div>

                        {/* Brand Filter */}
                        <div className="space-y-3">
                            <h4 className="font-medium text-gray-900">Thương hiệu</h4>
                            {filtersLoading ? (
                                <div className="text-sm text-gray-500">Đang tải...</div>
                            ) : (
                                brands.map(brand => (
                                    <div key={brand.id} className="flex items-center space-x-2">
                                        <Checkbox
                                            id={`brand-${brand.id}`}
                                            checked={selectedBrands.includes(brand.name)}
                                            onCheckedChange={(checked) => handleBrandChange(brand.name, checked as boolean)}
                                        />
                                        <label htmlFor={`brand-${brand.id}`} className="text-sm text-gray-700 cursor-pointer">
                                            {brand.name}
                                            {brand.productCount && (
                                                <span className="text-xs text-gray-500 ml-1">({brand.productCount})</span>
                                            )}
                                        </label>
                                    </div>
                                ))
                            )}
                        </div>

                        <Separator className="my-6" />

                        {/* Category Filter */}
                        <div className="space-y-3">
                            <h4 className="font-medium text-gray-900">Danh mục</h4>
                            {filtersLoading ? (
                                <div className="text-sm text-gray-500">Đang tải...</div>
                            ) : (
                                categories.map(category => (
                                    <div key={category.id} className="flex items-center space-x-2">
                                        <Checkbox
                                            id={`category-${category.id}`}
                                            checked={selectedCategories.includes(category.name)}
                                            onCheckedChange={(checked) => handleCategoryChange(category.name, checked as boolean)}
                                        />
                                        <label htmlFor={`category-${category.id}`} className="text-sm text-gray-700 cursor-pointer">
                                            {category.name}
                                            {category.productCount && (
                                                <span className="text-xs text-gray-500 ml-1">({category.productCount})</span>
                                            )}
                                        </label>
                                    </div>
                                ))
                            )}
                        </div>

                        <Separator className="my-6" />

                        {/* Price Range Filter */}
                        <div className="space-y-3">
                            <h4 className="font-medium text-gray-900">Khoảng giá</h4>
                            <div className="px-2">
                                <Slider
                                    value={priceRange}
                                    onValueChange={handlePriceRangeChange}
                                    max={100000000}
                                    step={1000000}
                                    className="mb-4"
                                />
                                <div className="flex justify-between text-sm text-gray-600">
                                    <span>{formatPrice(priceRange[0])}</span>
                                    <span>{formatPrice(priceRange[1])}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </aside>

                {/* Main Content */}
                <main className="flex-1">
                    {loading ? (
                        <div className="flex justify-center items-center py-12">
                            <LoadingSpinner size="lg" />
                        </div>
                    ) : products.length > 0 ? (
                        <>
                            {/* Products Grid/List */}
                            <div className={
                                viewMode === 'grid'
                                    ? 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6'
                                    : 'space-y-4'
                            }>
                                {products.map((product) => (
                                    <ProductCard
                                        key={product.id}
                                        product={product}
                                        layout={viewMode}
                                    />
                                ))}
                            </div>

                            {/* Pagination */}
                            {totalPages > 1 && (
                                <div className="flex justify-center items-center space-x-2 mt-12">
                                    <Button
                                        variant="outline"
                                        onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                                        disabled={currentPage === 1}
                                    >
                                        Trước
                                    </Button>

                                    {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                                        const page = i + 1;
                                        return (
                                            <Button
                                                key={page}
                                                variant={currentPage === page ? 'default' : 'outline'}
                                                onClick={() => setCurrentPage(page)}
                                                size="sm"
                                            >
                                                {page}
                                            </Button>
                                        );
                                    })}

                                    {totalPages > 5 && (
                                        <>
                                            <span className="text-gray-500">...</span>
                                            <Button
                                                variant={currentPage === totalPages ? 'default' : 'outline'}
                                                onClick={() => setCurrentPage(totalPages)}
                                                size="sm"
                                            >
                                                {totalPages}
                                            </Button>
                                        </>
                                    )}

                                    <Button
                                        variant="outline"
                                        onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
                                        disabled={currentPage === totalPages}
                                    >
                                        Sau
                                    </Button>
                                </div>
                            )}
                        </>
                    ) : (
                        <div className="text-center py-12">
                            <div className="text-gray-500 mb-4">
                                <Filter className="w-12 h-12 mx-auto mb-4 opacity-50" />
                                <h3 className="text-lg font-medium">Không tìm thấy sản phẩm</h3>
                                <p className="text-sm">Hãy thử điều chỉnh bộ lọc hoặc tìm kiếm với từ khóa khác</p>
                            </div>
                            {activeFiltersCount > 0 && (
                                <Button onClick={clearAllFilters} variant="outline">
                                    Xóa tất cả bộ lọc
                                </Button>
                            )}
                        </div>
                    )}
                </main>
            </div>
        </div>
    );
}

export default function ProductsPage() {
    return (
        <Suspense fallback={<LoadingSpinner />}>
            <ProductsContent />
        </Suspense>
    );
}