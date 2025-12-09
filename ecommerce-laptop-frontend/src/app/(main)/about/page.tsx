import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Gii thiu - Laptop Store',
    description: 'Tm hiu v Laptop Store - ca hng laptop chnh hng uy tn nht Vit Nam',
};

export default function AboutPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <h1 className="text-3xl font-bold mb-6">Gii Thiu V Laptop Store</h1>
            <p className="text-lg text-gray-700 mb-8">
                Laptop Store l ca hng chuyn cung cp laptop chnh hng vi hn 10 nm kinh nghim trong lnh vc cng ngh. Chng ti cam kt mang n cho khch hng nhng sn phm cht lng cao t cc thng hiu hng u th gii nh Dell, HP, Lenovo, Asus, MacBook v nhiu hn na.
            </p>

            <Separator className="my-8" />

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">S Mnh Ca Chng Ti</h2>
                <p className="text-gray-700">
                    Chng ti khng ch bn laptop m cn ng hnh cng khch hng trong vic la chn thit b ph hp vi nhu cu cng vic, hc tp v gii tr. Vi i ng chuyn vin t vn giu kinh nghim, Laptop Store lun sn sng h tr bn 24/7.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">Lch S Hnh Thnh</h2>
                <p className="text-gray-700">
                    Thnh lp t nm 2015, Laptop Store bt u nh mt ca hng nh chuyn cung cp laptop cho sinh vin v nhn vin vn phng. Vi s pht trin khng ngng v cam kt cht lng, chng ti  m rng quy m v tr thnh mt trong nhng nh phn phi laptop ln nht ti Vit Nam.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">Gi Tr Ct Li</h2>
                <div className="grid md:grid-cols-2 gap-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Cht Lng u Tin</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p>Tt c sn phm u c kim tra nghim ngt trc khi n tay khch hng. Chng ti ch bn hng chnh hng vi y  giy t chng nhn.</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>Phc V Tn Tm</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p>H tr khch hng t t vn n hu mi vi thi  chuyn nghip. i ng ca chng ti lun sn sng gii p mi thc mc.</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>Gi C Cnh Tranh</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p>Cam kt gi tt nht th trng vi chnh sch bo hnh r rng v nhiu chng trnh khuyn mi hp dn.</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>i Tr Linh Hot</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p>Chnh sch i tr trong 30 ngy vi iu kin n gin. Chng ti lun t s hi lng ca khch hng ln hng u.</p>
                        </CardContent>
                    </Card>
                </div>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">Lin H Vi Chng Ti</h2>
                <div className="grid md:grid-cols-3 gap-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Hotline</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p className="text-lg font-bold">1800-123-456</p>
                            <p className="text-sm text-gray-600">T vn 24/7</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>Email</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p className="text-lg">support@laptopstore.vn</p>
                            <p className="text-sm text-gray-600">Phn hi trong 24h</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>a Ch</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p className="text-lg">123 ng Laptop, TP.HCM</p>
                            <p className="text-sm text-gray-600">M ca 8:00 - 22:00</p>
                        </CardContent>
                    </Card>
                </div>
            </section>
        </div>
    );
}