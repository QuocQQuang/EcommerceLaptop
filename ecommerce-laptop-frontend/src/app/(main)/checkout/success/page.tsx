'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle, Package } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense } from 'react';

function CheckoutSuccessContent() {
    const searchParams = useSearchParams();
    const orderId = searchParams?.get('orderId');

    return (
        <div className="container mx-auto px-4 py-16 flex justify-center">
            <Card className="w-full max-w-lg text-center">
                <CardHeader>
                    <div className="mx-auto bg-green-100 rounded-full h-20 w-20 flex items-center justify-center">
                        <CheckCircle className="h-12 w-12 text-green-600" />
                    </div>
                    <CardTitle className="mt-6 text-2xl font-bold">
                        t hng thnh cng!
                    </CardTitle>
                </CardHeader>
                <CardContent className="space-y-6">
                    <p className="text-muted-foreground">
                        Cảm ơn bạn đã mua sắm! Đơn hàng của bạn đã được nhận và
                        ang c x l.
                    </p>
                    {orderId && (
                        <div className="bg-gray-100 rounded-lg p-3">
                            <p className="text-sm text-gray-600">M n hng ca bn:</p>
                            <p className="text-lg font-bold text-primary">#{orderId}</p>
                        </div>
                    )}
                    <div className="flex flex-col sm:flex-row gap-4 justify-center">
                        {orderId && (
                            <Button asChild>
                                <Link href={`/account/orders/${orderId}`}>
                                    <Package className="mr-2 h-4 w-4" />
                                    Xem chi tit n hng
                                </Link>
                            </Button>
                        )}
                        <Button asChild>
                            <Link href="/account/orders">
                                <Package className="mr-2 h-4 w-4" />
                                Xem lch s n hng
                            </Link>
                        </Button>
                        <Button variant="outline" asChild>
                            <Link href="/products">Tip tc mua sm</Link>
                        </Button>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}

export default function CheckoutSuccessPage() {
    return (
        <Suspense fallback={<div className="container mx-auto px-4 py-16 flex justify-center">Loading...</div>}>
            <CheckoutSuccessContent />
        </Suspense>
    );
}