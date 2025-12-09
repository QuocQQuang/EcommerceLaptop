'use client';

import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { Textarea } from '@/components/ui/textarea';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { PERMISSIONS } from '@/lib/admin-api';
import { formatCurrencyPrice } from '@/lib/currency';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  AlertCircle,
  ArrowLeft,
  Calendar,
  DollarSign,
  Info,
  Percent,
  Save,
  Tag,
  Truck
} from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

// Validation schema
const promotionSchema = z.object({
  name: z.string().min(1, 'Tn khuyn mi l bt buc').max(100, 'Tn khng c vt qu 100 k t'),
  code: z.string()
    .min(3, 'M khuyn mi phi c t nht 3 k t')
    .max(20, 'M khng c vt qu 20 k t')
    .regex(/^[A-Z0-9]+$/, 'M ch c cha ch hoa v s'),
  description: z.string().max(500, 'M t khng c vt qu 500 k t').optional(),
  type: z.enum(['percentage', 'fixed_amount', 'free_shipping'], {
    required_error: 'Vui lng chn loi khuyn mi'
  }),
  value: z.number().min(0, 'Gi tr phi ln hn 0'),
  minimumOrderAmount: z.number().min(0, 'Gi tr n hng ti thiu khng hp l').optional(),
  maxDiscount: z.number().min(0, 'Gim gi ti a khng hp l').optional(),
  usageLimit: z.number().min(1, 'Gii hn s dng phi ln hn 0').optional(),
  startDate: z.string().min(1, 'Ngy bt u l bt buc'),
  endDate: z.string().min(1, 'Ngy kt thc l bt buc'),
  isActive: z.boolean()
}).refine((data) => {
  // Validate value based on type
  if (data.type === 'percentage' && data.value > 100) {
    return false;
  }
  if (data.type === 'fixed_amount' && data.value < 1000) {
    return false;
  }
  return true;
}, {
  message: 'Gi tr khuyn mi khng hp l',
  path: ['value']
}).refine((data) => {
  // Validate dates
  const startDate = new Date(data.startDate);
  const endDate = new Date(data.endDate);
  return startDate < endDate;
}, {
  message: 'Ngy kt thc phi sau ngy bt u',
  path: ['endDate']
});

type PromotionFormData = z.infer<typeof promotionSchema>;

// Mock API call - Replace with real API
const useCreatePromotionMutation = () => {
  const queryClient = useQueryClient();
  const router = useRouter();

  return useMutation({
    mutationFn: async (data: PromotionFormData) => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      console.log('Creating promotion:', data);
      return { success: true, id: Date.now() };
    },
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'promotions'] });
      router.push('/promotions');
    }
  });
};

// Vietnamese formatting utilities - moved inside component

const formatDate = (dateString: string) => {
  return new Date(dateString).toISOString().split('T')[0];
};

export default function AddPromotionPage() {
  const { selectedCurrency } = useCurrencyContext();
  const router = useRouter();
  const { user } = useAdminAuth();

  const [previewData, setPreviewData] = useState<Partial<PromotionFormData>>({});

  // Helper functions
  const formatCurrency = (amount: number) => {
    return formatCurrencyPrice(amount, selectedCurrency);
  };

  const form = useForm<PromotionFormData>({
    resolver: zodResolver(promotionSchema),
    defaultValues: {
      name: '',
      code: '',
      description: '',
      type: 'percentage',
      value: 0,
      minimumOrderAmount: 0,
      maxDiscount: 0,
      usageLimit: undefined,
      startDate: new Date().toISOString().split('T')[0],
      endDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      isActive: true
    }
  });

  const createPromotionMutation = useCreatePromotionMutation();

  const watchedValues = form.watch();
  const promotionType = form.watch('type');

  const onSubmit = async (data: PromotionFormData) => {
    try {
      await createPromotionMutation.mutateAsync(data);
    } catch (error) {
      console.error('Error creating promotion:', error);
    }
  };

  const generatePromotionCode = () => {
    const name = form.getValues('name');
    if (!name) return;

    // Generate code from name
    const code = name
      .toUpperCase()
      .replace(/[^A-Z0-9]/g, '')
      .substring(0, 10);

    form.setValue('code', code + Math.random().toString().substring(2, 5));
  };

  const getPromotionPreview = () => {
    const values = form.getValues();

    if (!values.name || !values.type || values.value === 0) {
      return null;
    }

    let valueText = '';
    switch (values.type) {
      case 'percentage':
        valueText = `Gim ${values.value}%`;
        if (values.maxDiscount && values.maxDiscount > 0) {
          valueText += ` (ti a ${formatCurrency(values.maxDiscount)})`;
        }
        break;
      case 'fixed_amount':
        valueText = `Gim ${formatCurrency(values.value)}`;
        break;
      case 'free_shipping':
        valueText = 'Min ph vn chuyn';
        break;
    }

    return {
      name: values.name,
      code: values.code,
      valueText,
      minimumOrder: values.minimumOrderAmount || 0,
      startDate: values.startDate,
      endDate: values.endDate
    };
  };

  const preview = getPromotionPreview();

  return (
    <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/promotions">
              <Button variant="outline" size="sm">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Quay li
              </Button>
            </Link>
            <div>
              <h1 className="text-3xl font-bold">Thm khuyn mi mi</h1>
              <p className="text-muted-foreground">
                To chng trnh khuyn mi hoc m gim gi mi
              </p>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Form */}
          <div className="lg:col-span-2">
            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                {/* Basic Information */}
                <Card>
                  <CardHeader>
                    <CardTitle>Thng tin c bn</CardTitle>
                    <CardDescription>
                      Nhp thng tin chung v chng trnh khuyn mi
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <FormField
                      control={form.control}
                      name="name"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Tn khuyn mi *</FormLabel>
                          <FormControl>
                            <Input
                              placeholder="VD: Khuyn mi Tt 2025"
                              {...field}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="code"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>M khuyn mi *</FormLabel>
                          <div className="flex gap-2">
                            <FormControl>
                              <Input
                                placeholder="VD: TET2025"
                                className="uppercase"
                                {...field}
                                onChange={(e) => field.onChange(e.target.value.toUpperCase())}
                              />
                            </FormControl>
                            <Button
                              type="button"
                              variant="outline"
                              onClick={generatePromotionCode}
                            >
                              T ng to
                            </Button>
                          </div>
                          <FormDescription>
                            M ch c cha ch hoa v s, khng c du cch
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="description"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>M t</FormLabel>
                          <FormControl>
                            <Textarea
                              placeholder="M t chi tit v chng trnh khuyn mi..."
                              rows={3}
                              {...field}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </CardContent>
                </Card>

                {/* Promotion Details */}
                <Card>
                  <CardHeader>
                    <CardTitle>Chi tit khuyn mi</CardTitle>
                    <CardDescription>
                      Cu hnh loi v gi tr khuyn mi
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <FormField
                      control={form.control}
                      name="type"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Loi khuyn mi *</FormLabel>
                          <Select onValueChange={field.onChange} defaultValue={field.value}>
                            <FormControl>
                              <SelectTrigger>
                                <SelectValue placeholder="Chn loi khuyn mi" />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent>
                              <SelectItem value="percentage">
                                <div className="flex items-center gap-2">
                                  <Percent className="h-4 w-4" />
                                  Gim theo phn trm
                                </div>
                              </SelectItem>
                              <SelectItem value="fixed_amount">
                                <div className="flex items-center gap-2">
                                  <DollarSign className="h-4 w-4" />
                                  Gim c nh
                                </div>
                              </SelectItem>
                              <SelectItem value="free_shipping">
                                <div className="flex items-center gap-2">
                                  <Truck className="h-4 w-4" />
                                  Min ph vn chuyn
                                </div>
                              </SelectItem>
                            </SelectContent>
                          </Select>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    {promotionType !== 'free_shipping' && (
                      <FormField
                        control={form.control}
                        name="value"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>
                              Gi tr {promotionType === 'percentage' ? '(%)' : '(VND)'} *
                            </FormLabel>
                            <FormControl>
                              <Input
                                type="number"
                                placeholder={promotionType === 'percentage' ? '10' : '100000'}
                                {...field}
                                onChange={(e) => field.onChange(Number(e.target.value))}
                              />
                            </FormControl>
                            <FormDescription>
                              {promotionType === 'percentage'
                                ? 'Nhp phn trm gim gi (1-100)'
                                : 'Nhp s tin gim gi tnh bng VND'
                              }
                            </FormDescription>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    )}

                    <FormField
                      control={form.control}
                      name="minimumOrderAmount"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Gi tr n hng ti thiu (VND)</FormLabel>
                          <FormControl>
                            <Input
                              type="number"
                              placeholder="0"
                              {...field}
                              onChange={(e) => field.onChange(Number(e.target.value) || 0)}
                            />
                          </FormControl>
                          <FormDescription>
                             trng hoc nhp 0 nu khng c yu cu ti thiu
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    {promotionType === 'percentage' && (
                      <FormField
                        control={form.control}
                        name="maxDiscount"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Gim gi ti a (VND)</FormLabel>
                            <FormControl>
                              <Input
                                type="number"
                                placeholder="0"
                                {...field}
                                onChange={(e) => field.onChange(Number(e.target.value) || 0)}
                              />
                            </FormControl>
                            <FormDescription>
                              Gii hn s tin gim ti a cho khuyn mi phn trm
                            </FormDescription>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    )}

                    <FormField
                      control={form.control}
                      name="usageLimit"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Gii hn s lng s dng</FormLabel>
                          <FormControl>
                            <Input
                              type="number"
                              placeholder="Khng gii hn"
                              {...field}
                              onChange={(e) => field.onChange(Number(e.target.value) || undefined)}
                            />
                          </FormControl>
                          <FormDescription>
                             trng nu khng gii hn s ln s dng
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </CardContent>
                </Card>

                {/* Time Settings */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Calendar className="h-5 w-5" />
                      Thi gian p dng
                    </CardTitle>
                    <CardDescription>
                      Thit lp thi gian hiu lc ca khuyn mi
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <FormField
                        control={form.control}
                        name="startDate"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Ngy bt u *</FormLabel>
                            <FormControl>
                              <Input
                                type="date"
                                {...field}
                              />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />

                      <FormField
                        control={form.control}
                        name="endDate"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Ngy kt thc *</FormLabel>
                            <FormControl>
                              <Input
                                type="date"
                                {...field}
                              />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    </div>
                  </CardContent>
                </Card>

                {/* Status */}
                <Card>
                  <CardHeader>
                    <CardTitle>Trng thi</CardTitle>
                    <CardDescription>
                      Thit lp trng thi ban u ca khuyn mi
                    </CardDescription>
                  </CardHeader>
                  <CardContent>
                    <FormField
                      control={form.control}
                      name="isActive"
                      render={({ field }) => (
                        <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                          <div className="space-y-0.5">
                            <FormLabel className="text-base">
                              Kch hot ngay
                            </FormLabel>
                            <FormDescription>
                              Khuyn mi s c kch hot ngay sau khi to
                            </FormDescription>
                          </div>
                          <FormControl>
                            <Switch
                              checked={field.value}
                              onCheckedChange={field.onChange}
                            />
                          </FormControl>
                        </FormItem>
                      )}
                    />
                  </CardContent>
                </Card>

                {/* Submit Buttons */}
                <div className="flex justify-end gap-4">
                  <Link href="/promotions">
                    <Button variant="outline">Hy b</Button>
                  </Link>
                  <Button
                    type="submit"
                    disabled={createPromotionMutation.isPending}
                  >
                    {createPromotionMutation.isPending ? (
                      'ang to...'
                    ) : (
                      <>
                        <Save className="h-4 w-4 mr-2" />
                        To khuyn mi
                      </>
                    )}
                  </Button>
                </div>
              </form>
            </Form>
          </div>

          {/* Preview Sidebar */}
          <div className="space-y-6">
            {/* Preview Card */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Tag className="h-5 w-5" />
                  Xem trc
                </CardTitle>
                <CardDescription>
                  Khuyn mi s hin th nh th ny
                </CardDescription>
              </CardHeader>
              <CardContent>
                {preview ? (
                  <div className="space-y-4">
                    <div className="p-4 border rounded-lg bg-gradient-to-r from-blue-50 to-purple-50">
                      <div className="text-lg font-bold text-blue-900">
                        {preview.name}
                      </div>
                      <div className="text-sm text-blue-700 mb-2">
                        M: <code className="bg-white px-1 rounded">{preview.code}</code>
                      </div>
                      <div className="text-2xl font-bold text-purple-600 mb-2">
                        {preview.valueText}
                      </div>
                      {preview.minimumOrder > 0 && (
                        <div className="text-sm text-gray-600">
                          Cho n hng t {formatCurrency(preview.minimumOrder)}
                        </div>
                      )}
                      <div className="text-xs text-gray-500 mt-2">
                        C hiu lc t {new Date(preview.startDate).toLocaleDateString('vi-VN')}
                        {' '} n {new Date(preview.endDate).toLocaleDateString('vi-VN')}
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Info className="h-8 w-8 mx-auto mb-2" />
                    <p>Nhp thng tin  xem trc khuyn mi</p>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Tips Card */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <AlertCircle className="h-5 w-5" />
                  Gi 
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm">
                <div>
                  <strong>M khuyn mi:</strong> Nn ngn gn, d nh v lin quan n chng trnh
                </div>
                <div>
                  <strong>Gi tr:</strong> Cn nhc k  m bo li nhun v sc hp dn
                </div>
                <div>
                  <strong>Thi gian:</strong> Khng nn qu di  to cm gic cp bch
                </div>
                <div>
                  <strong>Gii hn:</strong> t gii hn s dng  kim sot ngn sch
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </PermissionGuard>
  );
}