"use client";

import { ProductCard } from '@/components/molecules/ProductCard';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { useCart } from '@/hooks/useCart';
import { useCompareStore } from '@/store/compareStore';
import { useWishlistStore } from '@/store/wishlistStore';
import { Heart } from 'lucide-react';
import Link from 'next/link';
import { useMemo, useState } from 'react';

type ViewMode = 'grid' | 'list';

export function WishlistView() {
    const { items, removeItem, setNote } = useWishlistStore();
    const { toggleItem: toggleCompare, isInCompare } = useCompareStore();
    const { addToCart } = useCart();

    const [view, setView] = useState<ViewMode>('grid');
    const [sort, setSort] = useState<string>('added_desc');
    const [brandFilter, setBrandFilter] = useState<string>('all');

    const brands = useMemo(() => {
        const uniqueBrands = new Set<string>();
        items.forEach(item => {
            const brand = (item.product.brand ?? '').trim();
            if (brand.length > 0) {
                uniqueBrands.add(brand);
            }
        });
        return Array.from(uniqueBrands).sort();
    }, [items]);

    const sorted = useMemo(() => {
        let list = [...items];
        if (brandFilter !== 'all') list = list.filter(i => i.product.brand === brandFilter);
        switch (sort) {
            case 'price_asc':
                list.sort((a, b) => (a.product.discountPrice ?? a.product.price) - (b.product.discountPrice ?? b.product.price));
                break;
            case 'price_desc':
                list.sort((a, b) => (b.product.discountPrice ?? b.product.price) - (a.product.discountPrice ?? a.product.price));
                break;
            case 'added_asc':
                list.sort((a, b) => new Date(a.addedAt).getTime() - new Date(b.addedAt).getTime());
                break;
            default:
                // added_desc
                list.sort((a, b) => new Date(b.addedAt).getTime() - new Date(a.addedAt).getTime());
        }
        return list;
    }, [items, sort, brandFilter]);

    if (items.length === 0) {
        return (
            <div className="py-12 text-center">
                <div className="mx-auto w-24 h-24 rounded-full bg-gray-100 flex items-center justify-center mb-6">
                    <Heart className="w-10 h-10 text-gray-400" />
                </div>
                <h3 className="text-2xl font-semibold">Danh sch yu thch trng!</h3>
                <p className="text-gray-600 mt-2">Thm mt vi laptop bn thch  bt u.</p>
                <Link href="/products" className="inline-block mt-4">
                    <Button>Duyt sn phm</Button>
                </Link>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                    <div className="flex items-center gap-4">
                        <div className="flex items-center gap-2">
                            <label className="text-sm text-gray-600">Hin th</label>
                            <Select value={view} onValueChange={(v: any) => setView(v)}>
                                <SelectTrigger className="h-8 w-36">
                                    <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="grid">Li</SelectItem>
                                    <SelectItem value="list">Danh sch</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>

                        <div className="flex items-center gap-2">
                            <label className="text-sm text-gray-600">Sp xp</label>
                            <Select value={sort} onValueChange={(v: any) => setSort(v)}>
                                <SelectTrigger className="h-8 w-44">
                                    <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="added_desc">Mi nht</SelectItem>
                                    <SelectItem value="added_asc">C nht</SelectItem>
                                    <SelectItem value="price_asc">Gi: Thp  Cao</SelectItem>
                                    <SelectItem value="price_desc">Gi: Cao  Thp</SelectItem>
                                </SelectContent>
                            </Select>
                        </div>

                        <div className="flex items-center gap-2">
                            <label className="text-sm text-gray-600">Thng hiu</label>
                            <Select value={brandFilter} onValueChange={(v: any) => setBrandFilter(v)}>
                                <SelectTrigger className="h-8 w-40">
                                    <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="all">Tt c</SelectItem>
                                    {brands.map(b => (
                                        <SelectItem key={b} value={b}>{b}</SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>
                    </div>
                </div>

                <div className="text-sm text-gray-500">{items.length} sn phm</div>
            </div>

            <div className={view === 'grid' ? 'grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6' : 'space-y-4'}>
                {sorted.map(item => (
                    <div key={item.product.id}>
                        {view === 'grid' ? (
                            <div>
                                <ProductCard product={item.product} />
                                <div className="mt-2 flex items-center gap-2">
                                    <Button variant="outline" size="sm" onClick={() => removeItem(item.product.id)}>Xa</Button>
                                    <Button size="sm" onClick={() => addToCart(item.product, 1)}>Thm vo gi</Button>
                                    <Button variant={isInCompare(item.product.id) ? 'default' : 'outline'} size="sm" onClick={() => toggleCompare(item.product)}>
                                        {isInCompare(item.product.id) ? ' chn' : 'So snh'}
                                    </Button>
                                </div>
                                <div className="mt-2">
                                    <Input
                                        value={item.note ?? ''}
                                        placeholder="Ghi ch lu sau..."
                                        onChange={(e) => setNote(item.product.id, e.target.value)}
                                    />
                                </div>
                            </div>
                        ) : (
                            <div className="flex gap-4 items-start">
                                <div className="w-32 flex-shrink-0">
                                    <ProductCard product={item.product} variant="compact" />
                                </div>
                                <div className="flex-1">
                                    <div className="flex items-center justify-between">
                                        <h4 className="font-medium">{item.product.name}</h4>
                                        <div className="flex items-center gap-2">
                                            <Button variant="outline" size="sm" onClick={() => removeItem(item.product.id)}>Xa</Button>
                                            <Button size="sm" onClick={() => addToCart(item.product, 1)}>Thm vo gi</Button>
                                            <Button variant={isInCompare(item.product.id) ? 'default' : 'outline'} size="sm" onClick={() => toggleCompare(item.product)}>
                                                {isInCompare(item.product.id) ? ' chn' : 'So snh'}
                                            </Button>
                                        </div>
                                    </div>

                                    <div className="mt-2">
                                        <Input
                                            value={item.note ?? ''}
                                            placeholder="Ghi ch lu sau..."
                                            onChange={(e) => setNote(item.product.id, e.target.value)}
                                        />
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}

export default WishlistView;
