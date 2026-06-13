"use client";

import { Button } from '@/components/ui/button';
import { useCompareStore } from '@/store/compareStore';
import { Scale } from 'lucide-react';
import Link from 'next/link';

export default function CompareBar() {
    const { items, clear } = useCompareStore();

    if (items.length === 0) return null;

    return (
        <div className="fixed bottom-6 right-6 z-50">
            <div className="bg-white dark:bg-slate-900 border rounded-lg shadow-lg p-3 flex items-center gap-3">
                <div className="flex items-center gap-2">
                    <Scale className="w-5 h-5 text-blue-600" />
                    <div className="text-sm font-medium">So sánh ({items.length})</div>
                </div>

                <div className="flex items-center gap-2">
                    <Link href="/compare">
                        <Button size="sm" className="bg-blue-600 text-white">So sánh ngay</Button>
                    </Link>
                    <Button size="sm" variant="outline" onClick={() => clear()}>Xóa</Button>
                </div>
            </div>
        </div>
    );
}
