import bundleAnalyzer from '@next/bundle-analyzer';

const withBundleAnalyzer = bundleAnalyzer({
    enabled: process.env.ANALYZE === 'true',
});

/** @type {import('next').NextConfig} */
const nextConfig = {
    // Disable Hot Module Replacement (Fast Reload)
    reactStrictMode: true,

    images: {
        // Explicit domain allow list (simpler than only remotePatterns for many common CDNs)
        // NOTE: If backend returns additional hosts (Cloudinary, S3, etc.), add them here.
        domains: [
            'i.imgur.com',
            'images.unsplash.com',
            'source.unsplash.com',
            'i.ibb.co',
            'qr.sepay.vn',
            'localhost',
            'plus.unsplash.com'// dev API hosted images if served directly
        ],
        // remotePatterns give finer control (port/path); keep localhost + any special patterns
        remotePatterns: [
            {
                protocol: 'http',
                hostname: 'localhost',
                port: '5129',
                pathname: '/**',
            },
            {
                protocol: 'https',
                hostname: 'qr.sepay.vn',
                pathname: '/**',
            },
            // (Optional) example pattern stub to copy when adding new CDN with path scoping
            // {
            //     protocol: 'https',
            //     hostname: 'cdn.example.com',
            //     pathname: '/products/**',
            // },
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

    // Webpack optimization
    webpack: (config, { isServer }) => {
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

        return config
    },

    // Disable ESLint during builds for faster compilation
    eslint: {
        ignoreDuringBuilds: true,
    },

    // Disable TypeScript checking during builds for faster compilation
    typescript: {
        ignoreBuildErrors: true,
    },

    // Basic build configuration
    trailingSlash: false,
    generateBuildId: async () => {
        return 'dynamic-build'
    },
};

export default withBundleAnalyzer(nextConfig);