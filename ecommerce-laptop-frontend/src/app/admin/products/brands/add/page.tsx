'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

export default function AddBrandPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Thm thng hiu mi</h1>
        <p className="text-muted-foreground">
          To thng hiu mi cho sn phm
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Thm thng hiu</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-center py-8">
            <p className="text-gray-500">Tnh nng ang c pht trin</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}