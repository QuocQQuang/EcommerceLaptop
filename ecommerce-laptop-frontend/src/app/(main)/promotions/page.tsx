import { AppLink } from '@/components/atoms/AppLink';
import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import { PromotionInteractiveContent } from '@/components/promotions/PromotionInteractiveContent';
import { Badge } from '@/components/ui/badge';
import { Gift, Percent, Tag } from 'lucide-react';

export default function PromotionsPage() {
  const tableOfContents = [
    { id: 'flash-sale', title: 'Flash Sale - Gi sc 24h' },
    { id: 'monthly-deals', title: 'u i thng 9' },
    { id: 'student-discount', title: 'u i sinh vin' },
    { id: 'trade-in', title: 'Thu c i mi' },
    { id: 'bulk-discount', title: 'Mua s lng ln' },
    { id: 'voucher-codes', title: 'M gim gi' }
  ];

  const breadcrumbs = [
    { label: 'Trang ch', href: '/' },
    { label: 'Khuyn mi' }
  ];

  return (
    <StaticPageLayout
      title=" Khuyn Mi & u i c Bit"
      subtitle="Cp nht lin tc cc chng trnh khuyn mi hp dn, tit kim ti a cho bn"
      lastUpdated="18/09/2025"
      author="Team Marketing"
      readTime="10"
      tableOfContents={tableOfContents}
      breadcrumbs={breadcrumbs}
    >
      <PromotionContent />
    </StaticPageLayout>
  );
}

function PromotionContent() {
  return (
    <div className="space-y-12">
      {/* Interactive Content */}
      <PromotionInteractiveContent />

      {/* Monthly Deals Section */}
      <section id="monthly-deals" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Gift className="w-8 h-8 mr-3 text-blue-600" />
           u i Thng 9/2025
        </h2>

        <div className="bg-blue-50 rounded-xl p-6 mb-8">
          <div className="text-center mb-6">
            <h3 className="text-2xl font-bold text-blue-900 mb-2">
              Thng khuyn mi ln nht trong nm
            </h3>
            <p className="text-blue-700">
              Gim gi ln n 40% cho tt c dng laptop cao cp
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="bg-white p-6 rounded-lg text-center">
              <div className="text-4xl mb-4"></div>
              <h4 className="font-bold text-blue-900 mb-2">Laptop vn phng</h4>
              <div className="text-2xl font-bold text-blue-600 mb-2">Gim 25%</div>
              <p className="text-sm text-blue-700">p dng cho tt c laptop vn phng</p>
            </div>

            <div className="bg-white p-6 rounded-lg text-center">
              <div className="text-4xl mb-4"></div>
              <h4 className="font-bold text-blue-900 mb-2">Laptop gaming</h4>
              <div className="text-2xl font-bold text-blue-600 mb-2">Gim 30%</div>
              <p className="text-sm text-blue-700">Km ph kin gaming min ph</p>
            </div>

            <div className="bg-white p-6 rounded-lg text-center">
              <div className="text-4xl mb-4"></div>
              <h4 className="font-bold text-blue-900 mb-2">Laptop  ha</h4>
              <div className="text-2xl font-bold text-blue-600 mb-2">Gim 35%</div>
              <p className="text-sm text-blue-700">Tng km phn mm thit k</p>
            </div>
          </div>
        </div>
      </section>

      {/* Student Discount Section */}
      <section id="student-discount" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Percent className="w-8 h-8 mr-3 text-green-600" />
           u i Sinh Vin
        </h2>

        <div className="bg-green-50 rounded-xl p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-2xl font-bold text-green-900 mb-4">
                Gim 30% cho sinh vin
              </h3>
              <ul className="space-y-3 text-green-800">
                <li className="flex items-center">
                  <span className="w-2 h-2 bg-green-500 rounded-full mr-3"></span>
                  p dng cho tt c laptop di 25 triu
                </li>
                <li className="flex items-center">
                  <span className="w-2 h-2 bg-green-500 rounded-full mr-3"></span>
                  Tng km balo laptop + chut khng dy
                </li>
                <li className="flex items-center">
                  <span className="w-2 h-2 bg-green-500 rounded-full mr-3"></span>
                  Bo hnh m rng 36 thng
                </li>
                <li className="flex items-center">
                  <span className="w-2 h-2 bg-green-500 rounded-full mr-3"></span>
                  H tr tr gp 0% li sut
                </li>
              </ul>

              <div className="mt-6 p-4 bg-white rounded-lg">
                <h4 className="font-bold text-green-900 mb-2"> iu kin p dng:</h4>
                <ul className="text-sm text-green-700 space-y-1">
                  <li> Xut trnh th sinh vin hoc giy xc nhn</li>
                  <li> p dng cho hc sinh, sinh vin t 16-25 tui</li>
                  <li> Mi th sinh vin ch mua c 1 my/nm</li>
                  <li> Khng p dng cng vi chng trnh khc</li>
                </ul>
              </div>
            </div>

            <div className="text-center">
              <div className="bg-white p-8 rounded-xl shadow-md">
                <div className="text-6xl mb-4"></div>
                <h3 className="text-xl font-bold text-green-900 mb-4">
                  ng k nhn u i
                </h3>
                <p className="text-green-700 mb-6">
                  Xc thc ti khon sinh vin  nhn m gim gi c quyn
                </p>
                <AppLink
                  href="/student-verification"
                  pageType="static"
                  className="inline-block bg-green-600 hover:bg-green-700 text-white px-6 py-3 rounded-lg font-semibold transition-colors"
                >
                  Xc thc ngay
                </AppLink>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Trade-in Section */}
      <section id="trade-in" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Tag className="w-8 h-8 mr-3 text-orange-600" />
           Thu C i Mi
        </h2>

        <div className="bg-orange-50 rounded-xl p-6">
          <div className="text-center mb-8">
            <h3 className="text-2xl font-bold text-orange-900 mb-2">
              nh gi laptop c - Nhn tin mt ngay
            </h3>
            <p className="text-orange-700">
              Gi thu c cao nht th trng, quy trnh nhanh chng ch 15 pht
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
            <div className="text-center">
              <div className="w-16 h-16 bg-orange-200 rounded-full flex items-center justify-center mx-auto mb-3">
                <span className="text-2xl"></span>
              </div>
              <h4 className="font-semibold text-orange-900 mb-2">ng k online</h4>
              <p className="text-sm text-orange-700">in thng tin laptop c</p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-orange-200 rounded-full flex items-center justify-center mx-auto mb-3">
                <span className="text-2xl"></span>
              </div>
              <h4 className="font-semibold text-orange-900 mb-2">nh gi</h4>
              <p className="text-sm text-orange-700">Chuyn vin kim tra ti nh</p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-orange-200 rounded-full flex items-center justify-center mx-auto mb-3">
                <span className="text-2xl"></span>
              </div>
              <h4 className="font-semibold text-orange-900 mb-2">Nhn tin</h4>
              <p className="text-sm text-orange-700">Thanh ton ngay ti ch</p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-orange-200 rounded-full flex items-center justify-center mx-auto mb-3">
                <span className="text-2xl"></span>
              </div>
              <h4 className="font-semibold text-orange-900 mb-2">Mua mi</h4>
              <p className="text-sm text-orange-700">p dng ngay vo my mi</p>
            </div>
          </div>

          <div className="bg-white p-6 rounded-lg">
            <h4 className="font-bold text-orange-900 mb-4 text-center">
               Bng gi thu c tham kho
            </h4>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="text-center p-4 border border-orange-200 rounded-lg">
                <h5 className="font-semibold text-orange-900 mb-2">MacBook</h5>
                <div className="text-lg font-bold text-orange-600">8-25 triu</div>
                <p className="text-sm text-orange-700">Ty theo nm v cu hnh</p>
              </div>
              <div className="text-center p-4 border border-orange-200 rounded-lg">
                <h5 className="font-semibold text-orange-900 mb-2">Laptop Gaming</h5>
                <div className="text-lg font-bold text-orange-600">Di 2000$</div>
                <p className="text-sm text-orange-700">RTX 20/30/40 series</p>
              </div>
              <div className="text-center p-4 border border-orange-200 rounded-lg">
                <h5 className="font-semibold text-orange-900 mb-2">Laptop vn phng</h5>
                <div className="text-lg font-bold text-orange-600">2-12 triu</div>
                <p className="text-sm text-orange-700">Dell, HP, Lenovo, Asus</p>
              </div>
            </div>
          </div>

          <div className="text-center mt-6">
            <AppLink
              href="/trade-in-quote"
              pageType="static"
              className="inline-block bg-orange-600 hover:bg-orange-700 text-white px-8 py-3 rounded-lg font-semibold transition-colors"
            >
              nh gi laptop c ngay
            </AppLink>
          </div>
        </div>
      </section>

      {/* Bulk Discount Section */}
      <section id="bulk-discount" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Gift className="w-8 h-8 mr-3 text-purple-600" />
           u i Doanh Nghip
        </h2>

        <div className="bg-purple-50 rounded-xl p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-2xl font-bold text-purple-900 mb-4">
                Gim gi theo s lng
              </h3>

              <div className="space-y-4">
                <div className="bg-white p-4 rounded-lg border-l-4 border-purple-500">
                  <div className="flex justify-between items-center">
                    <span className="font-semibold text-purple-900">5-10 my</span>
                    <Badge className="bg-purple-100 text-purple-800">Gim 5%</Badge>
                  </div>
                  <p className="text-sm text-purple-700 mt-1">+ Tng setup min ph</p>
                </div>

                <div className="bg-white p-4 rounded-lg border-l-4 border-purple-500">
                  <div className="flex justify-between items-center">
                    <span className="font-semibold text-purple-900">11-20 my</span>
                    <Badge className="bg-purple-100 text-purple-800">Gim 10%</Badge>
                  </div>
                  <p className="text-sm text-purple-700 mt-1">+ H tr k thut 6 thng</p>
                </div>

                <div className="bg-white p-4 rounded-lg border-l-4 border-purple-500">
                  <div className="flex justify-between items-center">
                    <span className="font-semibold text-purple-900">21-50 my</span>
                    <Badge className="bg-purple-100 text-purple-800">Gim 15%</Badge>
                  </div>
                  <p className="text-sm text-purple-700 mt-1">+ o to s dng min ph</p>
                </div>

                <div className="bg-white p-4 rounded-lg border-l-4 border-purple-500">
                  <div className="flex justify-between items-center">
                    <span className="font-semibold text-purple-900">50+ my</span>
                    <Badge className="bg-purple-100 text-purple-800">Gim 20%</Badge>
                  </div>
                  <p className="text-sm text-purple-700 mt-1">+ Dedicated support team</p>
                </div>
              </div>
            </div>

            <div>
              <h3 className="text-xl font-bold text-purple-900 mb-4">
                 Dch v doanh nghip
              </h3>

              <ul className="space-y-3 text-purple-800">
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-purple-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>T vn chuyn su:</strong><br />
                    <span className="text-sm">Phn tch nhu cu v  xut gii php ti u</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-purple-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Giao hng v setup:</strong><br />
                    <span className="text-sm">Trin khai ti vn phng, ci t phn mm</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-purple-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Bo hnh u tin:</strong><br />
                    <span className="text-sm">Hotline ring, thi gian phn hi trong 2h</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-purple-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Thanh ton linh hot:</strong><br />
                    <span className="text-sm">Chuyn khon, tr gp, leasing</span>
                  </div>
                </li>
              </ul>

              <div className="mt-6 text-center">
                <AppLink
                  href="/enterprise-contact"
                  pageType="static"
                  className="inline-block bg-purple-600 hover:bg-purple-700 text-white px-6 py-3 rounded-lg font-semibold transition-colors"
                >
                   Lin h t vn
                </AppLink>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}