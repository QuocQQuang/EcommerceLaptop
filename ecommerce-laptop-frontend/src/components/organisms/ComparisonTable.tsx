"use client";

import { Price } from '@/components/atoms/Price';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { useCompareStore } from '@/store/compareStore';
import { useWishlistStore } from '@/store/wishlistStore';
import { Product } from '@/types/api';
import Link from 'next/link';

function highlightDiffs(values: Array<string | number | undefined>) {
    const uniq = new Set(values.map(v => (v === undefined ? '__undefined__' : String(v))));
    return uniq.size > 1;
}

export function ComparisonTable() {
    const { items, removeItem, clear } = useCompareStore();
    const { items: wishlistItems } = useWishlistStore();

    if (items.length === 0) {
        return (
            <div className="py-12 text-center">
                <h3 className="text-xl font-semibold">Chưa có sản phẩm nào để so sánh</h3>
                <p className="text-gray-600 mt-2">Thêm sản phẩm từ danh sách yêu thích hoặc trang sản phẩm để so sánh.</p>
                <div className="mt-4 flex items-center justify-center gap-3">
                    <Link href="/products">
                        <Button>Duyệt sản phẩm</Button>
                    </Link>
                    {wishlistItems.length > 0 && (
                        <Link href="/wishlist">
                                <Button variant="outline">Mở danh sách yêu thích</Button>
                        </Link>
                    )}
                </div>
            </div>
        );
    }

    // Collect all unique specifications across items, sorted by category and displayOrder
    const allSpecs = items
        .flatMap(p => p.specifications || [])
        .reduce((unique, spec) => {
            const normalizedName = spec.name.toLowerCase().replace(/\s+/g, '');
            if (!unique.some(u => u.name.toLowerCase().replace(/\s+/g, '') === normalizedName)) {
                unique.push(spec);
            }
            return unique;
        }, [] as typeof items[0]['specifications'])
        .sort((a, b) => (a.category || '').localeCompare(b.category || '') || (a.displayOrder || 0) - (b.displayOrder || 0));

    // Get value for a spec by normalized name
    const getSpecValue = (p: Product, specName: string) => {
        const normalized = specName.toLowerCase().replace(/\s+/g, '');
        const found = (p.specifications || []).find(s => s.name.toLowerCase().replace(/\s+/g, '') === normalized);
        return found?.value || '';
    };

    // Special handling for price (not in specs)
    const priceSpec = { name: 'Price', category: 'Pricing', displayOrder: 0 };
    const allSpecsWithPrice = [priceSpec, ...allSpecs];

    return (
        <div className="space-y-6">
            {/* Desktop / Tablet: table */}
            <div className="hidden md:block">
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Thông số</TableHead>
                            {items.map(p => (
                                <TableHead key={p.id}>
                                    <div className="flex items-center justify-between">
                                        <div className="flex items-center gap-3">
                                            <img src={p.imageUrl} alt={p.name} className="w-16 h-10 object-contain rounded" />
                                            <div>
                                                <div className="font-medium">{p.name}</div>
                                                <div className="text-sm text-gray-500">{p.brand}</div>
                                            </div>
                                        </div>
                                        <div className="flex items-center gap-2">
                                            <Button variant="ghost" size="sm" onClick={() => removeItem(p.id)}>Xóa</Button>
                                        </div>
                                    </div>
                                </TableHead>
                            ))}
                        </TableRow>
                    </TableHeader>

                    <TableBody>
                        {allSpecsWithPrice.map(spec => {
                            const values = items.map(p => {
                                if (spec.name === 'Price') {
                                    return p.discountPrice ?? p.price;
                                }
                                return getSpecValue(p, spec.name);
                            });
                            const isDiff = highlightDiffs(values.map(v => v ?? ''));

                            return (
                                <TableRow key={spec.name} className={isDiff ? 'bg-blue-50' : ''}>
                                    <TableCell className="font-medium w-48">
                                        <div>{spec.name}</div>
                                        {spec.category && <div className="text-xs text-gray-500">{spec.category}</div>}
                                    </TableCell>
                                    {items.map((p) => (
                                        <TableCell key={p.id + '-' + spec.name}>
                                            {spec.name === 'Price' ? (
                                                <Price price={p.price} discountPrice={p.discountPrice} />
                                            ) : (
                                                <div className="text-sm text-gray-700">{String(getSpecValue(p, spec.name))}</div>
                                            )}
                                        </TableCell>
                                    ))}
                                </TableRow>
                            );
                        })}
                    </TableBody>
                </Table>

                <div className="flex items-start gap-4 mt-4">
                    {items.map(p => (
                        <Card key={p.id} className="flex-1">
                            <CardContent className="p-4">
                                <div className="flex items-center justify-between mb-2">
                                    <div className="text-sm text-gray-600">{p.brand}</div>
                                    <div className="text-sm text-gray-600">{p.type || 'Product'}</div>
                                </div>
                                <Link href={`/products/${p.slug}`}>
                                    <Button className="w-full">Mua ngay</Button>
                                </Link>
                            </CardContent>
                        </Card>
                    ))}

                    <div className="flex-shrink-0 self-start pt-4">
                        <Button variant="outline" onClick={() => clear()}>Xóa tất cả</Button>
                    </div>
                </div>
            </div>

            {/* Mobile: accordion-style - Dynamic specs */}
            <div className="md:hidden space-y-3">
                {items.map(p => (
                    <details key={p.id} className="border rounded-lg">
                        <summary className="px-4 py-3 flex items-center justify-between cursor-pointer list-none">
                            <div className="flex items-center gap-3">
                                <img src={p.imageUrl} alt={p.name} className="w-12 h-8 object-contain rounded" />
                                <div>
                                    <div className="font-medium">{p.name}</div>
                                    <div className="text-sm text-gray-500">{p.brand}</div>
                                </div>
                            </div>
                            <div className="flex items-center gap-2">
                                <Button variant="ghost" size="sm" onClick={(e) => { e.preventDefault(); removeItem(p.id); }}>Xóa</Button>
                            </div>
                        </summary>
                        <div className="p-4 space-y-2">
                            {/* Price first */}
                            <div className="border-t pt-2">
                                <div className="text-sm text-gray-600 font-medium">Giá</div>
                                <div className="text-sm text-gray-700"><Price price={p.price} discountPrice={p.discountPrice} /></div>
                            </div>

                            {/* Dynamic specs grouped by category */}
                            {Object.entries(allSpecs.reduce((acc, spec) => {
                                const cat = spec.category || 'General';
                                if (!acc[cat]) acc[cat] = [];
                                acc[cat].push(spec);
                                return acc;
                            }, {} as Record<string, typeof allSpecs>)).map(([category, catSpecs]) => (
                                <div key={category} className="border-t pt-2">
                                    <div className="text-sm text-gray-600 font-semibold mb-2">{category}</div>
                                    {catSpecs.map(spec => (
                                        <div key={spec.name} className="mb-1">
                                            <div className="text-xs text-gray-500">{spec.name}</div>
                                            <div className="text-sm text-gray-700">{getSpecValue(p, spec.name)}</div>
                                        </div>
                                    ))}
                                </div>
                            ))}

                            <Link href={`/products/${p.slug}`}>
                                <Button className="w-full mt-4">Mua ngay</Button>
                            </Link>
                        </div>
                    </details>
                ))}

                <div className="flex justify-center">
                    <Button variant="outline" onClick={() => clear()}>Xóa tất cả</Button>
                </div>
            </div>
        </div>
    );
}

export default ComparisonTable;
