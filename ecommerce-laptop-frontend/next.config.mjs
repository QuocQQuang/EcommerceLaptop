// Conditionally load bundle analyzer (only available in dev)
const withBundleAnalyzer = process.env.ANALYZE === 'true'
    ? (await import('@next/bundle-analyzer').then(m => m.default).catch(() => null))?.({ enabled: true })
    : null;

const applyBundleAnalyzer = withBundleAnalyzer || ((config) => config);

/** @type {import('next').NextConfig} */
const nextConfig = {
    // Disable Hot Module Replacement (Fast Reload)
    reactStrictMode: true,

    images: {
        // Explicit domain allow list (simpler than only remotePatterns for many common CDNs)
        // NOTE: If backend returns additional hosts (Cloudinary, S3, etc.), add them here.
        // remotePatterns give finer control and replacement for deprecated domains
        remotePatterns: [
            {
                protocol: 'http',
                hostname: 'localhost',
            },
            {
                protocol: 'https',
                hostname: 'i.imgur.com',
            },
            {
                protocol: 'https',
                hostname: 'images.unsplash.com',
            },
            {
                protocol: 'https',
                hostname: 'source.unsplash.com',
            },
            {
                protocol: 'https',
                hostname: 'plus.unsplash.com',
            },
            {
                protocol: 'https',
                hostname: 'i.ibb.co',
            },
            {
                protocol: 'https',
                hostname: 'qr.sepay.vn',
            },
        ],
        formats: ['image/webp', 'image/avif'],
        deviceSizes: [640, 750, 828, 1080, 1200, 1920, 2048, 3840],
        imageSizes: [16, 32, 48, 64, 96, 128, 256, 384],
        dangerouslyAllowSVG: true, // Many external sources (e.g. QR, logos) may be SVG
        contentDispositionType: 'inline',
    },

    // PWA Configuration
    experimental: {
        webpackBuildWorker: true,
        optimizePackageImports: [
            '@radix-ui/react-icons',
            'lucide-react',
        ],
    },

    // Webpack configuration
    webpack: (config, { dev, isServer }) => {
        // Disable HMR based on environment variable
        if (dev && !isServer && process.env.DISABLE_HMR === 'true') {
            // Completely disable file watching and HMR
            config.watchOptions = {
                ignored: ['**/*'],
            };

            // Disable HMR plugins
            config.plugins = config.plugins.filter(plugin =>
                plugin.constructor.name !== 'HotModuleReplacementPlugin'
            );
        }

        // Fallback for client-side
        if (!isServer) {
            config.resolve.fallback = {
                ...config.resolve.fallback,
                fs: false,
                net: false,
                tls: false,
            }
        }

        // Bundle splitting optimization
        config.optimization = {
            ...config.optimization,
            splitChunks: {
                chunks: 'all',
                cacheGroups: {
                    vendor: {
                        test: /[\\/]node_modules[\\/]/,
                        name: 'vendors',
                        chunks: 'all',
                    },
                },
            },
        }

        return config;
    },

    // Performance optimizations
    compress: true,

    // Security headers
    async headers() {
        return [
            {
                source: '/(.*)',
                headers: [
                    {
                        key: 'X-Frame-Options',
                        value: 'DENY',
                    },
                    {
                        key: 'X-Content-Type-Options',
                        value: 'nosniff',
                    },
                    {
                        key: 'Referrer-Policy',
                        value: 'origin-when-cross-origin',
                    },
                    {
                        key: 'Permissions-Policy',
                        value: 'camera=(), microphone=(), geolocation=()',
                    },
                ],
            },
        ]
    },





    // Basic build configuration
    trailingSlash: false,
    generateBuildId: async () => {
        return 'dynamic-build'
    },
};

export default applyBundleAnalyzer(nextConfig);