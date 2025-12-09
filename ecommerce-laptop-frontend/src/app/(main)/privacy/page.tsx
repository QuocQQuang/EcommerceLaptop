import { Separator } from '@/components/ui/separator';
import { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Chnh sch bo mt - Laptop Store',
    description: 'Chnh sch bo v thng tin c nhn ca ngi dng trn Laptop Store',
};

export default function PrivacyPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <h1 className="text-3xl font-bold mb-6">Chnh Sch Bo Mt</h1>
            <p className="text-lg text-gray-700 mb-8">
                Ti Laptop Store, chng ti coi trng quyn ring t ca bn. Chnh sch ny gii thch cch chng ti thu thp, s dng v bo v thng tin c nhn ca bn khi s dng website v dch v. Chnh sch c hiu lc t ngy 25/09/2025 v tun th Lut An ninh mng 2018 v Lut Bo v d liu c nhn 2023 ca Vit Nam.
            </p>

            <Separator className="my-8" />

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">1. Thng Tin Chng Ti Thu Thp</h2>
                <p className="text-gray-700 mb-4">
                    Chng ti thu thp thng tin cn thit  cung cp dch v, bao gm:
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>Tn, email, s in thoi khi ng k ti khon hoc t hng.</li>
                    <li>a ch giao hng, thng tin thanh ton (khng lu th tn dng).</li>
                    <li>Thng tin duyt web (IP, thit b, trnh duyt)  ci thin dch v v ngn chn gian ln.</li>
                    <li>D liu s dng (sn phm xem, tm kim)  c nhn ha khuyn ngh.</li>
                </ul>
                <p className="text-gray-700">
                    Chng ti khng thu thp d liu nhy cm khng cn thit. D liu c m ha truyn ti (HTTPS).
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">2. Cch Chng Ti S Dng Thng Tin</h2>
                <p className="text-gray-700 mb-4">
                    Thng tin c s dng :
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>X l n hng, giao hng, thanh ton.</li>
                    <li>Gi email xc nhn, khuyn mi (c th hy ng k).</li>
                    <li>Ci thin website (phn tch n danh).</li>
                    <li>Ngn chn lm dng (rate limiting, IP block nu vi phm).</li>
                </ul>
                <p className="text-gray-700">
                    Chng ti khng bn hoc chia s d liu vi bn th ba ngoi i tc vn chuyn/thanh ton (vi s ng ).
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">3. Chia S Thng Tin</h2>
                <p className="text-gray-700 mb-4">
                    Chng ti ch chia s vi:
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>i tc giao hng (a ch, s in thoi).</li>
                    <li>Cng thanh ton (thng tin giao dch, khng lu th).</li>
                    <li>C quan php lut nu yu cu (gian ln, lm dng).</li>
                </ul>
                <p className="text-gray-700">
                    Khng chia s vi bn th ba qung co. Cookie ch dng cho phin ng nhp/gi hng (c th xa).
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">4. Bo V D Liu</h2>
                <p className="text-gray-700 mb-4">
                    Chng ti s dng:
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>M ha d liu (AES-256 cho mt khu).</li>
                    <li>HTTPS cho tt c truyn ti.</li>
                    <li>Firewall, rate limiting, IP blocking chng tn cng.</li>
                    <li>Backup m ha, lu tr an ton.</li>
                </ul>
                <p className="text-gray-700">
                    Trong trng hp vi phm d liu, chng ti thng bo trong 72 gi theo lut.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">5. Quyn Ca Bn</h2>
                <p className="text-gray-700">
                    Bn c quyn:
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>Xem/xa/cp nht d liu c nhn (ti khon).</li>
                    <li>Hy ng k email marketing.</li>
                    <li>Ti d liu (email support@laptopstore.vn).</li>
                    <li>Khiu ni nu vi phm (lin h hotline).</li>
                </ul>
                <p className="text-gray-700">
                    D liu lu 5 nm sau ti khon khng hot ng, xa theo yu cu.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">6. Cookie V Theo Di</h2>
                <p className="text-gray-700">
                    Chng ti s dng cookie cho phin ng nhp, gi hng (essential). Cookie phn tch n danh (Google Analytics). Bn c th xa cookie qua trnh duyt settings.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">7. Thay i Chnh Sch</h2>
                <p className="text-gray-700">
                    Chng ti c th cp nht chnh sch ny. Thay i s c ng ti trn website v thng bo qua email nu nh hng ln. Tip tc s dng sau cp nht = ng .
                </p>
            </section>

            <p className="text-sm text-gray-500 mt-8">
                C cu hi? Lin h <a href="/contact" className="text-blue-600 hover:underline">support@laptopstore.vn</a> hoc hotline 1800-123-456.
            </p>
        </div>
    );
}