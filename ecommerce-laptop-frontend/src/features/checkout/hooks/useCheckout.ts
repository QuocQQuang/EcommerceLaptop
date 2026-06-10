import { useCallback, useState } from 'react';
import { useCartStore } from '@/store/cartStore';
import { useLogger } from '@/hooks/useLogger';
import { cartService } from '@/services/cartService';
import { vietnamAddressService } from '@/utils/vietnam-address';
import type { CartItem } from '@/types/api';

export interface ShippingFormData {
    recipientName: string;
    shippingAddress: string;
    provinceCode: string;
    districtCode: string;
    wardCode: string;
    shippingPostalCode?: string;
    phoneNumber: string;
    paymentMethod: 'COD' | 'SEPAY' | 'PayPal' | 'Stripe';
}

export interface CartSyncResult {
    success: boolean;
    syncedItems: number;
    error?: string;
}

export interface FormattedAddress {
    shippingCity: string;
    shippingProvince: string;
    fullAddress: string;
}

/**
 * Hook for checkout operations including cart sync and address formatting.
 * Extracts hardcoded logic from checkout page for reusability and testability.
 */
export function useCheckout() {
    const logger = useLogger('useCheckout');
    const { items } = useCartStore();
    const [isSyncing, setIsSyncing] = useState(false);
    const [syncError, setSyncError] = useState<string | null>(null);

    /**
     * Syncs local cart items to the server.
     * Clears server cart first, then adds each item from local cart.
     */
    const syncCartToServer = useCallback(async (): Promise<CartSyncResult> => {
        setIsSyncing(true);
        setSyncError(null);

        logger.info(' Syncing local cart to server', {
            localItemCount: items.length,
            itemsStructure: items.map((item: CartItem) => ({
                productId: item.productId,
                id: item.id,
                quantity: item.quantity,
                hasProduct: !!item.product
            }))
        });

        try {
            // Clear server cart first
            await cartService.clearCart();

            // Add each item from local cart to server cart
            for (const item of items) {
                // Handle nested product structure and extract correct productId
                let productId = item.productId;

                // Check if timestamp ID (too large for Int32)
                if (!productId || productId > 2147483647) {
                    const product = item.product as { data?: { id?: number }; id?: number } | undefined;
                    productId = product?.data?.id || product?.id || 0;
                }

                logger.info(' Processing cart item', {
                    originalProductId: item.productId,
                    extractedProductId: productId,
                    quantity: item.quantity,
                    isValidInt32: productId <= 2147483647
                });

                if (!productId || productId <= 0 || productId > 2147483647) {
                    const errorMsg = `Invalid product ID: ${productId}. Must be a positive Int32.`;
                    logger.error(' Invalid productId for cart item', {
                        item,
                        productId,
                        maxInt32: 2147483647
                    });
                    throw new Error(errorMsg);
                }

                await cartService.addToCart({
                    productId: productId,
                    quantity: item.quantity
                });

                logger.debug(' Added item to server cart', {
                    productId: productId,
                    quantity: item.quantity
                });
            }

            logger.info(' Cart sync completed', { syncedItems: items.length });
            setIsSyncing(false);
            return { success: true, syncedItems: items.length };

        } catch (error: unknown) {
            const errorMessage = error instanceof Error ? error.message : 'Unknown error';
            logger.error(' Cart sync failed', { error: errorMessage });

            // Handle specific error types
            if (errorMessage.includes('409:')) {
                const cleanedMessage = errorMessage.replace('409: ', '');
                // Parse detailed inventory error
                if (cleanedMessage.includes('Insufficient stock')) {
                    const match = cleanedMessage.match(/Available: (\d+), Requested: (\d+)/);
                    if (match) {
                        const [, available, requested] = match;
                        const stockError = `Sản phẩm không đủ hàng! Còn lại: ${available} sản phẩm, bạn yêu cầu: ${requested} sản phẩm.`;
                        setSyncError(stockError);
                        setIsSyncing(false);
                        return { success: false, syncedItems: 0, error: stockError };
                    }
                }
                const inventoryError = `Lỗi tồn kho: ${cleanedMessage}`;
                setSyncError(inventoryError);
                setIsSyncing(false);
                return { success: false, syncedItems: 0, error: inventoryError };
            }

            const genericError = 'Failed to sync cart to server';
            setSyncError(genericError);
            setIsSyncing(false);
            return { success: false, syncedItems: 0, error: genericError };
        }
    }, [items, logger]);

    /**
     * Formats shipping address data into display-ready strings.
     */
    const formatShippingAddress = useCallback((data: ShippingFormData): FormattedAddress => {
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
    }, []);

    /**
     * Builds full shipping address string for backend.
     */
    const buildFullShippingAddress = useCallback((data: ShippingFormData): string => {
        const addressInfo = formatShippingAddress(data);
        return [
            data.recipientName,
            data.phoneNumber,
            data.shippingAddress,
            data.wardCode,
            data.districtCode,
            addressInfo.shippingCity
        ].filter(Boolean).join(', ');
    }, [formatShippingAddress]);

    return {
        // State
        isSyncing,
        syncError,

        // Actions
        syncCartToServer,
        formatShippingAddress,
        buildFullShippingAddress,
    };
}
