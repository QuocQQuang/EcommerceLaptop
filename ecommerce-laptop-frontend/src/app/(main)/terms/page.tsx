import { Separator } from '@/components/ui/separator';
import { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'iu khon dch v - Laptop Store',
    description: 'iu khon v iu kin s dng dch v ca Laptop Store',
};

export default function TermsPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <h1 className="text-3xl font-bold mb-6">iu Khon Dch V</h1>
            <p className="text-lg text-gray-700 mb-8">
                Cho mng bn n vi Laptop Store. Vic s dng website v dch v ca chng ti c ngha l bn ng  vi cc iu khon sau. Vui lng c k trc khi s dng. Cc iu khon ny c hiu lc t ngy 25/09/2025.
            </p>

            <Separator className="my-8" />

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">1. iu Khon Chung</h2>
                <p className="text-gray-700 mb-4">
                    Laptop Store l nn tng thng mi in t chuyn cung cp sn phm laptop v ph kin chnh hng. Chng ti cam kt tun th php lut Vit Nam v bo v quyn li ngi tiu dng theo Lut Bo v quyn li ngi tiu dng 2010 (sa i 2023).
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>Tui ti thiu  s dng dch v: 18 tui hoc c s gim h ca ph huynh.</li>
                    <li>Chng ti c quyn t chi hoc hy n hng nu pht hin vi phm.</li>
                    <li>Thng tin trn website ch mang tnh cht tham kho, khng thay th t vn chuyn nghip.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">2. t Hng V Thanh Ton</h2>
                <p className="text-gray-700 mb-4">
                    Khi t hng, bn cam kt cung cp thng tin chnh xc. Laptop Store c quyn hy n nu thng tin sai lch hoc khng th xc minh.
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>Gi sn phm hin th l gi cui cng, c th thay i ty chng trnh khuyn mi.</li>
                    <li>Chng ti khng chu trch nhim v sai st in n hoc hin th gi.</li>
                    <li>Thanh ton qua cc cng uy tn (VNPAY, Momo, PayPal, v.v.). Thng tin thanh ton c bo mt theo tiu chun PCI DSS.</li>
                    <li>n hng ch c xc nhn sau khi nhn thanh ton thnh cng.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">3. Giao Hng V Nhn Hng</h2>
                <p className="text-gray-700 mb-4">
                    Laptop Store giao hng ton quc qua cc n v vn chuyn uy tn (GHN, GHTK, Viettel Post).
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>Thi gian giao hng: 2-5 ngy lm vic ty khu vc.</li>
                    <li>Khch hng chu trch nhim kim tra hng khi nhn. Khi k nhn, n hng c coi l hon tt.</li>
                    <li>Chng ti khng chu trch nhim mt mt hoc h hng trong qu trnh vn chuyn (bo him ty chn).</li>
                    <li>Ph giao hng c thng bo trc khi xc nhn n.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">4. i Tr V Hon Tin</h2>
                <p className="text-gray-700 mb-4">
                    Chng ti p dng chnh sch i tr theo quy nh php lut (Lut Bo v quyn li ngi tiu dng).
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>i tr trong 30 ngy nu sn phm li hoc khng ng m t (gi nguyn tem nim phong).</li>
                    <li>Hon tin 100% nu li nh sn xut, khng hon nu s dng sai.</li>
                    <li>Trng hp ht hng, chng ti hon tin hoc  xut sn phm thay th.</li>
                    <li>Lin h h tr  bt u quy trnh i tr.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">5. Bo Hnh V H tr</h2>
                <p className="text-gray-700 mb-4">
                    Tt c sn phm c bo hnh chnh hng t nh sn xut (1-3 nm ty model).
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>Bo hnh phn mm v phn cng theo chnh sch nh sn xut.</li>
                    <li>H tr k thut min ph trong thi gian bo hnh.</li>
                    <li>Khng bo hnh nu sa cha bn ngoi hoc s dng sai quy nh.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">6. Bo Mt Thng Tin</h2>
                <p className="text-gray-700">
                    Xem chi tit ti <a href="/privacy" className="text-blue-600 hover:underline">Chnh sch bo mt</a>. Chng ti cam kt bo v d liu c nhn theo GDPR v lut Vit Nam.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">7. Trch Nhim Php L</h2>
                <p className="text-gray-700">
                    Laptop Store khng chu trch nhim cho ni dung ngi dng to (reviews, comments). Chng ti c quyn xa ni dung vi phm. Tranh chp c gii quyt theo php lut Vit Nam.
                </p>
            </section>

            <p className="text-sm text-gray-500 mt-8">
                iu khon ny c th c cp nht m khng thng bo trc. Ngy cp nht cui: 25/09/2025.
            </p>
        </div>
    );
}