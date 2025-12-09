'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect } from 'react';
import { toast } from 'sonner';

function PaymentMethodsContent() {
    const searchParams = useSearchParams();
    const orderId = parseInt(searchParams?.get('orderId') || '0');

    useEffect(() => {
        if (orderId <= 0) {
            toast.error('ID n hng khng hp l');
            window.location.href = '/checkout';
        }
    }, [orderId]);

    if (orderId <= 0) {
        return (
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <Card>
                    <CardContent className="text-center p-8">
                        <h2 className="text-xl font-semibold mb-2">Li</h2>
                        <p>n hng khng hp l. <Link href="/checkout" className="text-primary underline">Quay li thanh ton</Link></p>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8">
            <Link href="/checkout" className="flex items-center text-muted-foreground hover:text-foreground mb-6">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Quay li
            </Link>

            <Card>
                <CardHeader>
                    <CardTitle>Chn phng thc thanh ton</CardTitle>
                    <CardDescription>
                        n hng #{orderId} - Chn phng thc ph hp vi bn
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <Button variant="outline" asChild className="h-auto p-4">
                            <Link href={`/payment/vnpay?orderId=${orderId}&env=sandbox`} className="flex items-center space-x-3 w-full">
                                <div className="w-10 h-10 bg-blue-600 rounded flex items-center justify-center">
                                    <span className="text-white font-bold text-sm">VN</span>
                                </div>
                                <div className="text-left">
                                    <h3 className="font-medium">VNPAY</h3>
                                    <p className="text-sm text-muted-foreground">Thanh ton qua VNPAY</p>
                                </div>
                            </Link>
                        </Button>

                        <Button variant="outline" asChild className="h-auto p-4">
                            <Link href={`/payment/sepay?orderId=${orderId}&env=sandbox`} className="flex items-center space-x-3 w-full">
                                <div className="w-10 h-10 bg-green-600 rounded flex items-center justify-center">
                                    <span className="text-white font-bold text-sm">QR</span>
                                </div>
                                <div className="text-left">
                                    <h3 className="font-medium">SePay</h3>
                                    <p className="text-sm text-muted-foreground">Chuyn khon ngn hng qua QR</p>
                                </div>
                            </Link>
                        </Button>

                        <Button variant="outline" asChild className="h-auto p-4">
                            <Link href={`/payment/paypal?orderId=${orderId}&env=sandbox`} className="flex items-center space-x-3 w-full">
                                <div className="w-10 h-10 bg-blue-400 rounded flex items-center justify-center">
                                    <span className="text-white font-bold text-sm">PP</span>
                                </div>
                                <div className="text-left">
                                    <h3 className="font-medium">PayPal</h3>
                                    <p className="text-sm text-muted-foreground">Thanh ton qua PayPal</p>
                                </div>
                            </Link>
                        </Button>

                        <Button variant="outline" asChild className="h-auto p-4">
                            <Link href={`/payment/stripe?orderId=${orderId}&env=sandbox`} className="flex items-center space-x-3 w-full">
                                <div className="w-10 h-10 bg-purple-600 rounded flex items-center justify-center">
                                    <span className="text-white font-bold text-sm">ST</span>
                                </div>
                                <div className="text-left">
                                    <h3 className="font-medium">Stripe</h3>
                                    <p className="text-sm text-muted-foreground">Thanh ton th quc t</p>
                                </div>
                            </Link>
                        </Button>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}

export default function PaymentMethodsPage() {
    return (
        <Suspense fallback={
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <Card>
                    <CardContent className="text-center p-8">
                        <p>ang ti...</p>
                    </CardContent>
                </Card>
            </div>
        }>
            <PaymentMethodsContent />
        </Suspense>
    );
}