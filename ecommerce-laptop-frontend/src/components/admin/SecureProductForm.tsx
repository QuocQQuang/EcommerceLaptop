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
        .min(1, 'Tn sn phm l bt buc')
        .min(3, 'Tn sn phm phi c t nht 3 k t')
        .max(200, 'Tn sn phm khng c qu 200 k t')
        .refine((val) => !/[<>\"'&]/.test(val), 'Tn sn phm cha k t khng hp l'),

    sku: z.string()
        .min(1, 'SKU l bt buc')
        .min(3, 'SKU phi c t nht 3 k t')
        .max(50, 'SKU khng c qu 50 k t')
        .regex(/^[A-Z0-9-_]+$/, 'SKU ch c cha ch ci in hoa, s, du gch ngang v gch di'),

    description: z.string()
        .min(10, 'M t phi c t nht 10 k t')
        .max(2000, 'M t khng c qu 2000 k t'),

    shortDescription: z.string()
        .min(10, 'M t ngn phi c t nht 10 k t')
        .max(500, 'M t ngn khng c qu 500 k t'),

    categoryId: z.string().min(1, 'Vui lng chn danh mc'),
    brandId: z.string().min(1, 'Vui lng chn thng hiu'),

    price: z.string()
        .min(1, 'Gi l bt buc')
        .regex(/^\d+(\.\d{1,2})?$/, 'Gi phi l s hp l')
        .refine((val) => parseFloat(val) > 0, 'Gi phi ln hn 0'),

    comparePrice: z.string()
        .optional()
        .refine((val) => !val || /^\d+(\.\d{1,2})?$/.test(val), 'Gi so snh phi l s hp l'),

    costPrice: z.string()
        .optional()
        .refine((val) => !val || /^\d+(\.\d{1,2})?$/.test(val), 'Gi vn phi l s hp l'),

    stock: z.string()
        .min(1, 'S lng tn kho l bt buc')
        .regex(/^\d+$/, 'S lng phi l s nguyn')
        .refine((val) => parseInt(val) >= 0, 'S lng khng c m'),

    lowStockThreshold: z.string()
        .optional()
        .refine((val) => !val || /^\d+$/.test(val), 'Ngng tn kho thp phi l s nguyn'),

    weight: z.string()
        .optional()
        .refine((val) => !val || /^\d+(\.\d{1,2})?$/.test(val), 'Trng lng phi l s hp l'),

    dimensions: z.string()
        .max(100, 'Kch thc khng c qu 100 k t')
        .optional(),

    status: z.enum(['active', 'inactive']),
    isFeatured: z.boolean(),
    productType: z.enum(['Laptop', 'Accessory']),

    // Laptop specific fields
    cpu: z.string().optional().refine((val) => !val || val.length <= 200, 'CPU khng c qu 200 k t'),
    ram: z.string().optional().refine((val) => !val || val.length <= 100, 'RAM khng c qu 100 k t'),
    storage: z.string().optional().refine((val) => !val || val.length <= 100, 'Storage khng c qu 100 k t'),
    gpu: z.string().optional().refine((val) => !val || val.length <= 200, 'GPU khng c qu 200 k t'),
    display: z.string().optional().refine((val) => !val || val.length <= 100, 'Display khng c qu 100 k t'),
    battery: z.string().optional().refine((val) => !val || val.length <= 100, 'Battery khng c qu 100 k t'),
    weight_kg: z.string().optional().refine((val) => !val || /^\d+(\.\d{1,2})?$/.test(val), 'Trng lng phi l s hp l'),
    operatingSystem: z.string().optional().refine((val) => !val || val.length <= 100, 'OS khng c qu 100 k t'),
    ports: z.string().optional().refine((val) => !val || val.length <= 200, 'Ports khng c qu 200 k t'),

    // Accessory specific fields
    accessoryType: z.string().optional().refine((val) => !val || val.length <= 100, 'Loi ph kin khng c qu 100 k t'),
    compatibility: z.string().optional().refine((val) => !val || val.length <= 300, 'Tng thch khng c qu 300 k t'),
    color: z.string().optional().refine((val) => !val || val.length <= 50, 'Mu sc khng c qu 50 k t'),
    material: z.string().optional().refine((val) => !val || val.length <= 100, 'Cht liu khng c qu 100 k t'),
    warranty: z.string().optional().refine((val) => !val || val.length <= 100, 'Bo hnh khng c qu 100 k t'),
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
                toast.error('Vui lng khc phc cc li bo mt trc khi lu');
                return;
            }

            if (overallSecurityScore < 70) {
                toast.error('im bo mt tng th qu thp. Vui lng kim tra li cc trng nhp liu');
                return;
            }

            await onSubmit(data, productImages);
            toast.success('Sn phm  c lu thnh cng');
        } catch (error) {
            console.error('Submit error:', error);
            toast.error('C li xy ra khi lu sn phm');
        }
    };

    return (
        <SecureFormWrapper
            title="Thm sn phm mi"
            description="To sn phm mi vi form nng cao"
            securityLevel="enhanced"
        >
            <div className="space-y-6">
                {/* Security Overview */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Shield className="h-5 w-5" />
                            Tng quan bo mt
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="flex items-center gap-4">
                            <div className="flex items-center gap-2">
                                <Badge
                                    variant={overallSecurityScore > 80 ? "default" : overallSecurityScore > 60 ? "secondary" : "destructive"}
                                >
                                    im bo mt: {overallSecurityScore}/100
                                </Badge>
                            </div>
                            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                {overallSecurityScore > 80 ? (
                                    <>
                                        <CheckCircle className="h-4 w-4 text-green-600" />
                                        Mc  bo mt tt
                                    </>
                                ) : (
                                    <>
                                        <AlertTriangle className="h-4 w-4 text-yellow-600" />
                                        Cn ci thin bo mt
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
                                Thng tin c bn
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="name">Tn sn phm</Label>
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
                                <Label htmlFor="shortDescription">M t ngn</Label>
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
                                <Label htmlFor="description">M t chi tit</Label>
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
                                Phn loi
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                <div className="space-y-2">
                                    <Label>Loi sn phm</Label>
                                    <Select
                                        value={productType}
                                        onValueChange={(value) => form.setValue('productType', value as 'Laptop' | 'Accessory')}
                                    >
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="Laptop">Laptop</SelectItem>
                                            <SelectItem value="Accessory">Ph kin</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>

                                <div className="space-y-2">
                                    <Label>Danh mc</Label>
                                    <Select
                                        value={form.watch('categoryId')}
                                        onValueChange={(value) => form.setValue('categoryId', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Chn danh mc" />
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
                                    <Label>Thng hiu</Label>
                                    <Select
                                        value={form.watch('brandId')}
                                        onValueChange={(value) => form.setValue('brandId', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Chn thng hiu" />
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
                                Gi v tn kho
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="price">Gi bn *</Label>
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
                                    <Label htmlFor="comparePrice">Gi so snh</Label>
                                    <SecureInput
                                        id="comparePrice"
                                        securityContext={SECURITY_CONTEXTS.PRICE}
                                        onSecurityValidation={(result) => handleSecurityValidation('comparePrice', result)}
                                        {...form.register('comparePrice')}
                                    />
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="costPrice">Gi vn</Label>
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
                                    <Label htmlFor="stock">Tn kho *</Label>
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
                                    <Label htmlFor="lowStockThreshold">Ngng tn kho thp</Label>
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
                            <CardTitle>Hnh nh sn phm</CardTitle>
                            <CardDescription>
                                Upload hnh nh sn phm vi form nng cao.
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
                            <CardTitle>Trng thi v ci t</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center justify-between">
                                <div className="space-y-0.5">
                                    <Label>Trng thi sn phm</Label>
                                    <p className="text-sm text-muted-foreground">
                                        Sn phm c c hin th trn website khng
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
                                    <Label>Sn phm ni bt</Label>
                                    <p className="text-sm text-muted-foreground">
                                        Hin th sn phm trong danh sch ni bt
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
                                    ang lu...
                                </>
                            ) : (
                                <>
                                    <Save className="h-4 w-4 mr-2" />
                                    Lu sn phm
                                </>
                            )}
                        </Button>

                        {overallSecurityScore < 70 && (
                            <Alert>
                                <AlertTriangle className="h-4 w-4" />
                                <AlertDescription>
                                    im bo mt qu thp. Vui lng kim tra li cc trng nhp liu.
                                </AlertDescription>
                            </Alert>
                        )}
                    </div>
                </form>
            </div>
        </SecureFormWrapper>
    );
}