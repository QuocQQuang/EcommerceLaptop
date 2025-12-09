import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  experimental: {
    // Reduce HMR aggressiveness
    turbo: {
      resolveExtensions: [
        '.mdx',
        '.tsx',
        '.ts',
        '.jsx',
        '.js',
        '.mjs',
        '.json',
      ],
    },
  },
  // Optimize for development
  webpack: (config, { dev, isServer }) => {
    if (dev) {
      // Reduce watch options to prevent excessive rebuilds
      config.watchOptions = {
        ...config.watchOptions,
        poll: false,
        aggregateTimeout: 300,
        ignored: [
          /node_modules/,
          /.next/,
          /.git/,
          /.turbo/,
          /\.log$/,
        ],
      };
    }
    return config;
  },
};

export default nextConfig;
