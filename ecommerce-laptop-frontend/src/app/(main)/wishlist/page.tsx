import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import WishlistView from '@/components/organisms/WishlistView';

export default function WishlistPage() {
    const breadcrumbs = [
        { label: 'Trang ch', href: '/' },
        { label: 'Danh sch yu thch' }
    ];

    return (
        <StaticPageLayout
            title="Wishlist"
            subtitle="Lu nhng mu laptop bn quan tm  xem sau hoc mua sau"
            tableOfContents={[]}
            breadcrumbs={breadcrumbs}
        >
            <WishlistView />
        </StaticPageLayout>
    );
}
