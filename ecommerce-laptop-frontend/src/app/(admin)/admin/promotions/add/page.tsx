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
  name: z.string().min(1, 'Tên khuyến mãi là bắt buộc').max(100, 'Tên không được vượt quá 100 ký tự'),
  code: z.string()
    .min(3, 'Mã khuyến mãi phải có ít nhất 3 ký tự')
    .max(20, 'Mã không được vượt quá 20 ký tự')
    .regex(/^[A-Z0-9]+$/, 'Mã chỉ được chứa chữ hoa và số'),
  description: z.string().max(500, 'Mô tả không được vượt quá 500 ký tự').optional(),
  type: z.enum(['percentage', 'fixed_amount', 'free_shipping'], {
    required_error: 'Vui lòng chọn loại khuyến mãi'
  }),
  value: z.number().min(0, 'Giá trị phải lớn hơn 0'),
  minimumOrderAmount: z.number().min(0, 'Giá trị đơn hàng tối thiểu không hợp lệ').optional(),
  maxDiscount: z.number().min(0, 'Giảm giá tối đa không hợp lệ').optional(),
  usageLimit: z.number().min(1, 'Giới hạn sử dụng phải lớn hơn 0').optional(),
  startDate: z.string().min(1, 'Ngày bắt đầu là bắt buộc'),
  endDate: z.string().min(1, 'Ngày kết thúc là bắt buộc'),
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
  message: 'Giá trị khuyến mãi không hợp lệ',
  path: ['value']
}).refine((data) => {
  // Validate dates
  const startDate = new Date(data.startDate);
  const endDate = new Date(data.endDate);
  return startDate < endDate;
}, {
  message: 'Ngày kết thúc phải sau ngày bắt đầu',
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
        valueText = `Giảm ${values.value}%`;
        if (values.maxDiscount && values.maxDiscount > 0) {
          valueText += ` (tối đa ${formatCurrency(values.maxDiscount)})`;
        }
        break;
      case 'fixed_amount':
        valueText = `Giảm ${formatCurrency(values.value)}`;
        break;
      case 'free_shipping':
        valueText = 'Miễn phí vận chuyển';
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
                Quay lại
              </Button>
            </Link>
            <div>
              <h1 className="text-3xl font-bold">Thêm khuyến mãi mới</h1>
              <p className="text-muted-foreground">
                Tạo chương trình khuyến mãi hoặc mã giảm giá mới
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
                    <CardTitle>Thông tin cơ bản</CardTitle>
                    <CardDescription>
                      Nhập thông tin chung về chương trình khuyến mãi
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <FormField
                      control={form.control}
                      name="name"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Tên khuyến mãi *</FormLabel>
                          <FormControl>
                            <Input
                              placeholder="VD: Khuyến mãi Tết 2025"
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
                          <FormLabel>Mã khuyến mãi *</FormLabel>
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
                              Tự động tạo
                            </Button>
                          </div>
                          <FormDescription>
                            Mã chỉ được chứa chữ hoa và số, không có dấu cách
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
                          <FormLabel>Mô tả</FormLabel>
                          <FormControl>
                            <Textarea
                              placeholder="Mô tả chi tiết về chương trình khuyến mãi..."
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
                    <CardTitle>Chi tiết khuyến mãi</CardTitle>
                    <CardDescription>
                      Cấu hình loại và giá trị khuyến mãi
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <FormField
                      control={form.control}
                      name="type"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Loại khuyến mãi *</FormLabel>
                          <Select onValueChange={field.onChange} defaultValue={field.value}>
                            <FormControl>
                              <SelectTrigger>
                                <SelectValue placeholder="Chọn loại khuyến mãi" />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent>
                              <SelectItem value="percentage">
                                <div className="flex items-center gap-2">
                                  <Percent className="h-4 w-4" />
                                  Giảm theo phần trăm
                                </div>
                              </SelectItem>
                              <SelectItem value="fixed_amount">
                                <div className="flex items-center gap-2">
                                  <DollarSign className="h-4 w-4" />
                                  Giảm cố định
                                </div>
                              </SelectItem>
                              <SelectItem value="free_shipping">
                                <div className="flex items-center gap-2">
                                  <Truck className="h-4 w-4" />
                                  Miễn phí vận chuyển
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
                              Giá trị {promotionType === 'percentage' ? '(%)' : '(VND)'} *
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
                                ? 'Nhập phần trăm giảm giá (1-100)'
                                : 'Nhập số tiền giảm giá tính bằng VND'
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
                          <FormLabel>Giá trị đơn hàng tối thiểu (VND)</FormLabel>
                          <FormControl>
                            <Input
                              type="number"
                              placeholder="0"
                              {...field}
                              onChange={(e) => field.onChange(Number(e.target.value) || 0)}
                            />
                          </FormControl>
                          <FormDescription>
                            Để trống hoặc nhập 0 nếu không có yêu cầu tối thiểu
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
                            <FormLabel>Giảm giá tối đa (VND)</FormLabel>
                            <FormControl>
                              <Input
                                type="number"
                                placeholder="0"
                                {...field}
                                onChange={(e) => field.onChange(Number(e.target.value) || 0)}
                              />
                            </FormControl>
                            <FormDescription>
                              Giới hạn số tiền giảm tối đa cho khuyến mãi phần trăm
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
                          <FormLabel>Giới hạn số lượng sử dụng</FormLabel>
                          <FormControl>
                            <Input
                              type="number"
                              placeholder="Không giới hạn"
                              {...field}
                              onChange={(e) => field.onChange(Number(e.target.value) || undefined)}
                            />
                          </FormControl>
                          <FormDescription>
                            Để trống nếu không giới hạn số lần sử dụng
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
                      Thời gian áp dụng
                    </CardTitle>
                    <CardDescription>
                      Thiết lập thời gian hiệu lực của khuyến mãi
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <FormField
                        control={form.control}
                        name="startDate"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Ngày bắt đầu *</FormLabel>
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
                            <FormLabel>Ngày kết thúc *</FormLabel>
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
                    <CardTitle>Trạng thái</CardTitle>
                    <CardDescription>
                      Thiết lập trạng thái ban đầu của khuyến mãi
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
                              Kích hoạt ngay
                            </FormLabel>
                            <FormDescription>
                              Khuyến mãi sẽ được kích hoạt ngay sau khi tạo
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
                    <Button variant="outline">Hủy bỏ</Button>
                  </Link>
                  <Button
                    type="submit"
                    disabled={createPromotionMutation.isPending}
                  >
                    {createPromotionMutation.isPending ? (
                      'Đang tạo...'
                    ) : (
                      <>
                        <Save className="h-4 w-4 mr-2" />
                        Tạo khuyến mãi
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
                  Xem trước
                </CardTitle>
                <CardDescription>
                  Khuyến mãi sẽ hiển thị như thế này
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
                        Mã: <code className="bg-white px-1 rounded">{preview.code}</code>
                      </div>
                      <div className="text-2xl font-bold text-purple-600 mb-2">
                        {preview.valueText}
                      </div>
                      {preview.minimumOrder > 0 && (
                        <div className="text-sm text-gray-600">
                          Cho đơn hàng từ {formatCurrency(preview.minimumOrder)}
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
                    <p>Nhập thông tin để xem trước khuyến mãi</p>
                  </div>
                )}
              </CardContent>
            </Card>

            {/* Tips Card */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <AlertCircle className="h-5 w-5" />
                  Gợi ý
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3 text-sm">
                <div>
                  <strong>Mã khuyến mãi:</strong> Nên ngắn gọn, dễ nhớ và liên quan đến chương trình
                </div>
                <div>
                  <strong>Giá trị:</strong> Cân nhắc kỹ để đảm bảo lợi nhuận và sức hấp dẫn
                </div>
                <div>
                  <strong>Thời gian:</strong> Không nên quá dài để tạo cảm giác cấp bách
                </div>
                <div>
                  <strong>Giới hạn:</strong> Đặt giới hạn sử dụng để kiểm soát ngân sách
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </PermissionGuard>
  );
}