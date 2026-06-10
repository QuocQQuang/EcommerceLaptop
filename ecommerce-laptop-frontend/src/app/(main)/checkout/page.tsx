'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { DistrictSelect, ProvinceSelect, WardSelect } from '@/components/organisms/AddressSelect';
import { Button, Button as UIButton } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from '@/components/ui/form';
import { Input, Input as UIInput } from '@/components/ui/input';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { useAuth } from '@/hooks/useAuth';
import { useLogger } from '@/hooks/useLogger';
import api from '@/lib/api';
import { formatCurrencyPrice } from '@/lib/currency';
import { cartService } from '@/services/cartService';
import { orderService } from '@/services/orderService';
import { userService } from '@/services/userService';
import { useCartStore } from '@/store/cartStore';
import { PaymentGateway, PaymentMethod } from '@/types/api';
import { vietnamAddressService } from '@/utils/vietnam-address';
import { zodResolver } from '@hookform/resolvers/zod';
import Image from 'next/image';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';

const shippingSchema = z.object({
    recipientName: z.string().min(1, 'Tên người nhận là bắt buộc'),
    shippingAddress: z.string().min(1, 'Địa chỉ giao hàng là bắt buộc'),
    provinceCode: z.string().min(1, 'Tỉnh/Thành phố là bắt buộc'),
    districtCode: z.string().min(1, 'Quận/Huyện là bắt buộc'),
    wardCode: z.string().min(1, 'Phường/Xã là bắt buộc'),
    shippingPostalCode: z.string().optional(),
    phoneNumber: z
        .string()
        .min(10, 'Số điện thoại phải có ít nhất 10 chữ số')
        .max(15, 'Số điện thoại không được quá 15 chữ số')
        .regex(/^\d{10,15}$/, 'Số điện thoại chỉ chứa số'),
    paymentMethod: z.enum(['COD', 'SEPAY', 'PayPal', 'Stripe'], {
        required_error: 'Vui lòng chọn phương thức thanh toán.',
    }),
});

type ShippingFormValues = z.infer<typeof shippingSchema>;

function CheckoutContent() {
    const { selectedCurrency } = useCurrencyContext();
    const router = useRouter();
    const searchParams = useSearchParams();
    const env = searchParams?.get('env') as 'sandbox' | 'production' | null || 'sandbox';
    const [environment, setEnvironment] = useState<'sandbox' | 'production'>(env);
    const { user, isAuthenticated } = useAuth();
    const logger = useLogger('Checkout');
    const { items, total, itemCount, clearCart } = useCartStore();
    const [isPlacingOrder, setIsPlacingOrder] = useState(false);
    const [isProcessingPayment, setIsProcessingPayment] = useState(false);
    const [savedAddresses, setSavedAddresses] = useState<any[]>([]);
    const [selectedAddressId, setSelectedAddressId] = useState<string | null>(null);
    const [isAddAddressOpen, setIsAddAddressOpen] = useState(false);
    const [newAddressForm, setNewAddressForm] = useState({
        fullName: '',
        phoneNumber: '',
        address: '',
        wardCode: '',
        districtCode: '',
        provinceCode: '',
        postalCode: '',
        type: 'home',
        isDefault: false,
    });
    const [isSavingAddress, setIsSavingAddress] = useState(false);

    // Sync local cart to server
    const syncCartToServer = async () => {
        logger.info(' Syncing local cart to server', {
            localItemCount: items.length,
            itemsStructure: items.map(item => ({
                productId: item.productId,
                id: item.id,
                quantity: item.quantity,
                hasProduct: !!item.product,
                allKeys: Object.keys(item)
            }))
        });

        try {
            // Clear server cart first
            await cartService.clearCart();

            // Add each item from local cart to server cart
            for (const item of items) {
                // Handle nested product structure and extract correct productId
                let productId = item.productId;
                if (!productId || productId > 2147483647) { // Check if timestamp ID (too large for Int32)
                    productId = (item.product as any)?.data?.id || item.product?.id;
                }

                logger.info(' Processing cart item', {
                    originalItem: item,
                    extractedProductId: productId,
                    quantity: item.quantity,
                    productIdType: typeof productId,
                    isValidInt32: productId <= 2147483647
                });

                if (!productId || productId <= 0 || productId > 2147483647) {
                    logger.error(' Invalid productId for cart item', {
                        item,
                        productId,
                        itemKeys: Object.keys(item),
                        maxInt32: 2147483647
                    });
                    throw new Error(`Invalid product ID: ${productId}. Must be a positive Int32.`);
                }

                await cartService.addToCart({
                    productId: productId,
                    quantity: item.quantity
                });
                logger.debug(' Added item to server cart', {
                    productId: productId,
                    quantity: item.quantity,
                    productName: item.product?.name || (item.product as any)?.data?.name
                });
            }

            logger.info(' Cart sync completed', { syncedItems: items.length });
        } catch (error: any) {
            logger.error(' Cart sync failed', { error });

            // Handle specific error types
            if (error.message?.includes('409:')) {
                const errorMessage = error.message.replace('409: ', '');
                // Parse detailed inventory error
                if (errorMessage.includes('Insufficient stock')) {
                    const match = errorMessage.match(/Available: (\d+), Requested: (\d+)/);
                    if (match) {
                        const [, available, requested] = match;
                        throw new Error(`Sản phẩm không đủ hàng! Còn lại: ${available} sản phẩm, bạn yêu cầu: ${requested} sản phẩm.`);
                    }
                }
                throw new Error(`Lỗi tồn kho: ${errorMessage}`);
            }

            throw new Error('Failed to sync cart to server');
        }
    };

    const form = useForm<ShippingFormValues>({
        resolver: zodResolver(shippingSchema),
        defaultValues: {
            recipientName: '',
            shippingAddress: '',
            phoneNumber: '',
            paymentMethod: 'COD',
        },
    });

    useEffect(() => {
        logger.info(' Checkout: Auth/Cart state changed', {
            isAuthenticated,
            itemCount,
            isPlacingOrder
        });

        if (!isAuthenticated) {
            logger.warn(' Redirecting to login - not authenticated');
            toast.info('Vui lòng đăng nhập để tiếp tục thanh toán.');
            router.push('/auth/login?callbackUrl=/checkout');
        } else if (itemCount === 0 && isPlacingOrder === false && !isProcessingPayment) {
            logger.warn(' Redirecting to home - empty cart and not placing order', {
                itemCount,
                isPlacingOrder,
                isProcessingPayment
            });
            toast.info('Giỏ hàng của bạn đang trống.');
            router.push('/');
        }
    }, [isAuthenticated, itemCount, router, isPlacingOrder, isProcessingPayment]);

    useEffect(() => {
        if (user) {
            form.reset({
                recipientName: `${user.firstName || ''} ${user.lastName || ''}`.trim(),
                shippingAddress: '',
                provinceCode: '',
                districtCode: '',
                wardCode: '',
                phoneNumber: user.phoneNumber || '',
                paymentMethod: 'COD',
            });

            // load saved addresses for user
            (async () => {
                try {
                    const res = await userService.getAddresses();
                    const list = (res?.data || []).map((a: any) => {
                        // Convert province/district/ward name to code if code is missing
                        let provinceCode = a.provinceCode || '';
                        let districtCode = a.districtCode || '';
                        let wardCode = a.wardCode || '';
                        // Tm m tnh t tn
                        if (!provinceCode && a.province) {
                            const found = vietnamAddressService.getProvinces().find(p => p.name_with_type === a.province || p.name === a.province);
                            provinceCode = found?.code || '';
                        }
                        // Tm m qun/huyn t tn city (ch khi  c m tnh)
                        if (!districtCode && a.city && provinceCode) {
                            const districts = vietnamAddressService.getDistrictsByProvince(provinceCode);
                            const found = districts.find(d => d.name_with_type === a.city || d.name === a.city);
                            districtCode = found?.code || '';
                        }
                        // Tm m phng/x t tn district (ch khi  c m qun/huyn)
                        if (!wardCode && a.district && districtCode) {
                            const wards = vietnamAddressService.getWardsByDistrict(districtCode);
                            const found = wards.find(w => w.name_with_type === a.district || w.name === a.district);
                            wardCode = found?.code || '';
                        }
                        return {
                            id: String(a.id || a._id || Date.now()),
                            fullName: a.fullName || a.recipientName || '',
                            phoneNumber: a.phoneNumber || a.phone || '',
                            address: a.street || a.address || '',
                            wardCode,
                            districtCode,
                            provinceCode,
                            postalCode: a.postalCode || a.zipCode || '',
                            type: a.type || 'home',
                            isDefault: !!a.isDefault
                        };
                    });
                    setSavedAddresses(list);
                    const def = list.find(a => a.isDefault);
                    if (def) setSelectedAddressId(def.id);
                } catch (err) {
                    // ignore
                }
            })();
        }
    }, [user, form]);

    // When selectedAddressId changes, prefill the shipping form
    useEffect(() => {
        if (!selectedAddressId) return;
        const addr = savedAddresses.find(a => a.id === selectedAddressId);
        if (!addr) return;

        // Prefill in correct order to trigger dependent selects
        form.setValue('recipientName', addr.fullName || form.getValues().recipientName);
        form.setValue('phoneNumber', addr.phoneNumber || form.getValues().phoneNumber);
        form.setValue('shippingAddress', addr.address || '');
        form.setValue('provinceCode', addr.provinceCode || '');
        setTimeout(() => {
            form.setValue('districtCode', addr.districtCode || '');
            setTimeout(() => {
                form.setValue('wardCode', addr.wardCode || '');
            }, 100);
        }, 100);
        form.setValue('shippingPostalCode', addr.postalCode || '');
    }, [selectedAddressId, savedAddresses, form]);

    // Handler for adding new address
    const handleAddAddress = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsSavingAddress(true);
        try {
            // Map codes to names for backend
            const provinceObj = vietnamAddressService.getProvinceByCode(newAddressForm.provinceCode);
            const districtObj = vietnamAddressService.getDistrictByCode(newAddressForm.districtCode);
            const wardObj = vietnamAddressService.getWardByCode(newAddressForm.wardCode);

            const payload = {
                fullName: newAddressForm.fullName,
                phoneNumber: newAddressForm.phoneNumber,
                street: newAddressForm.address,
                city: districtObj?.name || '', // Huyn/Qun  City
                province: provinceObj?.name || '', // Tnh  Province
                district: wardObj?.name || '', // X/Phng  District
                postalCode: newAddressForm.postalCode,
                country: 'Việt Nam',
                isDefault: newAddressForm.isDefault,
                // Keep original codes for frontend state management
                wardCode: newAddressForm.wardCode,
                districtCode: newAddressForm.districtCode,
                provinceCode: newAddressForm.provinceCode,
                type: newAddressForm.type,
            };

            const res = await userService.addAddress(payload as any);
            const created = res?.data as any;
            const newAddr = {
                id: String(created?.id || created?._id || Date.now()),
                ...newAddressForm,
            };
            setSavedAddresses(prev => [newAddr, ...prev]);
            setSelectedAddressId(newAddr.id);
            setIsAddAddressOpen(false);
            setNewAddressForm({
                fullName: '',
                phoneNumber: '',
                address: '',
                wardCode: '',
                districtCode: '',
                provinceCode: '',
                postalCode: '',
                type: 'home',
                isDefault: false,
            });
            toast.success('Thêm địa chỉ mới thành công');
        } catch (err) {
            toast.error('Thêm địa chỉ thất bại');
        } finally {
            setIsSavingAddress(false);
        }
    };

    // Helper function to format address for shipping
    const formatShippingAddress = (data: ShippingFormValues) => {
        const province = vietnamAddressService.getProvinceByCode(data.provinceCode);
        const district = vietnamAddressService.getDistrictByCode(data.districtCode);
        const ward = vietnamAddressService.getWardByCode(data.wardCode);

        return {
            shippingCity: province?.name_with_type || '',
            shippingProvince: province?.name_with_type || '',
            fullAddress: [
                data.shippingAddress,
                ward?.name_with_type,
                district?.name_with_type,
                province?.name_with_type
            ].filter(Boolean).join(', ')
        };
    };

    const onSubmit = async (data: ShippingFormValues) => {
        setIsPlacingOrder(true);
        const toastId = toast.loading('Đang tạo đơn hàng...');

        const addressInfo = formatShippingAddress(data);

        // Backend expects shippingAddress as a single string, not structured object
        const fullShippingAddress = [
            data.recipientName,
            data.phoneNumber,
            data.shippingAddress,
            form.watch('wardCode'),
            form.watch('districtCode'),
            addressInfo.shippingCity
        ].filter(Boolean).join(', ');

        try {
            // Check if local cart has items
            if (items.length === 0) {
                throw new Error('Cart is empty. Please add items to your cart before checkout.');
            }

            // First, sync local cart to server
            logger.info(' Checkout: Syncing local cart to server before order creation');
            await syncCartToServer();

            // Then get the server cart to get the cart ID
            logger.info(' Checkout: Getting cart for order creation');
            const cartResponse = await api.get('/cart');
            const cartData = cartResponse.data;

            logger.debug(' Checkout: Cart response', {
                keys: Object.keys(cartData),
                cartId: cartData.cartId,
                CartId: cartData.CartId,
                Id: cartData.Id,
                itemCount: cartData.summary?.itemCount,
                hasItems: cartData.items?.length > 0
            });

            const cartId = cartData.cartId || cartData.CartId || cartData.Id; // Try lowercase first, then uppercase

            if (!cartId) {
                logger.error(' Checkout: No cart ID found', {
                    cartData,
                    availableKeys: Object.keys(cartData)
                });
                throw new Error('Không thể lấy ID giỏ hàng. Vui lòng thử lại.');
            }

            logger.info(' Checkout: Using cart ID', { cartId });

            const customerId = user?.id || 0;

            logger.info(' Checkout: Creating order', {
                cartId: cartId.toString(),
                shippingAddress: fullShippingAddress,
                paymentMethod: data.paymentMethod,
                orderStatus: 'Pending'
            });

            const orderResult = await orderService.createOrderFromCart({
                cartId: cartId.toString(),
                shippingAddress: fullShippingAddress,
                paymentMethod: data.paymentMethod, // Pass payment method
                orderStatus: 'Pending' // Set initial status for online payments
            });

            logger.info(' Checkout: Order created successfully', {
                orderId: orderResult.id,
                orderNumber: orderResult.orderNumber,
                paymentMethod: data.paymentMethod
            });

            toast.success(`Đơn hàng #${orderResult.orderNumber} đã được tạo!`, {
                id: toastId,
            });

            // DON'T clear cart here yet - wait until after redirect

            logger.info(' Checkout: Processing payment redirect', {
                paymentMethod: data.paymentMethod,
                orderId: orderResult.id,
                environment
            });

            // Set payment processing flag to prevent redirect to home
            setIsProcessingPayment(true);

            // Redirect to the specific payment page
            if (data.paymentMethod === 'Stripe') {
                logger.info(' Redirecting to Stripe payment');
                clearCart(); // Clear cart before redirect to external payment
                router.push(`/payment/stripe?orderId=${orderResult.id}&env=${environment}`);
            } else if (data.paymentMethod === 'PayPal') {
                logger.info(' Redirecting to PayPal payment');
                clearCart(); // Clear cart before redirect to external payment
                router.push(`/payment/paypal?orderId=${orderResult.id}&env=${environment}`);
            } else if (data.paymentMethod === 'COD') {
                // For COD, redirect to confirmation page
                logger.info(' Processing COD order - redirecting to confirmation');
                clearCart(); // Clear cart for COD orders
                const confirmationUrl = `/checkout/confirmation?orderId=${orderResult.id}`;
                logger.info(' COD redirect URL:', { confirmationUrl });
                router.push(confirmationUrl);
            } else {
                // Handle other gateways like SEPAY, Stripe, PayPal if they need similar flow
                logger.info(' Processing other payment gateway', {
                    paymentMethod: data.paymentMethod,
                    orderId: orderResult.id
                });

                // Map payment method string to payment gateway and method enums
                const getPaymentConfig = (paymentMethod: string): { gateway: PaymentGateway; method: PaymentMethod } => {
                    switch (paymentMethod) {
                        case 'PayPal':
                            return { gateway: PaymentGateway.PayPal, method: PaymentMethod.CreditCard };
                        case 'SEPAY':
                            return { gateway: PaymentGateway.SePay, method: PaymentMethod.BankTransfer };
                        case 'Stripe':
                            return { gateway: PaymentGateway.Stripe, method: PaymentMethod.CreditCard };
                        case 'VNPAY':
                            return { gateway: PaymentGateway.VnPay, method: PaymentMethod.CreditCard };
                        case 'COD':
                            throw new Error('COD không sử dụng payment gateway');
                        default:
                            throw new Error('Phương thức thanh toán không hợp lệ');
                    }
                };

                const paymentConfig = getPaymentConfig(data.paymentMethod);

                const atomicRequest = {
                    orderId: orderResult.id,
                    gateway: paymentConfig.gateway,
                    method: paymentConfig.method,
                    description: `Payment for order ${orderResult.orderNumber}`,
                    returnUrl: `${window.location.origin}/checkout/confirmation?orderId=${orderResult.id}`,
                    cancelUrl: `${window.location.origin}/checkout`
                };

                logger.info(' Sending payment initialization request', {
                    atomicRequest,
                    originalPaymentMethod: data.paymentMethod,
                    mappedGateway: paymentConfig.gateway,
                    mappedMethod: paymentConfig.method
                });

                const atomicResponse = await api.post('/payment/initialize', atomicRequest);
                const result = atomicResponse.data;

                logger.info(' Payment initialization response', {
                    hasRedirectUrl: !!result.redirectUrl,
                    hasPaymentUrl: !!result.paymentUrl,
                    transactionId: result.transactionId,
                    paymentType: result.additionalData?.paymentType,
                    result
                });

                // Check if this is SEPAY inline QR payment
                if (data.paymentMethod === 'SEPAY' && result.additionalData?.paymentType === 'qr_inline') {
                    logger.info(' SEPAY inline QR payment detected', {
                        qrCodeData: !!result.qrCodeData,
                        bankAccount: result.additionalData?.bankAccount
                    });

                    // For SEPAY, show QR code inline instead of redirect
                    toast.success('Vui lòng quét mã QR để thanh toán!', { id: toastId });

                    // Store payment data for inline display
                    localStorage.setItem('sepayPaymentData', JSON.stringify({
                        orderId: orderResult.id,
                        transactionId: result.transactionId,
                        qrCodeData: result.qrCodeData,
                        qrCodeUrl: result.additionalData?.qrCodeUrl,
                        bankAccount: result.additionalData?.bankAccount,
                        bankName: result.additionalData?.bankName,
                        instructions: result.additionalData?.instructions,
                        amount: result.additionalData?.amount,
                        currency: result.additionalData?.currency
                    }));

                    clearCart(); // Clear cart for SEPAY payment
                    router.push(`/payment/sepay?orderId=${orderResult.id}`);
                    return; // Exit early for SEPAY
                }

                const paymentUrl = result.redirectUrl || result.paymentUrl;
                if (paymentUrl) {
                    logger.info(' Redirecting to payment gateway', {
                        paymentUrl,
                        paymentMethod: data.paymentMethod
                    });
                    clearCart(); // Clear cart before redirect to payment gateway

                    // Redirect to payment gateways
                    window.location.href = paymentUrl;
                } else {
                    logger.error(' Payment initialization failed', {
                        errorMessage: result.errorMessage,
                        result
                    });
                    throw new Error(result.errorMessage || 'Tạo phiên thanh toán thất bại');
                }
            }
        } catch (error: any) {
            logger.error(' Checkout error occurred', {
                error: error.message,
                response: error.response?.data,
                stack: error.stack
            });

            const errorMessage =
                error.response?.data?.message ||
                error.message ||
                'Tạo đơn hàng thất bại. Vui lòng thử lại.';
            // Detect email-not-verified error and show actionable toast
            const lowerMsg = (errorMessage || '').toLowerCase();
            if (lowerMsg.includes('chưa xác thực email') || lowerMsg.includes('cha xc thc email')) {
                toast.error('Bạn cần xác thực email để tiếp tục thanh toán.', {
                    id: toastId,
                    description: 'Kiểm tra hộp thư để xác thực. Bạn cũng có thể gửi lại email xác thực.',
                    action: {
                        label: 'Gửi lại email',
                        onClick: async () => {
                            try {
                                const email = user?.email;
                                if (!email) {
                                    toast.error('Không tìm thấy email tài khoản');
                                    return;
                                }
                                const res = await userService.resendEmailConfirmation(email);
                                if (res.success) {
                                    toast.success(res.message || 'đã gửi lại email xác thực');
                                } else {
                                    toast.error(res.message || 'Gửi lại email xác thực thất bại');
                                }
                            } catch (e: any) {
                                toast.error(e?.response?.data?.message || 'Không thể gửi lại email xác thực');
                            }
                        }
                    }
                });
            } else {
                toast.error(errorMessage, { id: toastId });
            }
        }

        logger.info(' Checkout submission completed', { isPlacingOrder: false });
        setIsPlacingOrder(false);
    };

    if (!isAuthenticated || (itemCount === 0 && !isPlacingOrder)) {
        logger.warn(' Checkout: Redirecting due to condition check', {
            isAuthenticated,
            itemCount,
            isPlacingOrder,
            shouldRedirect: !isAuthenticated || (itemCount === 0 && !isPlacingOrder)
        });

        return (
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <LoadingSpinner size="lg" />
            </div>
        );
    }

    return (
        <Suspense fallback={<LoadingSpinner size="lg" />}>
            <div className="container mx-auto px-4 py-8">
                <h1 className="text-3xl font-bold mb-6">Thanh toán</h1>
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                    <div className="lg:col-span-2">
                        <Form {...form}>
                            <form
                                onSubmit={form.handleSubmit(onSubmit)}
                                className="space-y-8"
                            >
                                <Card>
                                    <CardHeader>
                                        <CardTitle>Thông tin giao hàng</CardTitle>
                                    </CardHeader>
                                    <CardContent className="space-y-4">
                                        <FormField
                                            control={form.control}
                                            name="recipientName"
                                            render={({ field }) => (
                                                <FormItem>
                                                    <FormLabel>Tên người nhận</FormLabel>
                                                    <FormControl>
                                                        <Input
                                                            placeholder="Nguyễn Văn A"
                                                            {...field}
                                                        />
                                                    </FormControl>
                                                    <FormMessage />
                                                </FormItem>
                                            )}
                                        />
                                        <FormField
                                            control={form.control}
                                            name="shippingAddress"
                                            render={({ field }) => (
                                                <FormItem>
                                                    <FormLabel>Địa chỉ cụ thể</FormLabel>
                                                    <FormControl>
                                                        <div className="flex flex-col space-y-2">
                                                            <div className="flex items-center space-x-2">
                                                                <Input
                                                                    placeholder="Số nhà, tên đường"
                                                                    {...field}
                                                                />
                                                                <Dialog open={isAddAddressOpen} onOpenChange={setIsAddAddressOpen}>
                                                                    <DialogTrigger asChild>
                                                                        <UIButton type="button" variant="outline" size="sm">Thêm địa chỉ mới</UIButton>
                                                                    </DialogTrigger>
                                                                    <DialogContent className="max-w-md">
                                                                        <DialogHeader>
                                                                            <DialogTitle>Thêm địa chỉ mới</DialogTitle>
                                                                        </DialogHeader>
                                                                        <form onSubmit={handleAddAddress} className="space-y-3">
                                                                            <UIInput value={newAddressForm.fullName} onChange={e => setNewAddressForm(f => ({ ...f, fullName: e.target.value }))} placeholder="Họ và tên" required />
                                                                            <UIInput value={newAddressForm.phoneNumber} onChange={e => setNewAddressForm(f => ({ ...f, phoneNumber: e.target.value }))} placeholder="Số điện thoại" required />
                                                                            <UIInput value={newAddressForm.address} onChange={e => setNewAddressForm(f => ({ ...f, address: e.target.value }))} placeholder="Số nhà, tên đường" required />
                                                                            <ProvinceSelect value={newAddressForm.provinceCode} onValueChange={v => setNewAddressForm(f => ({ ...f, provinceCode: v, districtCode: '', wardCode: '' }))} placeholder="Chọn tỉnh/thành phố" />
                                                                            <DistrictSelect provinceCode={newAddressForm.provinceCode} value={newAddressForm.districtCode} onValueChange={v => setNewAddressForm(f => ({ ...f, districtCode: v, wardCode: '' }))} placeholder="Chọn quận/huyện" disabled={!newAddressForm.provinceCode} />
                                                                            <WardSelect districtCode={newAddressForm.districtCode} value={newAddressForm.wardCode} onValueChange={v => setNewAddressForm(f => ({ ...f, wardCode: v }))} placeholder="Chọn phường/xã" disabled={!newAddressForm.districtCode} />
                                                                            <UIInput value={newAddressForm.postalCode} onChange={e => setNewAddressForm(f => ({ ...f, postalCode: e.target.value }))} placeholder="Mã bưu điện" />
                                                                            <DialogFooter>
                                                                                <UIButton type="button" variant="outline" onClick={() => setIsAddAddressOpen(false)}>Hủy</UIButton>
                                                                                <UIButton type="submit" disabled={isSavingAddress}>Lưu</UIButton>
                                                                            </DialogFooter>
                                                                        </form>
                                                                    </DialogContent>
                                                                </Dialog>
                                                            </div>
                                                            <div className="flex flex-wrap gap-2 mt-2">
                                                                {savedAddresses.map(a => {
                                                                    // Format address display similar to addresses page
                                                                    const province = vietnamAddressService.getProvinceByCode(a.provinceCode);
                                                                    const district = vietnamAddressService.getDistrictByCode(a.districtCode);
                                                                    const ward = vietnamAddressService.getWardByCode(a.wardCode);

                                                                    const addressParts = [
                                                                        a.address,
                                                                        ward?.name || a.wardCode,
                                                                        district?.name || a.districtCode,
                                                                        province?.name || a.provinceCode
                                                                    ].filter(Boolean);

                                                                    const fullAddress = addressParts.join(', ');

                                                                    return (
                                                                        <button key={a.id} type="button" className={`border rounded px-2 py-1 ${selectedAddressId === a.id ? 'bg-blue-100 border-blue-500' : ''}`} onClick={() => setSelectedAddressId(a.id)}>
                                                                            <div className="text-left">
                                                                                <div className="font-medium">{a.fullName}</div>
                                                                                <div className="text-sm text-gray-600">{fullAddress}</div>
                                                                                {a.isDefault && <span className="text-xs text-blue-600">(Mặc định)</span>}
                                                                            </div>
                                                                        </button>
                                                                    );
                                                                })}
                                                            </div>
                                                        </div>
                                                    </FormControl>
                                                    <FormMessage />
                                                </FormItem>
                                            )}
                                        />

                                        <div className="grid gap-4 md:grid-cols-3">
                                            <FormField
                                                control={form.control}
                                                name="provinceCode"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>Tỉnh/Thành phố *</FormLabel>
                                                        <FormControl>
                                                            <ProvinceSelect
                                                                value={field.value}
                                                                onValueChange={(value) => {
                                                                    field.onChange(value);
                                                                    form.setValue('districtCode', '');
                                                                    form.setValue('wardCode', '');
                                                                }}
                                                                placeholder="Chọn tỉnh/thành phố"
                                                            />
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />
                                            <FormField
                                                control={form.control}
                                                name="districtCode"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>Quận/Huyện *</FormLabel>
                                                        <FormControl>
                                                            <DistrictSelect
                                                                provinceCode={form.watch('provinceCode')}
                                                                value={field.value}
                                                                onValueChange={(value) => {
                                                                    field.onChange(value);
                                                                    form.setValue('wardCode', '');
                                                                }}
                                                                placeholder="Chọn quận/huyện"
                                                                disabled={!form.watch('provinceCode')}
                                                            />
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />
                                            <FormField
                                                control={form.control}
                                                name="wardCode"
                                                render={({ field }) => (
                                                    <FormItem>
                                                        <FormLabel>Phường/Xã *</FormLabel>
                                                        <FormControl>
                                                            <WardSelect
                                                                districtCode={form.watch('districtCode')}
                                                                value={field.value}
                                                                onValueChange={field.onChange}
                                                                placeholder="Chọn phường/xã"
                                                                disabled={!form.watch('districtCode')}
                                                            />
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                )}
                                            />
                                        </div>
                                        <FormField
                                            control={form.control}
                                            name="phoneNumber"
                                            render={({ field }) => (
                                                <FormItem>
                                                    <FormLabel>Số điện thoại</FormLabel>
                                                    <FormControl>
                                                        <Input
                                                            placeholder="09xxxxxxxx"
                                                            {...field}
                                                        />
                                                    </FormControl>
                                                    <FormMessage />
                                                </FormItem>
                                            )}
                                        />
                                    </CardContent>
                                </Card>

                                <Card>
                                    <CardHeader>
                                        <CardTitle>Chế độ thanh toán</CardTitle>
                                    </CardHeader>
                                    <CardContent className="p-4">
                                        <div className="flex items-center space-x-2">
                                            <Switch
                                                id="environment-toggle"
                                                checked={environment === 'production'}
                                                onCheckedChange={(checked: boolean) => setEnvironment(checked ? 'production' : 'sandbox')}
                                            />
                                            <label
                                                htmlFor="environment-toggle"
                                                className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                                            >
                                                {environment === 'sandbox' ? 'Chế độ Test (Sandbox)' : 'Chế độ Thực tế (Production)'}
                                            </label>
                                        </div>
                                        <p className="text-sm text-muted-foreground mt-1">
                                            {environment === 'sandbox' ? 'Sử dụng dữ liệu test, không trả tiền thật.' : 'Thanh toán thực tế với tiền thật.'}
                                        </p>
                                    </CardContent>
                                </Card>

                                <Card>
                                    <CardHeader>
                                        <CardTitle>Phương thức thanh toán</CardTitle>
                                    </CardHeader>
                                    <CardContent className="space-y-4">
                                        <FormField
                                            control={form.control}
                                            name="paymentMethod"
                                            render={({ field }) => (
                                                <FormItem className="space-y-2">
                                                    <FormLabel>Chọn phương thức thanh toán</FormLabel>
                                                    <FormControl>
                                                        <RadioGroup
                                                            onValueChange={field.onChange}
                                                            value={field.value}
                                                            className="flex flex-col space-y-1"
                                                        >
                                                            <FormItem className="flex items-center space-x-3 space-y-0">
                                                                <FormControl>
                                                                    <RadioGroupItem value="COD" />
                                                                </FormControl>
                                                                <div className="flex items-center space-x-3">
                                                                    <div className="w-8 h-8 bg-gray-200 rounded flex items-center justify-center">
                                                                        <span className="text-sm font-medium">COD</span>
                                                                    </div>
                                                                    <div>
                                                                        <h3 className="font-medium">Thanh toán khi nhận hàng</h3>
                                                                        <p className="text-sm text-muted-foreground">An toàn và tiện lợi</p>
                                                                    </div>
                                                                </div>
                                                            </FormItem>
                                                            {/* VNPay option removed */}
                                                            <FormItem className="flex items-center space-x-3 space-y-0">
                                                                <FormControl>
                                                                    <RadioGroupItem value="SEPAY" />
                                                                </FormControl>
                                                                <div className="flex items-center space-x-3">
                                                                    <div className="w-8 h-8 bg-green-600 rounded flex items-center justify-center">
                                                                        <span className="text-white text-sm">QR</span>
                                                                    </div>
                                                                    <div>
                                                                        <h3 className="font-medium">SePay</h3>
                                                                        <p className="text-sm text-muted-foreground">Chuyển khoản ngân hàng qua QR</p>
                                                                    </div>
                                                                </div>
                                                            </FormItem>
                                                            <FormItem className="flex items-center space-x-3 space-y-0">
                                                                <FormControl>
                                                                    <RadioGroupItem value="Stripe" />
                                                                </FormControl>
                                                                <div className="flex items-center space-x-3">
                                                                    <div className="w-8 h-8 bg-purple-600 rounded flex items-center justify-center">
                                                                        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z"></path><path d="M15.5 8.5a2.5 2.5 0 0 0-5 0V10h5v4.5a2.5 2.5 0 1 1-5 0V14h-5"></path></svg>
                                                                    </div>
                                                                    <div>
                                                                        <h3 className="font-medium">Stripe</h3>
                                                                        <p className="text-sm text-muted-foreground">Thanh toán bằng thẻ tín dụng/ghi nợ</p>
                                                                    </div>
                                                                </div>
                                                            </FormItem>
                                                            <FormItem className="flex items-center space-x-3 space-y-0">
                                                                <FormControl>
                                                                    <RadioGroupItem value="PayPal" />
                                                                </FormControl>
                                                                <div className="flex items-center space-x-3">
                                                                    <div className="w-8 h-8 bg-blue-800 rounded flex items-center justify-center">
                                                                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="white" viewBox="0 0 16 16"><path d="M3.51 6.6c.22-.15.46-.28.7-.4.24-.12.48-.23.72-.33.5-.2 1.04-.35 1.6-.45.28-.05.56-.09.85-.12.28-.03.57-.05.85-.05.87 0 1.6.2 2.2.6.6.4.9.95.9 1.65 0 .4-.1.75-.3 1.05-.2.3-.5.55-.9.75-.3.15-.6.28-.9.4-.5.2-1.04.35-1.6.45-.28.05-.56.09-.85.12-.28.03-.57.05-.85.05-.9 0-1.65-.2-2.25-.6-.6-.4-.9-.95-.9-1.65 0-.4.1-.75.3-1.05.2-.3.5-.55.9-.75zm.5-2.05c-.3.2-.58.4-.85.6-.27.2-.5.4-.7.6-.3.3-.5.6-.65.9-.15.3-.2.6-.2 1 0 .7.2 1.3.6 1.8.4.5.95.9 1.65 1.15.7.25 1.5.35 2.4.35.8 0 1.5-.1 2.1-.3.6-.2 1.1-.5 1.5-.9.4-.4.7-.9.8-1.5.1-.6.1-1.2 0-1.7-.1-.6-.3-1.1-.6-1.5-.3-.4-.7-.7-1.2-.9-.5-.2-1.1-.3-1.8-.3-.45 0-.9.05-1.35.15-.45.1-.9.2-1.35.3z"></path><path d="M6.13 13.15c-.2-.3-.3-.6-.3-1 0-.3.05-.6.15-.85.1-.25.25-.5.45-.7s.4-.35.6-.5c.2-.15.4-.28.6-.4.4-.2.8-.35 1.25-.45.45-.1.9-.15 1.35-.15.9 0 1.65.2 2.25.6.6.4.9.95.9 1.65 0 .4-.1.75-.3 1.05-.2.3-.5.55-.9.75-.3.15-.6.28-.9.4-.5.2-1.04.35-1.6.45-.28.05-.56.09-.85.12-.28.03-.57.05-.85.05-.87 0-1.6-.2-2.2-.6-.6-.4-.9-.95-.9-1.65 0-.1.01-.2.03-.3h-1.1c-.02.1-.03.2-.03.3 0 .7.2 1.3.6 1.8.4.5.95.9 1.65 1.15.7.25 1.5.35 2.4.35.8 0 1.5-.1 2.1-.3.6-.2 1.1-.5 1.5-.9.4-.4.7-.9.8-1.5.1-.6.1-1.2 0-1.7-.1-.6-.3-1.1-.6-1.5-.3-.4-.7-.7-1.2-.9-.5-.2-1.1-.3-1.8-.3-1 0-1.9.2-2.7.6-.8.4-1.4.9-1.8 1.5-.4.6-.6 1.3-.6 2.1 0 .2.01.4.04.6h1.1z"></path></svg>
                                                                    </div>
                                                                    <div>
                                                                        <h3 className="font-medium">PayPal</h3>
                                                                        <p className="text-sm text-muted-foreground">Thanh toán an toàn qua cổng PayPal</p>
                                                                    </div>
                                                                </div>
                                                            </FormItem>
                                                        </RadioGroup>
                                                    </FormControl>
                                                    <FormMessage />
                                                </FormItem>
                                            )}
                                        />
                                    </CardContent>
                                </Card>

                                <Button
                                    type="submit"
                                    className="w-full"
                                    size="lg"
                                    disabled={isPlacingOrder}
                                >
                                    {isPlacingOrder ? (
                                        <LoadingSpinner />
                                    ) : (
                                        'Đặt hàng & Thanh toán'
                                    )}
                                </Button>
                            </form>
                        </Form>
                    </div>

                    <div className="lg:col-span-1">
                        <Card className="sticky top-24">
                            <CardHeader>
                                <CardTitle>Tóm tắt đơn hàng</CardTitle>
                                <CardDescription>
                                    {itemCount} sản phẩm
                                </CardDescription>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="max-h-64 overflow-y-auto space-y-4 pr-2">
                                    {items.map((item) => (
                                        <div
                                            key={item.id}
                                            className="flex items-center justify-between"
                                        >
                                            <div className="flex items-center space-x-3">
                                                <div className="relative w-12 h-12">
                                                    <Image
                                                        src={
                                                            item.product.imageUrl ||
                                                            '/placeholder-product.jpg'
                                                        }
                                                        alt={item.product.name}
                                                        fill
                                                        className="object-cover rounded-md"
                                                    />
                                                    <span className="absolute -top-2 -right-2 bg-primary text-primary-foreground text-xs rounded-full h-5 w-5 flex items-center justify-center">
                                                        {item.quantity}
                                                    </span>
                                                </div>
                                                <div className="flex-1">
                                                    <p className="text-sm font-medium leading-tight">
                                                        {item.product.name}
                                                    </p>
                                                </div>
                                            </div>
                                            <p className="text-sm font-semibold">
                                                {formatCurrencyPrice(item.totalPrice, selectedCurrency)}
                                            </p>
                                        </div>
                                    ))}
                                </div>
                                <Separator />
                                <div className="space-y-2">
                                    <div className="flex justify-between">
                                        <span>Tạm tính</span>
                                        <span>{formatCurrencyPrice(total, selectedCurrency)}</span>
                                    </div>
                                    <div className="flex justify-between">
                                        <span>Phí vận chuyển</span>
                                        <span>Miễn phí</span>
                                    </div>
                                    <Separator />
                                    <div className="flex justify-between text-lg font-bold">
                                        <span>Tổng cộng</span>
                                        <span>{formatCurrencyPrice(total, selectedCurrency)}</span>
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    </div>
                </div>
            </div>
        </Suspense>
    );
}

export default function CheckoutPage() {
    return (
        <Suspense fallback={
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <CheckoutContent />
        </Suspense>
    );
}