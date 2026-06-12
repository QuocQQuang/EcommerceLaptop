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
            subtitle="So sánh các mẫu laptop để chọn ra chiếc phù hợp nhất"
            tableOfContents={[]}
            breadcrumbs={breadcrumbs}
        >
            <ComparisonTable />
        </StaticPageLayout>
    );
}
