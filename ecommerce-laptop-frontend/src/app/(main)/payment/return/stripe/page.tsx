'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/hooks/useAuth';
import { usePaymentSession } from '@/hooks/usePaymentSession';
import logger from '@/lib/logger';
import { orderService } from '@/services/orderService';
import { CheckCircle, XCircle } from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

interface PaymentResult {
    success: boolean;
    orderId?: string;
    paymentIntentId?: string;
    message: string;
}

function StripeReturnContent() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const [result, setResult] = useState<PaymentResult | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isProcessing, setIsProcessing] = useState(false);
    const { user } = useAuth();
    const { isReady, isValid, userId, error: sessionError, hasOrderAccess } = usePaymentSession();

    useEffect(() => {
        const processReturn = async () => {
            // Prevent multiple processing
            if (isProcessing) {
                logger.info(' Stripe Return: Already processing, skipping');
                return;
            }

            // Wait for session to be ready
            if (!isReady) {
                logger.info(' Stripe Return: Waiting for session validation');
                return;
            }

            // Check if session is valid - only show error if we have a specific error message
            if (!isValid) {
                if (sessionError) {
                    logger.error(' Stripe Return: Invalid session', { sessionError });
                    setResult({
                        success: false,
                        message: sessionError
                    });
                    setIsLoading(false);
                    return;
                } else {
                    // No specific error, might be still loading
                    logger.info(' Stripe Return: Session not ready yet, waiting...');
                    return;
                }
            }

            // Set processing flag
            setIsProcessing(true);

            logger.info(' Stripe Return: Starting payment return processing', {
                pathname: window.location.pathname,
                search: window.location.search,
                userId
            });

            try {
                // Get URL parameters from Stripe return
                const paymentIntent = searchParams?.get('payment_intent');
                const paymentIntentClientSecret = searchParams?.get('payment_intent_client_secret');
                const redirectStatus = searchParams?.get('redirect_status');
                const orderIdStr = searchParams?.get('orderId');
                const orderId = parseInt(orderIdStr || '0');

                logger.info(' Stripe Return: Received payment parameters', {
                    paymentIntent,
                    hasClientSecret: !!paymentIntentClientSecret,
                    redirectStatus,
                    orderId,
                    allParams: searchParams ? Object.fromEntries(searchParams.entries()) : {}
                });

                if (!paymentIntent || orderId <= 0) {
                    logger.error(' Stripe Return: Missing payment intent or order ID');
                    setResult({
                        success: false,
                        message: 'Thiu thng tin thanh ton hoc n hng'
                    });
                    setIsLoading(false);
                    return;
                }

                // Check if payment was successful based on redirect_status
                const isSuccess = redirectStatus === 'succeeded';

                logger.info(' Stripe Return: Payment result determined', {
                    isSuccess,
                    redirectStatus,
                    criteria: 'redirect_status === "succeeded"'
                });

                if (isSuccess) {
                    logger.info(' Stripe Return: Payment SUCCESS detected', {
                        orderId,
                        paymentIntentId: paymentIntent,
                        redirectStatus
                    });

                    // Fetch order status to verify and check ownership
                    const orderRes = await orderService.getOrderStatus(orderId);

                    // Ownership check using payment session hook
                    const orderUserId = orderRes.customerId;
                    if (!orderUserId || !hasOrderAccess(orderUserId)) {
                        logger.warn(' Stripe Return: Unauthorized access to order', {
                            orderId,
                            userId,
                            orderUserId: orderUserId,
                            customerId: orderRes.customerId,
                            orderUserIdField: orderRes.userId,
                            hasAccess: false
                        });
                        toast.error('Bn khng c quyn truy cp n hng ny.');
                        router.push('/account/orders');
                        return;
                    }

                    // Status validation
                    if (orderRes.paymentStatus === 'Completed' || orderRes.status.toLowerCase() === 'confirmed') {
                        logger.info(' Stripe Return: Order confirmed, redirecting to dashboard', {
                            orderId,
                            status: orderRes.status,
                            paymentStatus: orderRes.paymentStatus
                        });

                        // Set success result before redirect
                        setResult({
                            success: true,
                            orderId: orderIdStr || 'N/A',
                            paymentIntentId: paymentIntent,
                            message: 'Thanh ton Stripe thnh cng! n hng  c xc nhn.'
                        });

                        toast.success('Thanh ton thnh cng! Chuyn n trang ti khon.');

                        // Delay redirect to show success message
                        setTimeout(() => {
                            router.push('/account');
                        }, 2000);
                        return;
                    } else {
                        logger.warn(' Stripe Return: Success redirect but order not confirmed', {
                            orderId,
                            status: orderRes.status,
                            paymentStatus: orderRes.paymentStatus
                        });
                        setResult({
                            success: true,
                            orderId: orderIdStr || 'N/A',
                            paymentIntentId: paymentIntent,
                            message: 'Thanh ton Stripe thnh cng, nhng n hng ang c x l.'
                        });
                        toast.success('Thanh ton thnh cng! n hng ang c xc nhn.');
                    }
                } else if (redirectStatus === 'failed') {
                    logger.warn(' Stripe Return: Payment FAILED detected', {
                        orderId,
                        paymentIntentId: paymentIntent,
                        redirectStatus
                    });

                    setResult({
                        success: false,
                        message: 'Thanh ton Stripe tht bi'
                    });
                    toast.error('Thanh ton tht bi!');
                    logger.error(' Stripe Return: Error toast displayed');
                } else {
                    logger.warn(' Stripe Return: Unknown redirect status', {
                        redirectStatus,
                        paymentIntent,
                        orderId
                    });

                    // Check payment intent status via API if needed
                    setResult({
                        success: false,
                        message: 'Trng thi thanh ton khng xc nh'
                    });
                    toast.warning('Trng thi thanh ton khng r rng');
                }
            } catch (error) {
                logger.error(' Stripe Return: Exception during processing', {
                    error: error instanceof Error ? error.message : 'Unknown error',
                    stack: error instanceof Error ? error.stack : undefined
                });
                console.error('Error processing Stripe return:', error);
                setResult({
                    success: false,
                    message: 'C li xy ra khi x l kt qu thanh ton'
                });
                toast.error('C li xy ra!');
            } finally {
                setIsLoading(false);
                logger.info(' Stripe Return: Processing completed', {
                    isLoading: false,
                    hasResult: !!result
                });
            }
        };

        processReturn();
    }, [searchParams, isReady, isValid, userId, isProcessing]);

    if (isLoading || !isReady || (isReady && !isValid && !sessionError)) {
        return (
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <Card className="w-full max-w-md">
                    <CardContent className="p-8 min-h-[200px] flex items-center justify-center">
                        <div className="flex flex-col items-center justify-center text-center space-y-4 w-full">
                            <div className="flex justify-center">
                                <LoadingSpinner size="lg" />
                            </div>
                            <p className="text-muted-foreground">
                                {!isReady ? 'ang xc thc phin ng nhp...' :
                                    (isReady && !isValid && !sessionError) ? 'ang ti thng tin ngi dng...' :
                                        'ang x l kt qu thanh ton...'}
                            </p>
                        </div>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
            <Card className="w-full max-w-md">
                <CardHeader className="text-center">
                    <div className="mx-auto mb-4">
                        {result?.success ? (
                            <CheckCircle className="h-16 w-16 text-green-500" />
                        ) : (
                            <XCircle className="h-16 w-16 text-red-500" />
                        )}
                    </div>
                    <CardTitle className={result?.success ? 'text-green-700' : 'text-red-700'}>
                        {result?.success ? 'Thanh ton thnh cng!' : 'Thanh ton tht bi!'}
                    </CardTitle>
                    <CardDescription>
                        {result?.message}
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    {result?.success && (
                        <div className="space-y-2 text-sm">
                            {result.orderId && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M n hng:</span>
                                    <span className="font-medium">#{result.orderId}</span>
                                </div>
                            )}
                            {result.paymentIntentId && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">Payment Intent:</span>
                                    <span className="font-medium text-xs">{result.paymentIntentId}</span>
                                </div>
                            )}
                        </div>
                    )}

                    <div className="flex flex-col gap-2">
                        {result?.success ? (
                            <>
                                <Button asChild className="w-full">
                                    <Link href="/account/orders">
                                        Xem n hng
                                    </Link>
                                </Button>
                                <Button variant="outline" asChild className="w-full">
                                    <Link href="/">
                                        Tip tc mua sm
                                    </Link>
                                </Button>
                            </>
                        ) : (
                            <>
                                <Button asChild className="w-full">
                                    <Link href="/checkout">
                                        Th li thanh ton
                                    </Link>
                                </Button>
                                <Button variant="outline" asChild className="w-full">
                                    <Link href="/cart">
                                        Quay li gi hng
                                    </Link>
                                </Button>
                            </>
                        )}
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}

export default function StripeReturnPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <StripeReturnContent />
        </Suspense>
    );
}