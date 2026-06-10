'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Shield, ArrowLeft, Home } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';

export default function UnauthorizedPage() {
    const router = useRouter();

    return (
        <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center p-4">
            <Card className="w-full max-w-md">
                <CardHeader className="text-center">
                    <div className="mx-auto mb-4 w-16 h-16 bg-red-100 dark:bg-red-900/20 rounded-full flex items-center justify-center">
                        <Shield className="w-8 h-8 text-red-600 dark:text-red-400" />
                    </div>
                    <CardTitle className="text-2xl">Không có quyền truy cập</CardTitle>
                    <CardDescription className="text-base">
                        Bạn không có quyền truy cập vào trang này. 
                        Vui lòng liên hệ quản trị viên nếu bạn cho rằng đây là lỗi.
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="flex flex-col gap-3">
                        <Button 
                            onClick={() => router.back()}
                            variant="outline"
                            className="w-full"
                        >
                            <ArrowLeft className="w-4 h-4 mr-2" />
                            Quay lại
                        </Button>
                        <Button 
                            asChild
                            className="w-full"
                        >
                            <Link href="/">
                                <Home className="w-4 h-4 mr-2" />
                                Về trang chủ
                            </Link>
                        </Button>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}