'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Textarea } from '@/components/ui/textarea';
import dynamic from 'next/dynamic';
import { useEffect, useState } from 'react';
import 'react-quill-new/dist/quill.snow.css';

// ReactQuill editor (React 19 compatible)
const ReactQuill = dynamic(
    () => import('react-quill-new').then(mod => mod.default),
    { ssr: false }
);

// Quill editor component
const QuillEditor = (props: any) => <ReactQuill {...props} />;

const sampleHtmlContent = `<h2>Gii thiu v Laptop Gaming mi nht 2024</h2>

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

<p>Vic la chn laptop gaming ph hp s gip bn c c tri nghim gaming tt nht. Hy cn nhc k cc yu t nh hiu nng, gi c v nhu cu s dng ca bn thn.</p>`;

export default function QuillHtmlTest() {
    const [htmlContent, setHtmlContent] = useState('');
    const [quillContent, setQuillContent] = useState('');
    const [quillInstance, setQuillInstance] = useState<any>(null);

    const quillModules = {
        toolbar: [
            [{ 'header': [1, 2, 3, 4, 5, 6, false] }],
            ['bold', 'italic', 'underline', 'strike'],
            [{ 'color': [] }, { 'background': [] }],
            [{ 'list': 'ordered' }, { 'list': 'bullet' }],
            [{ 'indent': '-1' }, { 'indent': '+1' }],
            [{ 'align': [] }],
            ['blockquote', 'code-block'],
            ['link', 'image', 'video'],
            ['clean']
        ],
        clipboard: {
            matchVisual: true, // Changed to true to allow HTML paste
        }
    };

    const quillFormats = [
        'header', 'bold', 'italic', 'underline', 'strike',
        'color', 'background', 'list', 'indent',
        'align', 'blockquote', 'code-block', 'link', 'image', 'video'
    ];

    const handlePasteHtml = () => {
        if (quillInstance) {
            quillInstance.clipboard.dangerouslyPasteHTML(sampleHtmlContent);
        }
    };

    const handlePasteCustomHtml = () => {
        if (quillInstance && htmlContent) {
            quillInstance.clipboard.dangerouslyPasteHTML(htmlContent);
        }
    };

    const handleQuillChange = (value: string) => {
        setQuillContent(value);
    };

    // Get Quill instance when component mounts
    useEffect(() => {
        const timer = setTimeout(() => {
            const quillElement = document.querySelector('.ql-editor');
            if (quillElement) {
                const quill = (quillElement as any).__quill;
                if (quill) {
                    setQuillInstance(quill);
                }
            }
        }, 1000);
        return () => clearTimeout(timer);
    }, [quillContent]);

    const getQuillHtml = () => {
        return quillContent;
    };

    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle>Quill HTML Paste Test</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium mb-2">
                            HTML Content to Paste:
                        </label>
                        <Textarea
                            value={htmlContent}
                            onChange={(e) => setHtmlContent(e.target.value)}
                            className="min-h-[200px] font-mono text-sm"
                            placeholder="Nhp HTML content  paste vo Quill..."
                        />
                    </div>

                    <div className="flex gap-2">
                        <Button
                            onClick={() => setHtmlContent(sampleHtmlContent)}
                            variant="outline"
                            size="sm"
                        >
                            Load Sample HTML
                        </Button>
                        <Button
                            onClick={handlePasteCustomHtml}
                            variant="default"
                            size="sm"
                            disabled={!htmlContent}
                        >
                            Paste Custom HTML
                        </Button>
                        <Button
                            onClick={handlePasteHtml}
                            variant="default"
                            size="sm"
                        >
                            Paste Sample HTML
                        </Button>
                        <Button
                            onClick={() => setQuillContent('')}
                            variant="outline"
                            size="sm"
                        >
                            Clear Quill
                        </Button>
                    </div>
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Quill Editor</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="border rounded-lg">
                        <QuillEditor
                            value={quillContent}
                            onChange={handleQuillChange}
                            modules={quillModules}
                            formats={quillFormats}
                            placeholder="Quill editor s hin th  y..."
                            className="min-h-[300px]"
                        />
                    </div>
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Quill HTML Output</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="bg-gray-100 p-4 rounded-lg">
                        <pre className="text-xs overflow-x-auto whitespace-pre-wrap">
                            {getQuillHtml()}
                        </pre>
                    </div>
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Debug Info</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="text-sm space-y-2">
                        <p><strong>Quill Content Length:</strong> {quillContent.length}</p>
                        <p><strong>Has Product Links:</strong> {quillContent.includes('[product:') ? 'Yes' : 'No'}</p>
                        <p><strong>Has HTML Tags:</strong> {quillContent.includes('<') ? 'Yes' : 'No'}</p>
                        <p><strong>Clipboard Config:</strong> matchVisual: true</p>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
