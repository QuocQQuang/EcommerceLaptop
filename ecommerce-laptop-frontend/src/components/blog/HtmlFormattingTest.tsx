'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Textarea } from '@/components/ui/textarea';
import { useState } from 'react';
import BlogContentPreview from './BlogContentPreview';

const sampleHtmlContent = `<h1>Tiu  chnh</h1>
<p>y l on vn bn u tin vi <strong>text m</strong> v <em>text nghing</em>.</p>

<h2>Tiu  ph</h2>
<p>on vn bn th hai vi <a href="#" class="text-blue-600 underline">link</a>.</p>

<h3>Danh sch</h3>
<ul>
    <li>Mc u tin</li>
    <li>Mc th hai</li>
    <li>Mc th ba</li>
</ul>

<h3>Danh sch c th t</h3>
<ol>
    <li>Bc u tin</li>
    <li>Bc th hai</li>
    <li>Bc th ba</li>
</ol>

<blockquote>
    y l mt trch dn quan trng t tc gi.
</blockquote>

<p>on vn vi <code>code inline</code> v on code:</p>

<pre><code>function hello() {
    console.log("Hello World!");
}</code></pre>

<hr>

<p>on cui vi nh:</p>
<img src="https://picsum.photos/400/300" alt="Sample image" />

<p>V mt sn phm: [product:1:MacBook Pro M3:25000000:https://picsum.photos/200/200]</p>`;

export default function HtmlFormattingTest() {
    const [htmlContent, setHtmlContent] = useState(sampleHtmlContent);

    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle>HTML Formatting Test</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div>
                        <label className="block text-sm font-medium mb-2">
                            HTML Content:
                        </label>
                        <Textarea
                            value={htmlContent}
                            onChange={(e) => setHtmlContent(e.target.value)}
                            className="min-h-[300px] font-mono text-sm"
                            placeholder="Nhp HTML content..."
                        />
                    </div>

                    <div className="flex gap-2">
                        <Button
                            onClick={() => setHtmlContent(sampleHtmlContent)}
                            variant="outline"
                            size="sm"
                        >
                            Load Sample
                        </Button>
                        <Button
                            onClick={() => setHtmlContent('')}
                            variant="outline"
                            size="sm"
                        >
                            Clear
                        </Button>
                    </div>
                </CardContent>
            </Card>

            <Card>
                <CardHeader>
                    <CardTitle>Preview</CardTitle>
                </CardHeader>
                <CardContent>
                    <BlogContentPreview content={htmlContent} />
                </CardContent>
            </Card>
        </div>
    );
}
