import { AppLink } from '@/components/atoms/AppLink';
import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import { SupportCentersInteractive } from '@/components/support/SupportCentersInteractive';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Clock,
  Download,
  HeadphonesIcon,
  HelpCircle,
  Mail,
  MapPin,
  MessageCircle,
  Phone,
  RefreshCw,
  Shield,
  Truck,
  Users
} from 'lucide-react';

export default function SupportPage() {
  const tableOfContents = [
    { id: 'contact-info', title: 'Thng tin lin h' },
    { id: 'warranty', title: 'Chnh sch bo hnh' },
    { id: 'shipping', title: 'Vn chuyn & Giao hng' },
    { id: 'return-policy', title: 'i tr sn phm' },
    { id: 'faq', title: 'Cu hi thng gp' },
    { id: 'service-centers', title: 'Trung tm bo hnh' },
    { id: 'technical-support', title: 'H tr k thut' }
  ];

  const breadcrumbs = [
    { label: 'Trang ch', href: '/' },
    { label: 'H tr khch hng' }
  ];

  return (
    <StaticPageLayout
      title=" H Tr Khch Hng"
      subtitle="Chng ti lun sn sng h tr bn 24/7 vi i ng chuyn vin giu kinh nghim"
      lastUpdated="18/09/2025"
      author="Team Support"
      readTime="8"
      tableOfContents={tableOfContents}
      breadcrumbs={breadcrumbs}
    >
      <SupportContent />
    </StaticPageLayout>
  );
}

function SupportContent() {
  const contactMethods = [
    {
      icon: <Phone className="w-6 h-6 text-blue-600" />,
      title: 'Hotline 24/7',
      info: '1900-1234',
      description: 'T vn v h tr min ph',
      availability: 'Hot ng 24/7'
    },
    {
      icon: <MessageCircle className="w-6 h-6 text-green-600" />,
      title: 'Live Chat',
      info: 'Chat ngay',
      description: 'H tr trc tip qua website',
      availability: '6:00 - 23:00 hng ngy'
    },
    {
      icon: <Mail className="w-6 h-6 text-purple-600" />,
      title: 'Email Support',
      info: 'support@laptopstore.vn',
      description: 'Phn hi trong vng 2 gi',
      availability: 'Phn hi nhanh chng'
    },
    {
      icon: <MapPin className="w-6 h-6 text-red-600" />,
      title: 'Showroom',
      info: '15+ ca hng',
      description: 'Tri nghim trc tip sn phm',
      availability: '8:00 - 22:00 hng ngy'
    }
  ];

  const faqItems = [
    {
      question: 'Lm th no  kim tra bo hnh ca sn phm?',
      answer: 'Bn c th kim tra tnh trng bo hnh bng cch nhp serial number hoc m n hng trn trang tra cu bo hnh ca chng ti.'
    },
    {
      question: 'Ti c th i tr sn phm trong bao lu?',
      answer: 'Chng ti chp nhn i tr trong vng 15 ngy k t ngy mua hng vi iu kin sn phm cn nguyn seal, y  ph kin v ha n.'
    },
    {
      question: 'Chi ph vn chuyn c tnh nh th no?',
      answer: 'Min ph vn chuyn cho n hng t 3 triu ng tr ln trong ni thnh. Cc khu vc khc tnh ph theo khong cch v trng lng.'
    },
    {
      question: 'Lm sao  c h tr ci t phn mm?',
      answer: 'Chng ti cung cp dch v ci t min ph Windows, Office v phn mm c bn. Lin h hotline  t lch hn.'
    }
  ];

  return (
    <div className="space-y-12">
      {/* Contact Information Section */}
      <section id="contact-info" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <HeadphonesIcon className="w-8 h-8 mr-3 text-blue-600" />
           Lin H Vi Chng Ti
        </h2>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {contactMethods.map((method, index) => (
            <Card key={index} className="text-center hover:shadow-lg transition-shadow">
              <CardHeader>
                <div className="w-16 h-16 mx-auto bg-gray-100 rounded-full flex items-center justify-center mb-4">
                  {method.icon}
                </div>
                <CardTitle className="text-lg">{method.title}</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="font-semibold text-xl text-gray-900 mb-2">
                  {method.info}
                </div>
                <p className="text-gray-600 text-sm mb-2">{method.description}</p>
                <Badge variant="secondary" className="text-xs">
                  {method.availability}
                </Badge>
              </CardContent>
            </Card>
          ))}
        </div>

        <div className="mt-8 text-center">
          <Button
            size="lg"
            className="bg-blue-600 hover:bg-blue-700"
          >
             Bt u tr chuyn ngay
          </Button>
        </div>
      </section>

      {/* Warranty Policy Section */}
      <section id="warranty" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Shield className="w-8 h-8 mr-3 text-green-600" />
           Chnh Sch Bo Hnh
        </h2>

        <div className="bg-green-50 rounded-xl p-6 mb-8">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center">
              <div className="text-4xl mb-3"></div>
              <h3 className="font-semibold text-green-900 mb-2">Bo hnh chnh hng</h3>
              <p className="text-green-700 text-sm">Ton b sn phm c bo hnh chnh hng t nh sn xut</p>
            </div>
            <div className="text-center">
              <div className="text-4xl mb-3"></div>
              <h3 className="font-semibold text-green-900 mb-2">Bo hnh nhanh</h3>
              <p className="text-green-700 text-sm">Thi gian bo hnh trung bnh 3-5 ngy lm vic</p>
            </div>
            <div className="text-4xl mb-3"></div>
            <h3 className="font-semibold text-green-900 mb-2">i mi 1:1</h3>
            <p className="text-green-700 text-sm">i mi ngay nu li phn cng trong 30 ngy u</p>
          </div>
        </div>

        <div className="space-y-6">
          <div className="border-l-4 border-green-500 pl-6">
            <h3 className="text-xl font-semibold text-gray-900 mb-3"> Quy trnh bo hnh</h3>
            <ol className="space-y-2 text-gray-700">
              <li><strong>Bc 1:</strong> Lin h hotline 1900-1234 hoc mang sn phm n trung tm bo hnh</li>
              <li><strong>Bc 2:</strong> K thut vin kim tra v bo co tnh trng sn phm</li>
              <li><strong>Bc 3:</strong> Thc hin sa cha hoc thay th linh kin</li>
              <li><strong>Bc 4:</strong> Test v bn giao sn phm v cho khch hng</li>
            </ol>
          </div>

          <div className="border-l-4 border-blue-500 pl-6">
            <h3 className="text-xl font-semibold text-gray-900 mb-3"> Thi gian bo hnh</h3>
            <ul className="space-y-2 text-gray-700">
              <li> <strong>Laptop:</strong> 12-36 thng ty theo hng v model</li>
              <li> <strong>Ph kin:</strong> 6-12 thng bo hnh chnh hng</li>
              <li> <strong>Bo hnh m rng:</strong> C th mua thm gi bo hnh 2-3 nm</li>
            </ul>
          </div>
        </div>
      </section>

      {/* Shipping Policy Section */}
      <section id="shipping" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Truck className="w-8 h-8 mr-3 text-orange-600" />
           Vn Chuyn & Giao Hng
        </h2>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div>
            <h3 className="text-xl font-semibold text-gray-900 mb-4"> Chnh sch giao hng</h3>
            <div className="space-y-4">
              <div className="bg-orange-50 p-4 rounded-lg">
                <h4 className="font-semibold text-orange-900 mb-2">Giao hng nhanh ni thnh</h4>
                <p className="text-orange-800 text-sm">1-2 gi (H Ni, TP.HCM) - Ph 30,000</p>
              </div>
              <div className="bg-blue-50 p-4 rounded-lg">
                <h4 className="font-semibold text-blue-900 mb-2">Giao hng tiu chun</h4>
                <p className="text-blue-800 text-sm">1-3 ngy lm vic - Min ph t 3 triu</p>
              </div>
              <div className="bg-green-50 p-4 rounded-lg">
                <h4 className="font-semibold text-green-900 mb-2">Giao hng ton quc</h4>
                <p className="text-green-800 text-sm">2-5 ngy lm vic - Ph theo khu vc</p>
              </div>
            </div>
          </div>

          <div>
            <h3 className="text-xl font-semibold text-gray-900 mb-4"> Dch v c bit</h3>
            <ul className="space-y-3 text-gray-700">
              <li className="flex items-start">
                <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                <div>
                  <strong>Giao hng v setup ti nh:</strong><br />
                  <span className="text-sm text-gray-600">Ph 100,000 - Bao gm ci t phn mm c bn</span>
                </div>
              </li>
              <li className="flex items-start">
                <span className="w-2 h-2 bg-blue-500 rounded-full mr-3 mt-2"></span>
                <div>
                  <strong>Giao hng theo lch hn:</strong><br />
                  <span className="text-sm text-gray-600">t lch giao hng theo thi gian mong mun</span>
                </div>
              </li>
              <li className="flex items-start">
                <span className="w-2 h-2 bg-green-500 rounded-full mr-3 mt-2"></span>
                <div>
                  <strong>Kim tra hng trc khi thanh ton:</strong><br />
                  <span className="text-sm text-gray-600">Cho php m seal v test my trc khi nhn</span>
                </div>
              </li>
            </ul>
          </div>
        </div>
      </section>

      {/* Return Policy Section */}
      <section id="return-policy" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <RefreshCw className="w-8 h-8 mr-3 text-purple-600" />
           Chnh Sch i Tr
        </h2>

        <div className="bg-purple-50 rounded-xl p-6 mb-6">
          <div className="text-center mb-6">
            <h3 className="text-2xl font-bold text-purple-900 mb-2">
              i tr min ph trong 15 ngy
            </h3>
            <p className="text-purple-700">
              Cam kt 100% hon tin nu khng hi lng v sn phm
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center">
              <div className="text-3xl mb-3"></div>
              <h4 className="font-semibold text-purple-900 mb-2">15 ngy u</h4>
              <p className="text-purple-700 text-sm">i mi hoc hon tin 100%</p>
            </div>
            <div className="text-center">
              <div className="text-3xl mb-3"></div>
              <h4 className="font-semibold text-purple-900 mb-2">Li nh sn xut</h4>
              <p className="text-purple-700 text-sm">i mi min ph ton b chi ph</p>
            </div>
            <div className="text-center">
              <div className="text-3xl mb-3"></div>
              <h4 className="font-semibold text-purple-900 mb-2">iu kin n gin</h4>
              <p className="text-purple-700 text-sm">Gi nguyn hp, ph kin v ha n</p>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <h3 className="text-lg font-semibold text-gray-900 mb-3"> iu kin i tr</h3>
            <ul className="space-y-2 text-gray-700 text-sm">
              <li> Sn phm trong thi gian bo hnh</li>
              <li> Cn nguyn tem nim phong (nu c)</li>
              <li> y  hp, ph kin, ti liu</li>
              <li> Khng c du hiu va p, cn mp</li>
              <li> C ha n mua hng hoc phiu bo hnh</li>
            </ul>
          </div>

          <div>
            <h3 className="text-lg font-semibold text-gray-900 mb-3"> Trng hp khng i tr</h3>
            <ul className="space-y-2 text-gray-700 text-sm">
              <li> Sn phm  qua s dng lu di</li>
              <li> H hng do tc ng ngoi lc</li>
              <li> H hng do ngm nc, chy n</li>
              <li> Sn phm  can thip sa cha</li>
              <li> Qu thi hn i tr quy nh</li>
            </ul>
          </div>
        </div>
      </section>

      {/* FAQ Section */}
      <section id="faq" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <HelpCircle className="w-8 h-8 mr-3 text-indigo-600" />
           Cu Hi Thng Gp
        </h2>

        <div className="space-y-4">
          {faqItems.map((item, index) => (
            <Card key={index} className="border-l-4 border-l-indigo-500">
              <CardHeader>
                <CardTitle className="text-lg text-indigo-900">{item.question}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-gray-700 leading-6">{item.answer}</p>
              </CardContent>
            </Card>
          ))}
        </div>

        <div className="mt-8 text-center">
          <p className="text-gray-600 mb-4">Khng tm thy cu tr li bn cn?</p>
          <AppLink
            href="/contact"
            pageType="static"
            className="inline-block bg-indigo-600 hover:bg-indigo-700 text-white px-6 py-3 rounded-lg font-semibold transition-colors"
          >
            t cu hi cho chng ti
          </AppLink>
        </div>
      </section>

      {/* Service Centers with Interactive Elements */}
      <SupportCentersInteractive />

      {/* Technical Support Section */}
      <section id="technical-support" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Users className="w-8 h-8 mr-3 text-teal-600" />
           H Tr K Thut
        </h2>

        <div className="bg-teal-50 rounded-xl p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-xl font-semibold text-teal-900 mb-4"> Dch v min ph</h3>
              <ul className="space-y-3 text-teal-800">
                <li className="flex items-center">
                  <Download className="w-5 h-5 mr-3" />
                  <span>Ci t Windows & Office bn quyn</span>
                </li>
                <li className="flex items-center">
                  <Shield className="w-5 h-5 mr-3" />
                  <span>Ci t phn mm bo mt & antivirus</span>
                </li>
                <li className="flex items-center">
                  <RefreshCw className="w-5 h-5 mr-3" />
                  <span>Chuyn d liu t my c sang my mi</span>
                </li>
                <li className="flex items-center">
                  <Clock className="w-5 h-5 mr-3" />
                  <span>Ti u ha hiu nng h thng</span>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="text-xl font-semibold text-teal-900 mb-4"> Dch v cao cp</h3>
              <ul className="space-y-3 text-teal-800">
                <li className="flex items-center">
                  <span className="w-5 h-5 bg-teal-600 rounded-full mr-3 flex items-center justify-center text-white text-xs">1</span>
                  <span>Nng cp RAM, SSD - T vn min ph</span>
                </li>
                <li className="flex items-center">
                  <span className="w-5 h-5 bg-teal-600 rounded-full mr-3 flex items-center justify-center text-white text-xs">2</span>
                  <span>V sinh laptop nh k - 200,000/ln</span>
                </li>
                <li className="flex items-center">
                  <span className="w-5 h-5 bg-teal-600 rounded-full mr-3 flex items-center justify-center text-white text-xs">3</span>
                  <span>Ci t phn mm chuyn ngnh</span>
                </li>
                <li className="flex items-center">
                  <span className="w-5 h-5 bg-teal-600 rounded-full mr-3 flex items-center justify-center text-white text-xs">4</span>
                  <span>H tr k thut t xa 24/7</span>
                </li>
              </ul>
            </div>
          </div>

          <div className="text-center mt-6">
            <Button
              size="lg"
              className="bg-teal-600 hover:bg-teal-700"
            >
               t lch h tr k thut
            </Button>
          </div>
        </div>
      </section>
    </div>
  );
}