'use client';

import { cartService } from '@/services/cartService';
import { useCartStore } from '@/store/cartStore';
// Use Sonner toast to display notifications; Root layout already renders <Toaster />
import { Product } from '@/types/api';
import { useCallback } from 'react';
import { toast } from 'sonner';

export function useCart() {
    const {
        items,
        total,
        itemCount,
        sessionId,
        addItem,
        removeItem,
        updateQuantity,
        clearCart,
        setItems,
        calculateTotals,
    } = useCartStore();

    const addToCart = useCallback(async (product: Product, quantity: number = 1, variantId?: number) => {
        try {
            addItem(product, quantity, variantId);

            // Sync with backend
            if (sessionId) {
                await cartService.addToCart({
                    productId: product.id,
                    quantity,
                    sessionId,
                    // include bundle items when present on the product
                    ...(product.bundleItems ? { bundleItems: product.bundleItems } : {}),
                    // include variant ID when present
                    ...(variantId ? { variantId } : {}),
                });
            }

            toast.success('Đã thêm vào giỏ hàng', {
                description: `${product.name} đã được thêm vào giỏ hàng`,
            });
        } catch (error: any) {
            console.error('Error adding to cart:', error);

            // Handle specific error types
            if (error.message?.includes('409:')) {
                const errorMessage = error.message.replace('409: ', '');
                // Parse detailed inventory error
                if (errorMessage.includes('Insufficient stock')) {
                    const match = errorMessage.match(/Available: (\d+), Requested: (\d+)/);
                    if (match) {
                        const [, available, requested] = match;
                        toast.error('Không đủ hàng', {
                            description: `Sản phẩm còn lại: ${available} sản phẩm, bạn yêu cầu: ${requested} sản phẩm.`,
                        });
                        return;
                    }
                }
                toast.error('Không đủ hàng', {
                    description: errorMessage,
                });
                return;
            }

            toast.error('Lỗi', {
                description: 'Không thể thêm sản phẩm vào giỏ hàng',
            });
        }
    }, [addItem, sessionId]);

    const removeFromCart = useCallback(async (itemId: number) => {
        try {
            console.log('FrontEnd Debug: Removing item', itemId, 'Session:', sessionId);
            removeItem(itemId);

            // Sync with backend
            if (sessionId) {
                console.log('FrontEnd Debug: Sending DELETE request for', itemId);
                await cartService.removeCartItem(itemId, sessionId);
                console.log('FrontEnd Debug: DELETE request success');
            } else {
                console.warn('FrontEnd Debug: No session ID');
            }

            toast.success('Đã xóa khỏi giỏ hàng', {
                description: 'Sản phẩm đã được xóa khỏi giỏ hàng',
            });
        } catch (error) {
            console.error('FrontEnd Debug: Error removing from cart:', error);
            toast.error('Lỗi', {
                description: 'Không thể xóa sản phẩm khỏi giỏ hàng',
            });
        }
    }, [removeItem, sessionId]);

    const updateCartQuantity = useCallback(async (itemId: number, quantity: number) => {
        try {
            updateQuantity(itemId, quantity);

            // Sync with backend
            if (sessionId) {
                await cartService.updateCartItem({
                    itemId,
                    quantity,
                    sessionId,
                });
            }
        } catch (error) {
            console.error('Error updating cart quantity:', error);
            toast.error('Lỗi', {
                description: 'Không thể cập nhật số lượng',
            });
        }
    }, [updateQuantity, sessionId]);

    const clearCartItems = useCallback(async () => {
        try {
            clearCart();

            // Sync with backend
            if (sessionId) {
                await cartService.clearCart(sessionId);
            }

            toast.success('Đã xóa giỏ hàng', {
                description: 'Tất cả sản phẩm đã được xóa khỏi giỏ hàng',
            });
        } catch (error) {
            console.error('Error clearing cart:', error);
            toast.error('Lỗi', {
                description: 'Không thể xóa giỏ hàng',
            });
        }
    }, [clearCart, sessionId]);

    const refreshCart = useCallback(async () => {
        try {
            if (!sessionId) return;

            const cart = await cartService.getCart(sessionId);
            if (cart && cart.items) {
                // Backend returns flat DTO (productName, productImageUrl, etc.)
                // Frontend Store expects nested Product object.
                // We need to map it to avoid "Cannot read properties of undefined (reading 'imageUrl')"
                const mappedItems = cart.items.map((item: any) => {
                    // Check if it's already in correct format (has item.product)
                    if (item.product) return item;

                    // Otherwise map from flat DTO
                    return {
                        ...item,
                        product: {
                            id: item.productId,
                            name: item.productName || 'Unknown Product',
                            sku: item.productSku || 'UNKNOWN',
                            imageUrl: item.productImageUrl || '',
                            brand: item.brand,
                            price: item.unitPrice, // Fallback
                            // Required fallback fields to satisfy Product interface
                            description: '',
                            slug: '',
                            type: 'Laptop',
                            isActive: item.isAvailable ?? true,
                            stockQuantity: item.stockQuantity ?? 0,
                            images: item.productImageUrl ? [{ imageUrl: item.productImageUrl, isPrimary: true }] : [],
                            categories: [],
                            specifications: [],
                            createdAt: new Date().toISOString(),
                            updatedAt: new Date().toISOString(),
                            isVariant: false,
                            isBaseProduct: true,
                            variants: []
                        }
                    };
                });

                setItems(mappedItems);

                // Recalculate totals based on the fetched data
                // calculateTotals(); // Store calculates based on items
            }
        } catch (error) {
            console.error('Error refreshing cart:', error);
            // Silent error for sync - don't spam toast on every page load
        }
    }, [sessionId, setItems, calculateTotals]);

    return {
        items,
        total,
        itemCount,
        addToCart,
        removeFromCart,
        updateCartQuantity,
        clearCartItems,
        refreshCart, // Expose for SyncProvider
        isEmpty: items.length === 0,
    };
}
