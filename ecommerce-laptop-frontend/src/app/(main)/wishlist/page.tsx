import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import WishlistView from '@/components/organisms/WishlistView';

export default function WishlistPage() {
    const breadcrumbs = [
        { label: 'Trang ch', href: '/' },
        { label: 'Danh sách yêu thích' }
    ];

    return (
        <StaticPageLayout
            title="Wishlist"
            subtitle="Lưu những mẫu laptop bạn quan tâm để xem sau hoặc mua sau"
            tableOfContents={[]}
            breadcrumbs={breadcrumbs}
        >
            <WishlistView />
        </StaticPageLayout>
    );
}
