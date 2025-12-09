'use client';

import { LoadingButton } from '@/components/atoms/LoadingButton';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import {
  Car,
  Clock,
  Coffee,
  CreditCard,
  MapPin,
  Navigation,
  Phone,
  Search,
  Shield,
  Star,
  Users,
  Wifi
} from 'lucide-react';
import { useState } from 'react';

interface Store {
  id: number;
  name: string;
  type: 'flagship' | 'regional' | 'authorized';
  address: string;
  district: string;
  city: string;
  phone: string;
  hours: string;
  rating: number;
  reviewCount: number;
  services: string[];
  features: string[];
  image: string;
}

const stores: Store[] = [
  {
    id: 1,
    name: 'EcommerceLaptop Flagship Nguyn Hu',
    type: 'flagship',
    address: '123 Nguyn Hu, Bn Ngh',
    district: 'Qun 1',
    city: 'TP. H Ch Minh',
    phone: '028-3822-1234',
    hours: '8:00 - 22:00',
    rating: 4.9,
    reviewCount: 1247,
    services: ['T vn chuyn su', 'Test my trc tip', 'Bo hnh ti ch', 'Trade-in'],
    features: ['Wifi min ph', 'Khu vc tr coffee', 'Bi  xe', 'Thanh ton th'],
    image: '/images/stores/flagship-nguyen-hue.jpg'
  },
  {
    id: 2,
    name: 'EcommerceLaptop Flagship H Ni',
    type: 'flagship',
    address: '456 B Triu, Hai B Trng',
    district: 'Qun Hai B Trng',
    city: 'H Ni',
    phone: '024-3943-5678',
    hours: '8:00 - 22:00',
    rating: 4.8,
    reviewCount: 982,
    services: ['T vn chuyn su', 'Test my trc tip', 'Bo hnh ti ch', 'Trade-in'],
    features: ['Wifi min ph', 'Khu vc tr coffee', 'Bi  xe', 'Thanh ton th'],
    image: '/images/stores/flagship-hanoi.jpg'
  },
  {
    id: 3,
    name: 'EcommerceLaptop  Nng',
    type: 'regional',
    address: '789 Trn Ph, Hi Chu',
    district: 'Qun Hi Chu',
    city: ' Nng',
    phone: '0236-3591-9012',
    hours: '8:00 - 21:00',
    rating: 4.7,
    reviewCount: 634,
    services: ['T vn sn phm', 'Test my trc tip', 'Bo hnh'],
    features: ['Wifi min ph', 'Bi  xe', 'Thanh ton th'],
    image: '/images/stores/danang.jpg'
  },
  {
    id: 4,
    name: 'EcommerceLaptop Cn Th',
    type: 'regional',
    address: '321 Mu Thn, Ninh Kiu',
    district: 'Qun Ninh Kiu',
    city: 'Cn Th',
    phone: '0292-3831-3456',
    hours: '8:00 - 21:00',
    rating: 4.6,
    reviewCount: 421,
    services: ['T vn sn phm', 'Test my trc tip', 'Bo hnh'],
    features: ['Wifi min ph', 'Bi  xe'],
    image: '/images/stores/cantho.jpg'
  }
];

export function StoreLocatorInteractive() {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCity, setSelectedCity] = useState('all');
  const [filteredStores, setFilteredStores] = useState(stores);

  const handleSearch = () => {
    let filtered = stores;

    if (searchQuery) {
      filtered = filtered.filter(store =>
        store.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        store.address.toLowerCase().includes(searchQuery.toLowerCase()) ||
        store.district.toLowerCase().includes(searchQuery.toLowerCase())
      );
    }

    if (selectedCity && selectedCity !== 'all') {
      filtered = filtered.filter(store => store.city === selectedCity);
    }

    setFilteredStores(filtered);
  };

  const handlePhoneCall = (phone: string) => {
    window.open(`tel:${phone}`);
  };

  const getStoreTypeInfo = (type: Store['type']) => {
    switch (type) {
      case 'flagship':
        return { label: 'Flagship Store', color: 'bg-blue-500 text-white' };
      case 'regional':
        return { label: 'Ca hng khu vc', color: 'bg-green-500 text-white' };
      case 'authorized':
        return { label: 'i l y quyn', color: 'bg-orange-500 text-white' };
    }
  };

  const getServiceIcon = (service: string) => {
    if (service.includes('Wifi')) return <Wifi className="w-4 h-4" />;
    if (service.includes('coffee')) return <Coffee className="w-4 h-4" />;
    if (service.includes('xe')) return <Car className="w-4 h-4" />;
    if (service.includes('th')) return <CreditCard className="w-4 h-4" />;
    return <Shield className="w-4 h-4" />;
  };

  return (
    <div className="space-y-8">
      {/* Search Section */}
      <section id="search-stores" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Search className="w-8 h-8 mr-3 text-blue-600" />
           Tm Ca Hng Gn Bn
        </h2>

        <div className="bg-blue-50 rounded-xl p-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
            <div>
              <label className="block text-sm font-medium text-blue-900 mb-2">
                Tm kim theo tn hoc a ch
              </label>
              <Input
                type="text"
                placeholder="VD: Nguyn Hu, Qun 1..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-blue-900 mb-2">
                Chn thnh ph
              </label>
              <Select value={selectedCity} onValueChange={setSelectedCity}>
                <SelectTrigger>
                  <SelectValue placeholder="Tt c thnh ph" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tt c thnh ph</SelectItem>
                  <SelectItem value="TP. H Ch Minh">TP. H Ch Minh</SelectItem>
                  <SelectItem value="H Ni">H Ni</SelectItem>
                  <SelectItem value=" Nng"> Nng</SelectItem>
                  <SelectItem value="Cn Th">Cn Th</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-end">
              <LoadingButton
                onClick={handleSearch}
                loadingKey="store-search"
                className="w-full bg-blue-600 hover:bg-blue-700 text-white"
              >
                <Search className="w-4 h-4 mr-2" />
                Tm kim
              </LoadingButton>
            </div>
          </div>

          <div className="text-center text-blue-700">
            <p className="text-sm">
               <strong>Tip:</strong> Gi trc  t lch t vn v nhn u i c bit
            </p>
          </div>
        </div>
      </section>

      {/* Store Results */}
      <section className="content-section">
        <h3 className="text-2xl font-bold text-gray-900 mb-6">
           Kt qu tm kim ({filteredStores.length} ca hng)
        </h3>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {filteredStores.map((store) => {
            const storeTypeInfo = getStoreTypeInfo(store.type);

            return (
              <Card key={store.id} className="hover:shadow-lg transition-shadow">
                <CardHeader>
                  <div className="flex justify-between items-start">
                    <div>
                      <CardTitle className="text-lg font-bold text-gray-900">
                        {store.name}
                      </CardTitle>
                      <div className="flex items-center mt-2">
                        <Badge className={storeTypeInfo.color}>
                          {storeTypeInfo.label}
                        </Badge>
                        <div className="flex items-center ml-3">
                          <Star className="w-4 h-4 text-yellow-500 fill-current" />
                          <span className="ml-1 text-sm font-medium">{store.rating}</span>
                          <span className="ml-1 text-sm text-gray-500">
                            ({store.reviewCount} nh gi)
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>
                </CardHeader>

                <CardContent className="space-y-4">
                  <div className="flex items-start">
                    <MapPin className="w-5 h-5 text-gray-400 mt-0.5" />
                    <div className="ml-3">
                      <p className="font-medium text-gray-900">{store.address}</p>
                      <p className="text-sm text-gray-600">{store.district}, {store.city}</p>
                    </div>
                  </div>

                  <div className="flex items-center justify-between">
                    <div className="flex items-center">
                      <Clock className="w-5 h-5 text-gray-400" />
                      <span className="ml-2 text-sm text-gray-600">{store.hours}</span>
                    </div>

                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => handlePhoneCall(store.phone)}
                      className="text-blue-600 hover:text-blue-700 hover:bg-blue-50"
                    >
                      <Phone className="w-4 h-4 mr-1" />
                      {store.phone}
                    </Button>
                  </div>

                  <div>
                    <h4 className="font-semibold text-gray-900 mb-2"> Dch v</h4>
                    <div className="flex flex-wrap gap-2">
                      {store.services.map((service, index) => (
                        <Badge key={index} variant="outline" className="text-xs">
                          {service}
                        </Badge>
                      ))}
                    </div>
                  </div>

                  <div>
                    <h4 className="font-semibold text-gray-900 mb-2"> Tin ch</h4>
                    <div className="flex flex-wrap gap-2">
                      {store.features.map((feature, index) => (
                        <div key={index} className="flex items-center text-xs text-gray-600">
                          {getServiceIcon(feature)}
                          <span className="ml-1">{feature}</span>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3 pt-3">
                    <Button
                      size="sm"
                      variant="outline"
                      className="w-full"
                    >
                      <Navigation className="w-4 h-4 mr-2" />
                      Ch ng
                    </Button>

                    <LoadingButton
                      size="sm"
                      loadingKey={`visit-${store.id}`}
                      className="w-full bg-blue-600 hover:bg-blue-700 text-white"
                    >
                      <Users className="w-4 h-4 mr-2" />
                      t lch thm
                    </LoadingButton>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        {filteredStores.length === 0 && (
          <div className="text-center py-12">
            <div className="text-6xl mb-4"></div>
            <h3 className="text-xl font-semibold text-gray-900 mb-2">
              Khng tm thy ca hng ph hp
            </h3>
            <p className="text-gray-600 mb-4">
              Th thay i t kha tm kim hoc m rng khu vc tm kim
            </p>
            <Button
              onClick={() => {
                setSearchQuery('');
                setSelectedCity('all');
                setFilteredStores(stores);
              }}
              variant="outline"
            >
              Xem tt c ca hng
            </Button>
          </div>
        )}
      </section>
    </div>
  );
}