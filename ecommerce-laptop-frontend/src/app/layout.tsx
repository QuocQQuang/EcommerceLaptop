import DebugLogger from "@/components/DebugLogger";
import CompareBar from '@/components/organisms/CompareBar';
import { Toaster } from "@/components/ui/sonner";
import type { Metadata, Viewport } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { Providers } from "./providers";

const inter = Inter({
  subsets: ["latin"],
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: "LaptopStore - Cửa hàng laptop và phụ kiện công nghệ",
  description: "Cửa hàng laptop và phụ kiện công nghệ hàng đầu Việt Nam. Chúng tôi cam kết mang đến những sản phẩm chất lượng cao với giá cả hợp lý.",
  keywords: ["laptop", "computer", "technology", "accessories", "gaming laptop", "business laptop"],
  authors: [{ name: "LaptopStore" }],
  creator: "LaptopStore",
  publisher: "LaptopStore",
  formatDetection: {
    email: false,
    address: false,
    telephone: false,
  },
  metadataBase: new URL(process.env.NEXTAUTH_URL || 'http://localhost:3000'),
  manifest: '/manifest.json',
  appleWebApp: {
    capable: true,
    statusBarStyle: 'default',
    title: 'LaptopStore',
  },
  applicationName: 'LaptopStore',
  openGraph: {
    type: "website",
    locale: "vi_VN",
    url: "/",
    siteName: "LaptopStore",
    title: "LaptopStore - Cửa hàng laptop và phụ kiện công nghệ",
    description: "Cửa hàng laptop và phụ kiện công nghệ hàng đầu Việt Nam",
    images: [
      {
        url: "/images/og-image.jpg",
        width: 1200,
        height: 630,
        alt: "LaptopStore",
      },
    ],
  },
  twitter: {
    card: "summary_large_image",
    title: "LaptopStore - Cửa hàng laptop và phụ kiện công nghệ",
    description: "Cửa hàng laptop và phụ kiện công nghệ hàng đầu Việt Nam",
    images: ["/images/og-image.jpg"],
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-video-preview': -1,
      'max-image-preview': 'large',
      'max-snippet': -1,
    },
  },
  verification: {
    google: "google-verification-code",
  },
};

export const viewport: Viewport = {
  width: 'device-width',
  initialScale: 1,
  minimumScale: 1,
  viewportFit: 'cover',
  themeColor: '#2563eb',
  colorScheme: 'light',
};

import { CartSyncProvider } from "@/components/providers/CartSyncProvider";


export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi" suppressHydrationWarning>
      <body
        className={`${inter.variable} font-sans antialiased`}
        suppressHydrationWarning
      >
        <Providers>
          <CartSyncProvider>
            {children}
          </CartSyncProvider>
          <CompareBar />

          <Toaster />
          {/* Debug logger: enable in development or when NEXT_PUBLIC_DEBUG === 'true'.
        For production, set NEXT_PUBLIC_DEBUG=true in Vercel Environment Variables
        (do NOT store secrets here). */}
          {(process.env.NODE_ENV === 'development' || process.env.NEXT_PUBLIC_DEBUG === 'true') && <DebugLogger />}
        </Providers>
      </body>
    </html>
  );
}
