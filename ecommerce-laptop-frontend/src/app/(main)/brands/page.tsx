import { AppLink } from '@/components/atoms/AppLink';
import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Star } from 'lucide-react';
import Image from 'next/image';

export default function BrandsPage() {
  const tableOfContents = [
    { id: 'premium-brands', title: 'Thng hiu cao cp' },
    { id: 'business-brands', title: 'Thng hiu doanh nghip' },
    { id: 'gaming-brands', title: 'Thng hiu gaming' },
    { id: 'budget-brands', title: 'Thng hiu ph thng' },
    { id: 'why-choose', title: 'Ti sao chn chng ti' }
  ];

  const breadcrumbs = [
    { label: 'Trang ch', href: '/' },
    { label: 'Thng hiu' }
  ];

  return (
    <StaticPageLayout
      title="Thng Hiu Laptop Uy Tn"
      subtitle="Khm ph cc thng hiu laptop hng u th gii vi cht lng c chng minh"
      lastUpdated="18/09/2025"
      author="Team LaptopStore"
      readTime="5"
      tableOfContents={tableOfContents}
      breadcrumbs={breadcrumbs}
    >
      <BrandsContent />
    </StaticPageLayout>
  );
}

function BrandsContent() {
  const premiumBrands = [
    {
      name: 'Apple',
      logo: '/images/brands/apple.png',
      description: 'Thit k ng cp, hiu nng vt tri vi chip M-series',
      rating: 4.8,
      products: 15,
      highlights: ['MacBook Air M3', 'MacBook Pro M3 Max', 'iMac 24"']
    },
    {
      name: 'Dell',
      logo: '/images/brands/dell.png',
      description: 'ng tin cy cho doanh nghip v sng to chuyn nghip',
      rating: 4.6,
      products: 42,
      highlights: ['XPS 13 Plus', 'Alienware m16', 'Inspiron 15 3000']
    },
    {
      name: 'HP',
      logo: '/images/brands/hp.png',
      description: 'a dng dng sn phm t vn phng n gaming',
      rating: 4.5,
      products: 38,
      highlights: ['Spectre x360', 'Omen Gaming', 'Pavilion']
    }
  ];

  const businessBrands = [
    {
      name: 'Lenovo',
      logo: '/images/brands/lenovo.png',
      description: 'ThinkPad huyn thoi v Legion gaming mnh m',
      rating: 4.7,
      products: 35,
      highlights: ['ThinkPad X1 Carbon', 'Legion Pro 7', 'IdeaPad']
    },
    {
      name: 'Asus',
      logo: '/images/brands/asus.png',
      description: 'Cn bng hon ho gia hiu nng v gi c',
      rating: 4.4,
      products: 28,
      highlights: ['ZenBook Pro', 'ROG Strix', 'VivoBook']
    }
  ];

  const gamingBrands = [
    {
      name: 'MSI',
      logo: '/images/brands/msi.png',
      description: 'Chuyn gia laptop gaming vi RGB p mt',
      rating: 4.6,
      products: 22,
      highlights: ['GE78 Raider', 'Katana 15', 'Creator Z16']
    },
    {
      name: 'Acer',
      logo: '/images/brands/acer.png',
      description: 'Predator gaming v Swift siu mng',
      rating: 4.3,
      products: 25,
      highlights: ['Predator Helios', 'Swift X', 'Aspire 5']
    }
  ];

  return (
    <div className="space-y-12">
      {/* Premium Brands Section */}
      <section id="premium-brands" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6">
           Thng Hiu Cao Cp
        </h2>
        <p className="text-gray-600 mb-8 leading-7">
          Nhng thng hiu dn u th gii vi cng ngh tin tin nht,
          thit k ng cp v cht lng c cng nhn ton cu.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {premiumBrands.map((brand) => (
            <BrandCard key={brand.name} brand={brand} />
          ))}
        </div>
      </section>

      {/* Business Brands Section */}
      <section id="business-brands" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6">
           Thng Hiu Doanh Nghip
        </h2>
        <p className="text-gray-600 mb-8 leading-7">
          Ti u cho cng vic chuyn nghip vi  bn cao,
          bo mt tt v h tr doanh nghip ton din.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {businessBrands.map((brand) => (
            <BrandCard key={brand.name} brand={brand} />
          ))}
        </div>
      </section>

      {/* Gaming Brands Section */}
      <section id="gaming-brands" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6">
           Thng Hiu Gaming
        </h2>
        <p className="text-gray-600 mb-8 leading-7">
          Hiu nng nh cao cho game th vi card  ha mnh m,
          tn nhit ti u v thit k gaming c trng.
        </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {gamingBrands.map((brand) => (
            <BrandCard key={brand.name} brand={brand} />
          ))}
        </div>
      </section>

      {/* Why Choose Section */}
      <section id="why-choose" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6">
           Ti Sao Chn LaptopStore?
        </h2>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div className="space-y-6">
            <div className="flex items-start space-x-4">
              <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-blue-600 font-bold">1</span>
              </div>
              <div>
                <h3 className="font-semibold text-gray-900 mb-2">Chnh Hng 100%</h3>
                <p className="text-gray-600">
                  Tt c sn phm u l hng chnh hng, c tem phiu y 
                  v c bo hnh theo chnh sch ca hng.
                </p>
              </div>
            </div>

            <div className="flex items-start space-x-4">
              <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-green-600 font-bold">2</span>
              </div>
              <div>
                <h3 className="font-semibold text-gray-900 mb-2">Gi Tt Nht</h3>
                <p className="text-gray-600">
                  Cam kt gi tt nht th trng vi ch  hon tin
                  nu tm thy gi r hn  ni khc.
                </p>
              </div>
            </div>
          </div>

          <div className="space-y-6">
            <div className="flex items-start space-x-4">
              <div className="w-8 h-8 bg-purple-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-purple-600 font-bold">3</span>
              </div>
              <div>
                <h3 className="font-semibold text-gray-900 mb-2">T Vn Chuyn Su</h3>
                <p className="text-gray-600">
                  i ng chuyn gia t vn min ph  gip bn chn
                  laptop ph hp vi nhu cu v ngn sch.
                </p>
              </div>
            </div>

            <div className="flex items-start space-x-4">
              <div className="w-8 h-8 bg-orange-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-orange-600 font-bold">4</span>
              </div>
              <div>
                <h3 className="font-semibold text-gray-900 mb-2">H Tr Sau Bn</h3>
                <p className="text-gray-600">
                  Dch v h tr 24/7, bo hnh nhanh chng v
                  chm sc khch hng tn tnh.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

function BrandCard({ brand }: { brand: any }) {
  return (
    <Card className="hover:shadow-lg transition-shadow duration-200">
      <CardHeader className="text-center">
        <div className="w-20 h-20 mx-auto mb-4 bg-gray-100 rounded-lg flex items-center justify-center">
          <Image
            src={brand.logo}
            alt={`${brand.name} logo`}
            width={60}
            height={60}
            className="object-contain"
          />
        </div>
        <CardTitle className="text-xl font-bold">{brand.name}</CardTitle>
        <div className="flex items-center justify-center space-x-2">
          <div className="flex items-center">
            <Star className="w-4 h-4 text-yellow-400 fill-current" />
            <span className="text-sm font-medium ml-1">{brand.rating}</span>
          </div>
          <Badge variant="secondary">{brand.products} sn phm</Badge>
        </div>
      </CardHeader>

      <CardContent>
        <p className="text-gray-600 mb-4 text-center leading-6">
          {brand.description}
        </p>

        <div className="space-y-2 mb-4">
          <h4 className="font-medium text-sm text-gray-900">Sn phm ni bt:</h4>
          <ul className="text-sm text-gray-600 space-y-1">
            {brand.highlights.map((product: string, index: number) => (
              <li key={index} className="flex items-center">
                <span className="w-1.5 h-1.5 bg-blue-500 rounded-full mr-2"></span>
                {product}
              </li>
            ))}
          </ul>
        </div>

        <AppLink
          href={`/products?brand=${brand.name.toLowerCase()}`}
          pageType="static"
          className="block w-full text-center bg-blue-600 hover:bg-blue-700 text-white py-2 px-4 rounded-md transition-colors font-medium"
        >
          Xem sn phm {brand.name}
        </AppLink>
      </CardContent>
    </Card>
  );
}