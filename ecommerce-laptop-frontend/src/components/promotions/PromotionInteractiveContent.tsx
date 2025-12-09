'use client';

import { LoadingButton } from '@/components/atoms/LoadingButton';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Clock, Tag, Zap } from 'lucide-react';

interface FlashSaleItem {
  name: string;
  originalPrice: string;
  salePrice: string;
  discount: string;
  timeLeft: string;
  image: string;
  soldCount: number;
  totalStock: number;
  features: string[];
}

export function PromotionInteractiveContent() {
  const flashSaleItems: FlashSaleItem[] = [
    {
      name: 'MacBook Air M2 13"',
      originalPrice: '32,990,000',
      salePrice: '26,990,000',
      discount: '18%',
      timeLeft: '05:42:18',
      image: '',
      soldCount: 47,
      totalStock: 100,
      features: ['M2 8-core', '8GB RAM', '256GB SSD', 'Midnight']
    },
    {
      name: 'Dell XPS 13 Plus',
      originalPrice: '28,990,000',
      salePrice: '22,990,000',
      discount: '21%',
      timeLeft: '05:42:18',
      image: '',
      soldCount: 38,
      totalStock: 80,
      features: ['Intel i7-12700H', '16GB RAM', '512GB SSD', 'OLED 3.5K']
    },
    {
      name: 'ASUS ROG Strix G15',
      originalPrice: '35,990,000',
      salePrice: '29,990,000',
      discount: '17%',
      timeLeft: '05:42:18',
      image: '',
      soldCount: 62,
      totalStock: 120,
      features: ['RTX 4060', 'AMD Ryzen 7', '16GB RAM', '144Hz']
    }
  ];

  const voucherCodes = [
    {
      code: 'LAPTOP500K',
      title: 'Gim 500,000',
      description: 'Cho n hng t 15 triu',
      expiry: '30/09/2025',
      used: 847,
      total: 1000,
      color: 'bg-red-500'
    },
    {
      code: 'STUDENT30',
      title: 'Gim 30%',
      description: 'Dnh cho sinh vin',
      expiry: '31/12/2025',
      used: 234,
      total: 500,
      color: 'bg-blue-500'
    },
    {
      code: 'FIRSTBUY200K',
      title: 'Gim 200,000',
      description: 'Khch hng mi',
      expiry: '15/10/2025',
      used: 156,
      total: 300,
      color: 'bg-green-500'
    },
    {
      code: 'FREESHIP',
      title: 'Min ph ship',
      description: 'Ton quc',
      expiry: '25/09/2025',
      used: 1247,
      total: 2000,
      color: 'bg-purple-500'
    }
  ];

  const copyToClipboard = async (code: string) => {
    try {
      await navigator.clipboard.writeText(code);
      // You could add a toast notification here
    } catch (err) {
      console.error('Failed to copy: ', err);
    }
  };

  const handleShopNow = (productName: string) => {
    // Handle shop now action
    console.log(`Shopping for: ${productName}`);
  };

  return (
    <div className="space-y-12">
      {/* Flash Sale Section */}
      <section id="flash-sale" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Zap className="w-8 h-8 mr-3 text-red-600" />
           Flash Sale - Gi Sc 24h
        </h2>

        <div className="bg-gradient-to-r from-red-500 to-orange-500 rounded-xl p-6 text-white mb-8">
          <div className="text-center">
            <h3 className="text-2xl font-bold mb-2"> ANG DIN RA</h3>
            <p className="text-lg mb-4">Thi gian cn li:</p>
            <div className="flex justify-center space-x-4 text-2xl font-mono font-bold">
              <div className="bg-black/20 px-4 py-2 rounded">05</div>
              <span>:</span>
              <div className="bg-black/20 px-4 py-2 rounded">42</div>
              <span>:</span>
              <div className="bg-black/20 px-4 py-2 rounded">18</div>
            </div>
            <div className="flex justify-center space-x-4 text-sm mt-2">
              <span>Gi</span>
              <span>Pht</span>
              <span>Giy</span>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {flashSaleItems.map((item, index) => (
            <Card key={index} className="overflow-hidden hover:shadow-xl transition-shadow border-2 border-red-200">
              <CardContent className="p-0">
                <div className="bg-red-500 text-white p-4 text-center">
                  <div className="text-4xl mb-2">{item.image}</div>
                  <Badge className="bg-white text-red-600 font-bold">
                    -{item.discount}
                  </Badge>
                </div>

                <div className="p-4">
                  <h3 className="font-bold text-lg text-gray-900 mb-2">{item.name}</h3>

                  <div className="flex items-center space-x-2 mb-3">
                    <span className="text-2xl font-bold text-red-600">{item.salePrice}</span>
                    <span className="text-sm text-gray-500 line-through">{item.originalPrice}</span>
                  </div>

                  <div className="flex flex-wrap gap-1 mb-3">
                    {item.features.map((feature, idx) => (
                      <Badge key={idx} variant="secondary" className="text-xs">
                        {feature}
                      </Badge>
                    ))}
                  </div>

                  <div className="mb-3">
                    <div className="flex justify-between text-sm text-gray-600 mb-1">
                      <span> bn: {item.soldCount}</span>
                      <span>Cn li: {item.totalStock - item.soldCount}</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div
                        className="bg-red-500 h-2 rounded-full transition-all"
                        style={{ width: `${(item.soldCount / item.totalStock) * 100}%` }}
                      />
                    </div>
                  </div>

                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center text-red-600 text-sm">
                      <Clock className="w-4 h-4 mr-1" />
                      <span>{item.timeLeft}</span>
                    </div>
                  </div>

                  <LoadingButton
                    className="w-full bg-red-600 hover:bg-red-700"
                    loadingKey={`flash-sale-${index}`}
                    onClick={() => handleShopNow(item.name)}
                  >
                     Mua ngay
                  </LoadingButton>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>

        <div className="text-center mt-8">
          <LoadingButton
            size="lg"
            variant="outline"
            loadingKey="view-all-flash-sale"
            className="border-red-500 text-red-600 hover:bg-red-50"
          >
            Xem tt c Flash Sale
          </LoadingButton>
        </div>
      </section>

      {/* Voucher Codes Section */}
      <section id="voucher-codes" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Tag className="w-8 h-8 mr-3 text-purple-600" />
           M Gim Gi
        </h2>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {voucherCodes.map((voucher, index) => (
            <Card key={index} className="relative overflow-hidden hover:shadow-lg transition-shadow">
              <div className={`absolute top-0 left-0 right-0 h-1 ${voucher.color}`} />

              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-lg">{voucher.title}</CardTitle>
                  <Badge variant="secondary" className="text-xs">
                    {voucher.used}/{voucher.total}
                  </Badge>
                </div>
              </CardHeader>

              <CardContent>
                <div className="bg-gray-100 p-3 rounded-lg mb-4 text-center">
                  <div className="font-mono font-bold text-lg text-gray-900 mb-1">
                    {voucher.code}
                  </div>
                  <div className="text-sm text-gray-600">
                    {voucher.description}
                  </div>
                </div>

                <div className="text-xs text-gray-500 mb-3 text-center">
                  Ht hn: {voucher.expiry}
                </div>

                <div className="w-full bg-gray-200 rounded-full h-1 mb-3">
                  <div
                    className={`h-1 rounded-full transition-all ${voucher.color}`}
                    style={{ width: `${(voucher.used / voucher.total) * 100}%` }}
                  />
                </div>

                <LoadingButton
                  size="sm"
                  variant="outline"
                  loadingKey={`copy-voucher-${index}`}
                  onClick={() => copyToClipboard(voucher.code)}
                  className="w-full"
                >
                   Sao chp m
                </LoadingButton>
              </CardContent>
            </Card>
          ))}
        </div>

        <div className="mt-8 text-center bg-purple-50 rounded-xl p-6">
          <h3 className="text-xl font-bold text-purple-900 mb-2">
             Cch s dng m gim gi
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm text-purple-800">
            <div>
              <div className="text-2xl mb-2"></div>
              <strong>Bc 1:</strong> Thm sn phm vo gi hng
            </div>
            <div>
              <div className="text-2xl mb-2"></div>
              <strong>Bc 2:</strong> Nhp m ti trang thanh ton
            </div>
            <div>
              <div className="text-2xl mb-2"></div>
              <strong>Bc 3:</strong> Nhn ngay u i
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}