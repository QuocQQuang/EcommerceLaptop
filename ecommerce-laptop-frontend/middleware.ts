import { createSecurityMiddleware, SecurityConfig } from '@/lib/security-middleware';
import { getToken } from 'next-auth/jwt';
import { NextRequest, NextResponse } from 'next/server';

// List of paths that require authentication
const protectedPaths = [
    '/admin',
    '/admin/dashboard',
    '/admin/products',
    '/admin/orders',
    '/admin/users',
    '/admin/settings',
    '/admin/promotions',
    '/admin/reports',
    '/admin/content',
    '/admin/security',
    '/admin/roles'
]

// List of customer paths that require authentication  
// NOTE: `/account` and `/account/orders` were causing redirect loops in some
// deployments when middleware performed server-side redirects. Treat these
// two paths as client-guarded (no server-side redirect) and handle auth on the
// client to avoid a hard redirect loop. Keep other account paths protected.
const customerProtectedPaths = [
    // '/account', // removed from server-side enforced list intentionally
    '/account/profile',
    // '/account/orders', // removed from server-side enforced list intentionally
    '/account/addresses',
    '/account/wishlist',
    '/account/reviews',
    '/profile',
    '/dashboard'
]

// List of public paths (no authentication required)
const publicPaths = [
    '/admin-login',
    '/api/auth',
    '/api/public',
    '/',
    '/auth',
    '/auth/login',
    '/register',
    '/products',
    '/about',
    '/contact',
    '/unauthorized'
]

// Security configuration - disable global rate limit since backend handles it, keep for admin/API
const securityConfig: Partial<SecurityConfig> = {
    enableCSRF: true,
    enableRateLimit: false, // Disabled globally; enabled conditionally for sensitive routes
    enableSessionSecurity: false, // Disabled globally to rely on path-specific NextAuth validation; enable conditionally if needed
    rateLimitConfig: {
        windowMs: 15 * 60 * 1000, // 15 minutes
        maxRequests: 100, // Strict for admin/API
        keyGenerator: (req) => {
            // Strict for admin/API routes
            if (req.nextUrl.pathname.startsWith('/admin') || req.nextUrl.pathname.startsWith('/api/admin')) {
                return `admin:${getClientIP(req)}`;
            }
            return getClientIP(req);
        },
    },
    trustedOrigins: [
        'http://localhost:3000',
        'https://localhost:3000',
        'http://localhost:5129',
        'https://localhost:7008',
        'https://ecommerce-laptop-quocquang.vercel.app',
        'https://a33lprojecct.id.vn',
        process.env.NEXT_PUBLIC_APP_URL || '',
    ],
    secureHeaders: true,
};

// Helper function to get client IP
function getClientIP(req: NextRequest): string {
    const forwarded = req.headers.get('x-forwarded-for');
    const realIP = req.headers.get('x-real-ip');
    const clientIP = req.headers.get('x-client-ip');

    if (forwarded) {
        return forwarded.split(',')[0].trim();
    }

    if (realIP) {
        return realIP;
    }

    if (clientIP) {
        return clientIP;
    }

    // Extract IP from request URL or connection info
    const url = new URL(req.url);
    return url.hostname || 'unknown';
}

// Create security middleware instance
const securityMiddleware = createSecurityMiddleware(securityConfig);

export async function middleware(request: NextRequest) {
    const { pathname } = request.nextUrl

    // Skip middleware for static files and specific API routes
    if (
        pathname.startsWith('/_next/') ||
        pathname.startsWith('/api/health') ||
        pathname.startsWith('/api/csp-report') ||
        pathname.match(/\.(ico|png|jpg|jpeg|gif|svg|css|js|woff|woff2|ttf|eot)$/)
    ) {
        return NextResponse.next()
    }

    // NOTE: Do not early-return for public paths here; we must first evaluate
    // session tokens to properly redirect authenticated users away from auth pages.

    // Apply security middleware first - but for root GET, use relaxed config
    let securityResponse;
    if (request.nextUrl.pathname === '/' && request.method === 'GET') {
        const publicConfig = {
            ...securityConfig, rateLimitConfig: {
                ...(securityConfig.rateLimitConfig || {}),
                windowMs: securityConfig.rateLimitConfig?.windowMs || 15 * 60 * 1000,
                maxRequests: 1000
            }
        };
        const publicMiddleware = createSecurityMiddleware(publicConfig);
        securityResponse = await publicMiddleware(request);
    } else {
        securityResponse = await securityMiddleware(request);
    }
    if (securityResponse.status !== 200) {
        return securityResponse;
    }

    // Check if current path is admin protected
    const isAdminProtectedPath = protectedPaths.some(path =>
        pathname.startsWith(path) && pathname !== '/admin-login'
    )

    // Check if current path is customer protected
    // Note: exact `/account` and `/account/orders` are intentionally excluded
    // from server-side redirect enforcement (they are client-guarded).
    const isCustomerProtectedPath = customerProtectedPaths.some(path =>
        pathname.startsWith(path)
    )

    const isAccountRootOrOrders = pathname === '/account' || pathname === '/account/orders';

    // Check if current path is public
    const isPublicPath = publicPaths.some(path =>
        pathname === path || pathname.startsWith(path)
    )

    // Handle redirects for already authenticated users on login pages - with validation to prevent loops
    // 
    // DUAL-AUTH ARCHITECTURE:
    // - Customer Auth: Uses NextAuth's getToken() for session validation
    // - Admin Auth: Uses dedicated 'admin-session' JWT cookie (separate auth system)
    // 
    // This separation is intentional:
    // - Admin panel has its own login flow (/admin-login)
    // - Customer uses NextAuth OAuth/credentials (/auth/login)
    // - They do not share sessions to prevent privilege escalation

    // --- CUSTOMER SESSION VALIDATION (NextAuth) ---
    let isValidCustomerSession = false;
    try {
        // Use NextAuth's getToken - handles cookie name variants automatically in v4+
        const token = await getToken({ req: request, secret: process.env.NEXTAUTH_SECRET });

        if (token) {
            // token.exp (if present) is in seconds since epoch per JWT standard
            const exp = (token as { exp?: number }).exp;
            if (typeof exp === 'number') {
                isValidCustomerSession = exp * 1000 > Date.now();
            } else {
                // If there's no exp claim, assume token is valid (rare)
                isValidCustomerSession = true;
            }
        }
    } catch (err) {
        // Treat any error as unauthenticated; middleware should continue to enforce login
        if (process.env.NEXT_PUBLIC_LOG_LEVEL === 'debug' || process.env.NODE_ENV !== 'production') {
            console.error(' middleware: getToken error:', String(err));
        }
        isValidCustomerSession = false;
    }

    // --- ADMIN SESSION VALIDATION (Dedicated JWT) ---
    const adminSessionToken = request.cookies.get('admin-session')?.value;

    // Helper for admin token validation (JWT format with expiry check)
    function isValidAdminToken(token: string | undefined): boolean {
        if (!token) return false;
        try {
            const parts = token.split('.');
            if (parts.length !== 3) return false; // JWT should have 3 parts
            // Decode payload (base64url)
            const payloadJson = Buffer.from(parts[1].replace(/-/g, '+').replace(/_/g, '/'), 'base64').toString('utf8');
            const payload = JSON.parse(payloadJson);
            return payload.exp ? payload.exp * 1000 > Date.now() : false;
        } catch {
            return false;
        }
    }

    const isValidAdminSession = isValidAdminToken(adminSessionToken);

    // Skip redirect logic for auth pages to prevent loops
    const isAuthPage = pathname === '/auth/login' || pathname === '/auth' || pathname === '/register' ||
        pathname === '/admin-login' || pathname.startsWith('/auth/forgot-password') ||
        pathname.startsWith('/auth/reset');

    // Redirect authenticated users away from guest-only auth pages
    if (isAuthPage) {
        if (isValidCustomerSession) {
            if (pathname === '/auth/login' || pathname === '/auth') {
                return NextResponse.redirect(new URL('/account', request.url));
            }
            if (pathname === '/register') {
                return NextResponse.redirect(new URL('/account', request.url));
            }
            if (pathname === '/auth/forgot-password' || pathname.startsWith('/auth/reset')) {
                return NextResponse.redirect(new URL('/account', request.url));
            }
        }

        if (isValidAdminSession && pathname === '/admin-login') {
            return NextResponse.redirect(new URL('/admin/dashboard', request.url));
        }

        // For unauthenticated users on auth pages, prevent back/forward cache
        const authPageResponse = NextResponse.next();
        authPageResponse.headers.set('Cache-Control', 'no-store, no-cache, must-revalidate, proxy-revalidate');
        authPageResponse.headers.set('Pragma', 'no-cache');
        authPageResponse.headers.set('Expires', '0');
        return authPageResponse;
    }

    if (isAdminProtectedPath) {
        // Check for admin authentication - use the already validated tokens
        if (!isValidAdminSession) {
            const loginUrl = new URL('/admin-login', request.url)
            loginUrl.searchParams.set('callbackUrl', pathname)
            return NextResponse.redirect(loginUrl)
        }

        // Enable rate limiting for admin routes (backend supplements this)
        const adminConfig: Partial<SecurityConfig> = {
            ...securityConfig,
            enableRateLimit: true,
            rateLimitConfig: {
                windowMs: 10 * 60 * 1000, // 10 minutes
                maxRequests: 100, // Strict for admin
                keyGenerator: (req) => `admin:${getClientIP(req)}`,
            },
        };

        const adminSecurityMiddleware = createSecurityMiddleware(adminConfig);
        const adminSecurityResponse = await adminSecurityMiddleware(request);

        if (adminSecurityResponse.status !== 200) {
            return adminSecurityResponse;
        }

        // Copy security headers from our middleware
        const response = NextResponse.next();
        adminSecurityResponse.headers.forEach((value, key) => {
            response.headers.set(key, value);
        });

        // Additional admin-specific headers
        response.headers.set('X-Frame-Options', 'DENY');
        response.headers.set('Cache-Control', 'no-store, no-cache, must-revalidate, proxy-revalidate');
        response.headers.set('Pragma', 'no-cache');
        response.headers.set('Expires', '0');

        return response;
    }

    if (isCustomerProtectedPath) {
        // Check for valid customer authentication (NextAuth session with expiry check)
        // Use the already validated session
        if (!isValidCustomerSession) {
            const loginUrl = new URL('/auth/login', request.url)
            loginUrl.searchParams.set('callbackUrl', pathname)
            return NextResponse.redirect(loginUrl)
        }
        // Customer is authenticated (per getToken). To avoid double-validation and
        // potential cookie/name mismatches that can cause redirect loops, reuse the
        // earlier securityResponse (it already applied public security headers)
        // and return it directly for customer routes.
        // Copy any headers from the initial securityResponse to a fresh response to
        // ensure we return a NextResponse object owned by this middleware.
        const response = NextResponse.next();
        try {
            securityResponse.headers.forEach((value, key) => {
                response.headers.set(key, value);
            });
        } catch (copyErr) {
            // Ignore header copy errors
        }

        // Customer-specific security headers (additive)
        response.headers.set('X-Content-Type-Options', 'nosniff');
        response.headers.set('Referrer-Policy', 'strict-origin-when-cross-origin');

        return response;
    }
    // If this is the root account path or account/orders, allow request to proceed
    // (client-side will enforce authentication). This prevents hard server redirects.
    if (isAccountRootOrOrders) {
        return securityResponse;
    }

    // For private API routes, enable rate limit + CSRF (supplements backend)
    if (pathname.startsWith('/api/') && !pathname.startsWith('/api/auth/') && !pathname.startsWith('/api/public/')) {
        const apiConfig: Partial<SecurityConfig> = {
            ...securityConfig,
            enableRateLimit: true,
            enableCSRF: ['POST', 'PUT', 'DELETE', 'PATCH'].includes(request.method),
            rateLimitConfig: {
                windowMs: 5 * 60 * 1000, // 5 minutes
                maxRequests: 400, // API rate limit
                keyGenerator: (req) => `api:${getClientIP(req)}`,
            },
        };

        const apiSecurityMiddleware = createSecurityMiddleware(apiConfig);
        return apiSecurityMiddleware(request);
    }

    // For public routes, use the security response from our middleware
    return securityResponse;
}

export const config = {
    matcher: [
        /*
         * Match all request paths except for the ones starting with:
         * - api (API routes)
         * - _next/static (static files)
         * - _next/image (image optimization files)
         * - favicon.ico (favicon file)
         */
        '/((?!api|_next/static|_next/image|favicon.ico).*)',
    ],
}