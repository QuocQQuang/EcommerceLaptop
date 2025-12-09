'use client';

import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import ComparisonTable from '@/components/organisms/ComparisonTable';

export default function ComparePage() {
    const breadcrumbs = [
        { label: 'Trang ch', href: '/' },
        { label: 'So snh' }
    ];

    return (
        <StaticPageLayout
            title="Compare Laptops"
            subtitle="So snh cc mu laptop  chn ra chic ph hp nht"
            tableOfContents={[]}
            breadcrumbs={breadcrumbs}
        >
            <ComparisonTable />
        </StaticPageLayout>
    );
}
