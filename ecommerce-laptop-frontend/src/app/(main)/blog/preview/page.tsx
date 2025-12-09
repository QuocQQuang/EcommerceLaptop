'use client';

import BlogContentPreview from '@/components/blog/BlogContentPreview';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useState } from 'react';

export default function BlogPreviewPage() {
    const [content, setContent] = useState(`
        <h2>Gii thiu v Laptop Gaming mi nht 2025</h2>
        
        <p>Trong th gii cng ngh hin i, laptop gaming  tr thnh mt cng c khng th thiu cho cc game th chuyn nghip v nhng ngi yu thch tri nghim gaming cht lng cao.</p>
        
        <p>Vi s pht trin khng ngng ca cng ngh, cc nh sn xut  cho ra i nhng mu laptop gaming vi hiu nng mnh m, thit k p mt v tnh nng hin i.</p>
        
        <h3>Nhng tnh nng ni bt</h3>
        
        <ul>
            <li>Card  ha ri mnh m</li>
            <li>B x l a nhn hiu nng cao</li>
            <li>Mn hnh c tn s qut cao</li>
            <li>H thng tn nhit tin tin</li>
            <li>Bn phm c chuyn dng</li>
        </ul>
        
        <p>Di y l mt s sn phm laptop gaming c nh gi cao nht hin ti:</p>
        
        [product:1:Laptop Gaming ASUS ROG Strix G15:25000000:https://picsum.photos/400/300?random=1]
        
        <p>ASUS ROG Strix G15 l mt trong nhng laptop gaming c yu thch nht vi hiu nng mnh m v thit k n tng.</p>
        
        <p>Vi card  ha RTX 4060 v b x l AMD Ryzen 7, chic laptop ny c th chy mt m cc ta game AAA mi nht.</p>
        
        [product:2:Laptop Gaming MSI Katana GF66:22000000:https://picsum.photos/400/300?random=2]
        
        <p>MSI Katana GF66 mang n tri nghim gaming tuyt vi vi gi c hp l, ph hp vi nhiu i tng ngi dng.</p>
        
        <h3>Kt lun</h3>
        
        <p>Vic la chn laptop gaming ph hp s gip bn c c tri nghim gaming tt nht. Hy cn nhc k cc yu t nh hiu nng, gi c v nhu cu s dng ca bn thn.</p>
    `);

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="container mx-auto px-4 py-8">
                <div className="max-w-4xl mx-auto">
                    <Card>
                        <CardHeader>
                            <CardTitle>Blog Content Preview</CardTitle>
                            <CardDescription>
                                Xem trc cch ni dung blog s hin th vi nh v product links
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="mb-6">
                                <label className="block text-sm font-medium text-gray-700 mb-2">
                                    Ni dung blog (HTML):
                                </label>
                                <textarea
                                    value={content}
                                    onChange={(e) => setContent(e.target.value)}
                                    className="w-full h-40 p-3 border border-gray-300 rounded-md font-mono text-sm"
                                    placeholder="Nhp ni dung HTML ca blog..."
                                />
                            </div>

                            <div className="border-t pt-6">
                                <h3 className="text-lg font-semibold mb-4">Preview:</h3>
                                <BlogContentPreview content={content} />
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}
