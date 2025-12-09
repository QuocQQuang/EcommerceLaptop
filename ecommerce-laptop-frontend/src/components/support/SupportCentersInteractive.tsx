'use client';

import { LoadingButton } from '@/components/atoms/LoadingButton';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Clock,
  MapPin,
  Phone
} from 'lucide-react';

interface SupportCenter {
  id: number;
  name: string;
  address: string;
  phone: string;
  hours: string;
  services: string[];
}

const supportCenters: SupportCenter[] = [
  {
    id: 1,
    name: 'Trung tm bo hnh TP.HCM',
    address: '123 Nguyn Vn C, Qun 5, TP.HCM',
    phone: '028-3555-1234',
    hours: '8:00 - 17:00 (T2-T6)',
    services: ['Bo hnh laptop', 'Sa cha phn cng', 'Thay th linh kin', 'Ci t phn mm']
  },
  {
    id: 2,
    name: 'Trung tm bo hnh H Ni',
    address: '456 Gii Phng, Hai B Trng, H Ni',
    phone: '024-3666-5678',
    hours: '8:00 - 17:00 (T2-T6)',
    services: ['Bo hnh laptop', 'Sa cha phn cng', 'Thay th linh kin', 'Ci t phn mm']
  },
  {
    id: 3,
    name: 'Trung tm bo hnh  Nng',
    address: '789 L Dun, Hi Chu,  Nng',
    phone: '0236-3777-9012',
    hours: '8:00 - 17:00 (T2-T6)',
    services: ['Bo hnh laptop', 'Sa cha c bn', 'T vn k thut']
  }
];

export function SupportCentersInteractive() {
  const handlePhoneCall = (phone: string) => {
    window.open(`tel:${phone}`);
  };

  return (
    <section className="content-section">
      <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
        <Phone className="w-8 h-8 mr-3 text-green-600" />
         Trung Tm Bo Hnh
      </h2>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {supportCenters.map((center, index) => (
          <Card key={center.id} className="hover:shadow-lg transition-shadow">
            <CardHeader>
              <CardTitle className="text-lg font-bold text-gray-900">
                {center.name}
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-start">
                <MapPin className="w-5 h-5 text-gray-400 mt-0.5" />
                <p className="ml-3 text-gray-600">{center.address}</p>
              </div>

              <div className="flex items-center">
                <Clock className="w-5 h-5 text-gray-400" />
                <p className="ml-3 text-gray-600">{center.hours}</p>
              </div>

              <div>
                <h4 className="font-semibold text-gray-900 mb-2">Dch v:</h4>
                <div className="flex flex-wrap gap-2">
                  {center.services.map((service, serviceIndex) => (
                    <Badge key={serviceIndex} variant="outline" className="text-xs">
                      {service}
                    </Badge>
                  ))}
                </div>
              </div>

              <div className="text-center">
                <LoadingButton
                  variant="outline"
                  loadingKey={`call-center-${index}`}
                  onClick={() => handlePhoneCall(center.phone)}
                >
                   Gi ngay
                </LoadingButton>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </section>
  );
}