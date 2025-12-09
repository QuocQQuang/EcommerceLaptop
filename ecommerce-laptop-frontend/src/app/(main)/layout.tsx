import { CartSidebar } from '@/components/organisms/CartSidebar';
import { FloatingCartButton } from '@/components/organisms/FloatingCartButton';
import { Footer } from '@/components/organisms/Footer';
import { Header } from '@/components/organisms/Header';
import { PWAInstaller } from '@/components/organisms/PWAInstaller';
import { ReactNode } from 'react';

interface MainLayoutProps {
    children: ReactNode;
}

export default function MainLayout({ children }: MainLayoutProps) {
    return (
        <div className="min-h-screen flex flex-col">
            <Header />
            <main className="flex-1">
                {children}
            </main>
            <Footer />
            <CartSidebar />
            <FloatingCartButton />
            <PWAInstaller />
        </div>
    );
}