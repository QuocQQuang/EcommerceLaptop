'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export default function AddBrandPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Thêm thương hiệu mới</h1>
        <p className="text-muted-foreground">
          Tạo thương hiệu mới cho sản phẩm
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Thêm thương hiệu</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8">
            <p className="text-gray-500">Tính năng đang được phát triển</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}



