import { AppLink } from '@/components/atoms/AppLink';
import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import { StoreLocatorInteractive } from '@/components/store-locator/StoreLocatorInteractive';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Clock,
  Package,
  Shield,
  Star,
  Users
} from 'lucide-react';

export default function StoreLocatorPage() {
  const tableOfContents = [
    { id: 'search-stores', title: 'Tm ca hng' },
    { id: 'flagship-stores', title: 'Ca hng flagship' },
    { id: 'regional-stores', title: 'Ca hng khu vc' },
    { id: 'store-services', title: 'Dch v ti ca hng' },
    { id: 'visit-tips', title: 'Li khuyn khi n ca hng' }
  ];

  const breadcrumbs = [
    { label: 'Trang ch', href: '/' },
    { label: 'H thng ca hng' }
  ];

  return (
    <StaticPageLayout
      title=" H Thng Ca Hng"
      subtitle="Khm ph 50+ ca hng trn ton quc vi khng gian hin i v i ng t vn chuyn nghip"
      lastUpdated="18/09/2025"
      author="Store Operations Team"
      readTime="12"
      tableOfContents={tableOfContents}
      breadcrumbs={breadcrumbs}
    >
      <StoreLocatorContent />
    </StaticPageLayout>
  );
}

function StoreLocatorContent() {
  return (
    <div className="space-y-12">
      {/* Interactive Store Locator */}
      <StoreLocatorInteractive />

      {/* Store Services Section */}
      <section id="store-services" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Package className="w-8 h-8 mr-3 text-purple-600" />
           Dch V Ti Ca Hng
        </h2>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <Card className="hover:shadow-lg transition-shadow">
            <CardHeader>
              <CardTitle className="flex items-center text-lg">
                <Users className="w-6 h-6 mr-3 text-blue-600" />
                T vn chuyn su
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2 text-gray-600">
                <li> Phn tch nhu cu s dng chi tit</li>
                <li>  xut cu hnh ph hp ngn sch</li>
                <li> So snh cc dng sn phm</li>
                <li> T vn ph kin i km</li>
              </ul>
            </CardContent>
          </Card>

          <Card className="hover:shadow-lg transition-shadow">
            <CardHeader>
              <CardTitle className="flex items-center text-lg">
                <Star className="w-6 h-6 mr-3 text-yellow-600" />
                Test sn phm
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2 text-gray-600">
                <li> Tri nghim thc t trc khi mua</li>
                <li> Test hiu nng vi phn mm chuyn dng</li>
                <li> Kim tra cht lng mn hnh, bn phm</li>
                <li> Demo cc tnh nng c bit</li>
              </ul>
            </CardContent>
          </Card>

          <Card className="hover:shadow-lg transition-shadow">
            <CardHeader>
              <CardTitle className="flex items-center text-lg">
                <Shield className="w-6 h-6 mr-3 text-green-600" />
                Bo hnh ti ch
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2 text-gray-600">
                <li> Kim tra v sa cha ngay ti ca hng</li>
                <li> Thay th linh kin chnh hng</li>
                <li> Backup d liu trc sa cha</li>
                <li> Bo hnh m rng c ph</li>
              </ul>
            </CardContent>
          </Card>
        </div>
      </section>

      {/* Visit Tips Section */}
      <section id="visit-tips" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Clock className="w-8 h-8 mr-3 text-orange-600" />
           Li Khuyn Khi n Ca Hng
        </h2>

        <div className="bg-orange-50 rounded-xl p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-xl font-bold text-orange-900 mb-4">
                 Trc khi n
              </h3>
              <ul className="space-y-3 text-orange-800">
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Gi in t lch:</strong><br />
                    <span className="text-sm">m bo c nhn vin t vn sn sng</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Chun b ngn sch:</strong><br />
                    <span className="text-sm">C khung gi r rng  t vn chnh xc</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Lit k nhu cu:</strong><br />
                    <span className="text-sm">Gaming, vn phng,  ha, lp trnh...</span>
                  </div>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="text-xl font-bold text-orange-900 mb-4">
                 Khi n ca hng
              </h3>
              <ul className="space-y-3 text-orange-800">
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Mang theo giy t:</strong><br />
                    <span className="text-sm">CMND/CCCD  lm th tc mua hng</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Test k sn phm:</strong><br />
                    <span className="text-sm">Kim tra tt c cc cng, phm, touchpad</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Hi v promotion:</strong><br />
                    <span className="text-sm">Cc u i hin ti, qu tng km</span>
                  </div>
                </li>
              </ul>
            </div>
          </div>

          <div className="mt-8 p-4 bg-white rounded-lg">
            <h4 className="font-bold text-orange-900 mb-3 text-center">
               Checklist mua laptop hon ho
            </h4>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-2">
                <h5 className="font-semibold text-orange-900">Hardware</h5>
                <ul className="text-sm text-orange-700 space-y-1">
                  <li> CPU ph hp vi cng vic</li>
                  <li> RAM  cho multitasking</li>
                  <li> Storage SSD cho tc </li>
                  <li> GPU ph hp vi  ha</li>
                </ul>
              </div>
              <div className="space-y-2">
                <h5 className="font-semibold text-orange-900">Ngoi hnh</h5>
                <ul className="text-sm text-orange-700 space-y-1">
                  <li> Kch thc ph hp</li>
                  <li> Trng lng mang vc</li>
                  <li> Cht lng build solid</li>
                  <li> Bn phm comfortable</li>
                </ul>
              </div>
              <div className="space-y-2">
                <h5 className="font-semibold text-orange-900">Dch v</h5>
                <ul className="text-sm text-orange-700 space-y-1">
                  <li> Chnh sch bo hnh</li>
                  <li> Gi software km theo</li>
                  <li> H tr sau bn hng</li>
                  <li> Trade-in laptop c</li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Store Hours & Contact */}
      <section className="content-section">
        <div className="bg-gray-50 rounded-xl p-6">
          <h3 className="text-xl font-bold text-gray-900 mb-4 text-center">
             Thng Tin Lin H & Gi M Ca
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center p-4 bg-white rounded-lg">
              <div className="text-3xl mb-2"></div>
              <h4 className="font-semibold text-gray-900 mb-2">Gi m ca</h4>
              <p className="text-gray-600">
                Th 2 - Ch nht<br />
                8:00 - 22:00
              </p>
            </div>

            <div className="text-center p-4 bg-white rounded-lg">
              <div className="text-3xl mb-2"></div>
              <h4 className="font-semibold text-gray-900 mb-2">Hotline</h4>
              <p className="text-gray-600">
                1900-1234 (min ph)<br />
                H tr 24/7
              </p>
            </div>

            <div className="text-center p-4 bg-white rounded-lg">
              <div className="text-3xl mb-2"></div>
              <h4 className="font-semibold text-gray-900 mb-2">Chat h tr</h4>
              <p className="text-gray-600">
                Zalo, Facebook, Website<br />
                Phn hi trong 5 pht
              </p>
            </div>
          </div>

          <div className="text-center mt-6">
            <AppLink
              href="/contact"
              pageType="static"
              className="inline-block bg-blue-600 hover:bg-blue-700 text-white px-8 py-3 rounded-lg font-semibold transition-colors"
            >
               Gi yu cu h tr
            </AppLink>
          </div>
        </div>
      </section>
    </div>
  );
}