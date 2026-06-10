/**
 * Secure Product Form Component
 * Enhanced product creation/editing form with comprehensive security validation
 */

'use client';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle
} from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { zodResolver } from '@hookform/resolvers/zod';
import {
    AlertTriangle,
    CheckCircle,
    DollarSign,
    FileText,
    Package,
    Save,
    Shield,
    Tag
} from 'lucide-react';
import { useCallback, useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';
import ImageUpload, { ProductImage } from './ImageUpload';
import {
    SecureFormWrapper,
    SecureInput,
    SecureTextarea,
    SECURITY_CONTEXTS,
    SecurityValidationResult
} from './SecureForm';

// Enhanced product schema with security validation
const secureProductSchema = z.object({
    name: z.string()
        .min(1, 'Tên sản phẩm là bắt buộc')
        .min(3, 'Tên sản phẩm phải có ít nhất 3 ký tự')
        .max(200, 'Tên sản phẩm không được quá 200 ký tự')
        .refine((val) => !/[<>\"'&]/.test(val), 'Tên sản phẩm chứa ký tự không hợp lệ'),

    sku: z.string()
        .min(1, 'SKU là bắt buộc')
        .min(3, 'SKU phải có ít nhất 3 ký tự')
        .max(50, 'SKU không được quá 50 ký tự')
        .regex(/^[A-Z0-9-_]+$/, 'SKU chỉ được chứa chữ cái in hoa, số, dấu gạch ngang và gạch dưới'),

    description: z.string()
        .min(10, 'Mô tả phải có ít nhất 10 ký tự')
        .max(2000, 'Mô tả không được quá 2000 ký tự'),

    shortDescription: z.string()
        .min(10, 'Mô tả ngắn phải có ít nhất 10 ký tự')
        .max(500, 'Mô tả ngắn không được quá 500 ký tự'),

    categoryId: z.string().min(1, 'Vui lòng chọn danh mục'),
    brandId: z.string().min(1, 'Vui lòng chọn thương hiệu'),

    price: z.string()
        .min(1, 'Giá là bắt buộc')
        .regex(/^\d+(\.\d{1,2})?$/, 'Giá phải là số hợp lệ')
        .refine((val) => parseFloat(val) > 0, 'Giá phải lớn hơn 0'),

    comparePrice: z.string()
        .optional()
        .refine((val) => !val || /^\d+(\.\d{1,2})?$/.test(val), 'Giá so sánh phải là số hợp lệ'),

    costPrice: z.string()
        .optional()
        .refine((val) => !val || /^\d+(\.\d{1,2})?$/.test(val), 'Giá vốn phải là số hợp lệ'),

    stock: z.string()
        .min(1, 'Số lượng tồn kho là bắt buộc')
        .regex(/^\d+$/, 'Số lượng phải là số nguyên')
        .refine((val) => parseInt(val) >= 0, 'Số lượng không được âm'),

    lowStockThreshold: z.string()
        .optional()
        .refine((val) => !val || /^\d+$/.test(val), 'Ngưỡng tồn kho thấp phải là số nguyên'),

    weight: z.string()
        .optional()
        .refine((val) => !val || /^\d+(\.\d{1,2})?$/.test(val), 'Trọng lượng phải là số hợp lệ'),

    dimensions: z.string()
        .max(100, 'Kích thước không được quá 100 ký tự')
        .optional(),

    status: z.enum(['active', 'inactive']),
    isFeatured: z.boolean(),
    productType: z.enum(['Laptop', 'Accessory']),

    // Laptop specific fields
    cpu: z.string().optional().refine((val) => !val || val.length <= 200, 'CPU không được quá 200 ký tự'),
    ram: z.string().optional().refine((val) => !val || val.length <= 100, 'RAM không được quá 100 ký tự'),
    storage: z.string().optional().refine((val) => !val || val.length <= 100, 'Storage không được quá 100 ký tự'),
    gpu: z.string().optional().refine((val) => !val || val.length <= 200, 'GPU không được quá 200 ký tự'),
    display: z.string().optional().refine((val) => !val || val.length <= 100, 'Display không được quá 100 ký tự'),
    battery: z.string().optional().refine((val) => !val || val.length <= 100, 'Battery không được quá 100 ký tự'),
    weight_kg: z.string().optional().refine((val) => !val || /^\d+(\.\d{1,2})?$/.test(val), 'Trọng lượng phải là số hợp lệ'),
    operatingSystem: z.string().optional().refine((val) => !val || val.length <= 100, 'OS không được quá 100 ký tự'),
    ports: z.string().optional().refine((val) => !val || val.length <= 200, 'Ports không được quá 200 ký tự'),

    // Accessory specific fields
    accessoryType: z.string().optional().refine((val) => !val || val.length <= 100, 'Loại phụ kiện không được quá 100 ký tự'),
    compatibility: z.string().optional().refine((val) => !val || val.length <= 300, 'Tương thích không được quá 300 ký tự'),
    color: z.string().optional().refine((val) => !val || val.length <= 50, 'Màu sắc không được quá 50 ký tự'),
    material: z.string().optional().refine((val) => !val || val.length <= 100, 'Chất liệu không được quá 100 ký tự'),
    warranty: z.string().optional().refine((val) => !val || val.length <= 100, 'Bảo hành không được quá 100 ký tự'),
});

type SecureProductFormData = z.infer<typeof secureProductSchema>;

interface SecureProductFormProps {
    onSubmit: (data: SecureProductFormData, images: ProductImage[]) => Promise<void>;
    initialData?: Partial<SecureProductFormData>;
    categories: Array<{ id: string; name: string }>;
    brands: Array<{ id: string; name: string }>;
    isLoading?: boolean;
}

export function SecureProductForm({
    onSubmit,
    initialData,
    categories,
    brands,
    isLoading = false
}: SecureProductFormProps) {
    const [securityValidations, setSecurityValidations] = useState<Record<string, SecurityValidationResult>>({});
    const [productImages, setProductImages] = useState<ProductImage[]>([]);
    const [overallSecurityScore, setOverallSecurityScore] = useState(100);

    const form = useForm<SecureProductFormData>({
        resolver: zodResolver(secureProductSchema),
        defaultValues: {
            name: '',
            sku: '',
            description: '',
            shortDescription: '',
            categoryId: '',
            brandId: '',
            price: '',
            comparePrice: '',
            costPrice: '',
            stock: '0',
            lowStockThreshold: '5',
            weight: '',
            dimensions: '',
            status: 'active',
            isFeatured: false,
            productType: 'Laptop',
            ...initialData
        }
    });

    const productType = form.watch('productType');

    // Handle security validation updates
    const handleSecurityValidation = useCallback((field: string, result: SecurityValidationResult) => {
        setSecurityValidations(prev => ({
            ...prev,
            [field]: result
        }));

        // Calculate overall security score
        const allValidations = { ...securityValidations, [field]: result };
        const scores = Object.values(allValidations).map(v => v.securityScore);
        const avgScore = scores.length > 0 ? scores.reduce((a, b) => a + b, 0) / scores.length : 100;
        setOverallSecurityScore(Math.round(avgScore));
    }, [securityValidations]);

    const handleSubmit = async (data: SecureProductFormData) => {
        try {
            // Final security check
            const hasSecurityErrors = Object.values(securityValidations).some(v => !v.isValid);
            if (hasSecurityErrors) {
                toast.error('Vui lòng khắc phục các lỗi bảo mật trước khi lưu');
                return;
            }

            if (overallSecurityScore < 70) {
                toast.error('Điểm bảo mật tổng thể quá thấp. Vui lòng kiểm tra lại các trường nhập liệu');
                return;
            }

            await onSubmit(data, productImages);
            toast.success('Sản phẩm đã được lưu thành công');
        } catch (error) {
            console.error('Submit error:', error);
            toast.error('Có lỗi xảy ra khi lưu sản phẩm');
        }
    };

    return (
        <SecureFormWrapper
            title="Thêm sản phẩm mới"
            description="Tạo sản phẩm mới với form nâng cao"
            securityLevel="enhanced"
        >
            <div className="space-y-6">
                {/* Security Overview */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Shield className="h-5 w-5" />
                            Tổng quan bảo mật
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="flex items-center gap-4">
                            <div className="flex items-center gap-2">
                                <Badge
                                    variant={overallSecurityScore > 80 ? "default" : overallSecurityScore > 60 ? "secondary" : "destructive"}
                                >
                                    Điểm bảo mật: {overallSecurityScore}/100
                                </Badge>
                            </div>
                            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                {overallSecurityScore > 80 ? (
                                    <>
                                        <CheckCircle className="h-4 w-4 text-green-600" />
                                        Mức độ bảo mật tốt
                                    </>
                                ) : (
                                    <>
                                        <AlertTriangle className="h-4 w-4 text-yellow-600" />
                                        Cần cải thiện bảo mật
                                    </>
                                )}
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6">
                    {/* Basic Information */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <FileText className="h-5 w-5" />
                                Thông tin cơ bản
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="name">Tên sản phẩm</Label>
                                    <SecureInput
                                        id="name"
                                        securityContext={SECURITY_CONTEXTS.NAME}
                                        onSecurityValidation={(result) => handleSecurityValidation('name', result)}
                                        {...form.register('name')}
                                    />
                                    {form.formState.errors.name && (
                                        <p className="text-sm text-red-600">{form.formState.errors.name.message}</p>
                                    )}
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="sku">SKU</Label>
                                    <SecureInput
                                        id="sku"
                                        securityContext={{
                                            ...SECURITY_CONTEXTS.NAME,
                                            pattern: /^[A-Z0-9-_]+$/,
                                            maxLength: 50
                                        }}
                                        onSecurityValidation={(result) => handleSecurityValidation('sku', result)}
                                        {...form.register('sku')}
                                    />
                                    {form.formState.errors.sku && (
                                        <p className="text-sm text-red-600">{form.formState.errors.sku.message}</p>
                                    )}
                                </div>
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="shortDescription">Mô tả ngắn</Label>
                                <SecureTextarea
                                    id="shortDescription"
                                    securityContext={{
                                        ...SECURITY_CONTEXTS.DESCRIPTION,
                                        maxLength: 500
                                    }}
                                    onSecurityValidation={(result) => handleSecurityValidation('shortDescription', result)}
                                    {...form.register('shortDescription')}
                                />
                                {form.formState.errors.shortDescription && (
                                    <p className="text-sm text-red-600">{form.formState.errors.shortDescription.message}</p>
                                )}
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="description">Mô tả chi tiết</Label>
                                <SecureTextarea
                                    id="description"
                                    securityContext={SECURITY_CONTEXTS.DESCRIPTION}
                                    onSecurityValidation={(result) => handleSecurityValidation('description', result)}
                                    rows={6}
                                    {...form.register('description')}
                                />
                                {form.formState.errors.description && (
                                    <p className="text-sm text-red-600">{form.formState.errors.description.message}</p>
                                )}
                            </div>
                        </CardContent>
                    </Card>

                    {/* Category and Brand */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Tag className="h-5 w-5" />
                                Phân loại
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                <div className="space-y-2">
                                    <Label>Loại sản phẩm</Label>
                                    <Select
                                        value={productType}
                                        onValueChange={(value) => form.setValue('productType', value as 'Laptop' | 'Accessory')}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="Laptop">Laptop</SelectItem>
                                            <SelectItem value="Accessory">Phụ kiện</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>

                                <div className="space-y-2">
                                    <Label>Danh mục</Label>
                                    <Select
                                        value={form.watch('categoryId')}
                                        onValueChange={(value) => form.setValue('categoryId', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Chọn danh mục" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {categories.map((category) => (
                                                <SelectItem key={category.id} value={category.id}>
                                                    {category.name}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    {form.formState.errors.categoryId && (
                                        <p className="text-sm text-red-600">{form.formState.errors.categoryId.message}</p>
                                    )}
                                </div>

                                <div className="space-y-2">
                                    <Label>Thương hiệu</Label>
                                    <Select
                                        value={form.watch('brandId')}
                                        onValueChange={(value) => form.setValue('brandId', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Chọn thương hiệu" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {brands.map((brand) => (
                                                <SelectItem key={brand.id} value={brand.id}>
                                                    {brand.name}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    {form.formState.errors.brandId && (
                                        <p className="text-sm text-red-600">{form.formState.errors.brandId.message}</p>
                                    )}
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Pricing and Inventory */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <DollarSign className="h-5 w-5" />
                                Giá và tồn kho
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="price">Giá bán *</Label>
                                    <SecureInput
                                        id="price"
                                        securityContext={SECURITY_CONTEXTS.PRICE}
                                        onSecurityValidation={(result) => handleSecurityValidation('price', result)}
                                        {...form.register('price')}
                                    />
                                    {form.formState.errors.price && (
                                        <p className="text-sm text-red-600">{form.formState.errors.price.message}</p>
                                    )}
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="comparePrice">Giá so sánh</Label>
                                    <SecureInput
                                        id="comparePrice"
                                        securityContext={SECURITY_CONTEXTS.PRICE}
                                        onSecurityValidation={(result) => handleSecurityValidation('comparePrice', result)}
                                        {...form.register('comparePrice')}
                                    />
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="costPrice">Giá vốn</Label>
                                    <SecureInput
                                        id="costPrice"
                                        securityContext={SECURITY_CONTEXTS.PRICE}
                                        onSecurityValidation={(result) => handleSecurityValidation('costPrice', result)}
                                        {...form.register('costPrice')}
                                    />
                                </div>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="stock">Tồn kho *</Label>
                                    <SecureInput
                                        id="stock"
                                        securityContext={SECURITY_CONTEXTS.QUANTITY}
                                        onSecurityValidation={(result) => handleSecurityValidation('stock', result)}
                                        {...form.register('stock')}
                                    />
                                    {form.formState.errors.stock && (
                                        <p className="text-sm text-red-600">{form.formState.errors.stock.message}</p>
                                    )}
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="lowStockThreshold">Ngưỡng tồn kho thấp</Label>
                                    <SecureInput
                                        id="lowStockThreshold"
                                        securityContext={SECURITY_CONTEXTS.QUANTITY}
                                        onSecurityValidation={(result) => handleSecurityValidation('lowStockThreshold', result)}
                                        {...form.register('lowStockThreshold')}
                                    />
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Product Images */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Hình ảnh sản phẩm</CardTitle>
                            <CardDescription>
                                Upload hình ảnh sản phẩm với form nâng cao.
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <ImageUpload
                                images={productImages}
                                onImagesChange={setProductImages}
                                maxImages={10}
                            />
                        </CardContent>
                    </Card>

                    {/* Status and Settings */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Trạng thái và cài đặt</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center justify-between">
                                <div className="space-y-0.5">
                                    <Label>Trạng thái sản phẩm</Label>
                                    <p className="text-sm text-muted-foreground">
                                        Sản phẩm có được hiển thị trên website không
                                    </p>
                                </div>
                                <Switch
                                    checked={form.watch('status') === 'active'}
                                    onCheckedChange={(checked) =>
                                        form.setValue('status', checked ? 'active' : 'inactive')
                                    }
                                />
                            </div>

                            <div className="flex items-center justify-between">
                                <div className="space-y-0.5">
                                    <Label>Sản phẩm nổi bật</Label>
                                    <p className="text-sm text-muted-foreground">
                                        Hiển thị sản phẩm trong danh sách nổi bật
                                    </p>
                                </div>
                                <Switch
                                    checked={form.watch('isFeatured')}
                                    onCheckedChange={(checked) => form.setValue('isFeatured', checked)}
                                />
                            </div>
                        </CardContent>
                    </Card>

                    {/* Submit Button */}
                    <div className="flex items-center gap-4">
                        <Button
                            type="submit"
                            disabled={isLoading || overallSecurityScore < 70}
                            className="min-w-[120px]"
                        >
                            {isLoading ? (
                                <>
                                    <Package className="h-4 w-4 mr-2 animate-spin" />
                                    Đang lưu...
                                </>
                            ) : (
                                <>
                                    <Save className="h-4 w-4 mr-2" />
                                    Lưu sản phẩm
                                </>
                            )}
                        </Button>

                        {overallSecurityScore < 70 && (
                            <Alert>
                                <AlertTriangle className="h-4 w-4" />
                                <AlertDescription>
                                    Điểm bảo mật quá thấp. Vui lòng kiểm tra lại các trường nhập liệu.
                                </AlertDescription>
                            </Alert>
                        )}
                    </div>
                </form>
            </div>
        </SecureFormWrapper>
    );
}
