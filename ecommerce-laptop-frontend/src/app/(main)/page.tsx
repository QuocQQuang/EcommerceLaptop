import { CTA } from '@/components/sections/CTA';
import { Features } from '@/components/sections/Features';
import { Hero } from '@/components/sections/Hero';
import { ProductShowcase } from '@/components/sections/ProductShowcase';
import { Reviews } from '@/components/sections/Reviews';
export default function Home() {
    return (
        <>
            <Hero />
            <Features />
            <ProductShowcase />
            <Reviews />
            <CTA />
        </>
    );
}