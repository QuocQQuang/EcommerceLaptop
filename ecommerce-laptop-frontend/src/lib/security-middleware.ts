import { NextRequest, NextResponse } from 'next/server';

// Web Crypto API utilities for Edge Runtime
async function generateRandomBytes(length: number): Promise<string> {
    const array = new Uint8Array(length);
    crypto.getRandomValues(array);
    return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
}

async function hashString(input: string): Promise<string> {
    const encoder = new TextEncoder();
    const data = encoder.encode(input);
    const hashBuffer = await crypto.subtle.digest('SHA-256', data);
    const hashArray = new Uint8Array(hashBuffer);
    return Array.from(hashArray, byte => byte.toString(16).padStart(2, '0')).join('');
}

// Rate limiting store (in production, use Redis or database)
const rateLimitStore = new Map<string, { count: number; resetTime: number }>();

// Helper to validate NextAuth JWT expiry (simple base64 decode check)
function isValidNextAuthToken(token: string | undefined): boolean {
    if (!token) return false;
    try {
        // JWT payload is base64url encoded. Convert to base64 before decoding.
        const base64Url = token.split('.')[1] || '';
        let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const pad = base64.length % 4;
        if (pad) {
            base64 += '='.repeat(4 - pad);
        }
        const payload = JSON.parse(atob(base64));
        return payload.exp ? payload.exp * 1000 > Date.now() : false;
    } catch {
        return false;
    }
}

// CSRF token store (in production, use Redis or database)
const csrfTokenStore = new Map<string, { token: string; expires: number; userId?: string }>();

export interface RateLimitConfig {
    windowMs: number;
    maxRequests: number;
    keyGenerator?: (req: NextRequest) => string;
    skipSuccessfulRequests?: boolean;
    skipFailedRequests?: boolean;
}

export interface SecurityConfig {
    enableCSRF: boolean;
    enableRateLimit: boolean;
    enableSessionSecurity: boolean;
    rateLimitConfig: RateLimitConfig;
    trustedOrigins: string[];
    secureHeaders: boolean;
}

const defaultSecurityConfig: SecurityConfig = {
    enableCSRF: true,
    enableRateLimit: true,
    enableSessionSecurity: true,
    rateLimitConfig: {
        windowMs: 15 * 60 * 1000, // 15 minutes
        maxRequests: 500, // Increased default for public paths
        keyGenerator: (req) => getClientIP(req),
    },
    trustedOrigins: [
        'http://localhost:3000',
        'https://localhost:3000',
        process.env.NEXT_PUBLIC_APP_URL || '',
    ],
    secureHeaders: true,
};

// Get client IP address
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

    // Fallback to a generic unknown for Vercel/internal requests - group them but don't use hostname
    return 'unknown-vercel';
}

// Rate limiting implementation
export function rateLimit(config: RateLimitConfig = defaultSecurityConfig.rateLimitConfig) {
    return (req: NextRequest): { allowed: boolean; remaining: number; resetTime: number } => {
        const key = config.keyGenerator ? config.keyGenerator(req) : getClientIP(req);
        const now = Date.now();

        // Clean up expired entries
        for (const [entryKey, entry] of rateLimitStore.entries()) {
            if (entry.resetTime < now) {
                rateLimitStore.delete(entryKey);
            }
        }

        const entry = rateLimitStore.get(key);

        if (!entry) {
            // First request from this key
            const resetTime = now + config.windowMs;
            rateLimitStore.set(key, { count: 1, resetTime });
            return { allowed: true, remaining: config.maxRequests - 1, resetTime };
        }

        if (entry.resetTime < now) {
            // Window has expired, reset
            entry.count = 1;
            entry.resetTime = now + config.windowMs;
            rateLimitStore.set(key, entry);
            return { allowed: true, remaining: config.maxRequests - 1, resetTime: entry.resetTime };
        }

        if (entry.count >= config.maxRequests) {
            // Rate limit exceeded
            return { allowed: false, remaining: 0, resetTime: entry.resetTime };
        }

        // Increment count
        entry.count += 1;
        rateLimitStore.set(key, entry);

        return {
            allowed: true,
            remaining: config.maxRequests - entry.count,
            resetTime: entry.resetTime
        };
    };
}

// CSRF protection
export class CSRFProtection {
    private static async generateToken(): Promise<string> {
        return await generateRandomBytes(32);
    }

    private static async hashToken(token: string, secret: string): Promise<string> {
        return await hashString(token + secret);
    }

    static async generateCSRFToken(userId?: string): Promise<string> {
        const token = await this.generateToken();
        const sessionId = await generateRandomBytes(16);
        const expires = Date.now() + (24 * 60 * 60 * 1000); // 24 hours

        csrfTokenStore.set(sessionId, {
            token,
            expires,
            userId,
        });

        return `${sessionId}.${token}`;
    }

    static validateCSRFToken(submittedToken: string, userId?: string): boolean {
        if (!submittedToken || !submittedToken.includes('.')) {
            return false;
        }

        const [sessionId, token] = submittedToken.split('.');
        const storedData = csrfTokenStore.get(sessionId);

        if (!storedData) {
            return false;
        }

        // Check expiration
        if (storedData.expires < Date.now()) {
            csrfTokenStore.delete(sessionId);
            return false;
        }

        // Check token match
        if (storedData.token !== token) {
            return false;
        }

        // Check user ID if provided
        if (userId && storedData.userId && storedData.userId !== userId) {
            return false;
        }

        return true;
    }

    static cleanupExpiredTokens(): void {
        const now = Date.now();
        for (const [sessionId, data] of csrfTokenStore.entries()) {
            if (data.expires < now) {
                csrfTokenStore.delete(sessionId);
            }
        }
    }
}

// Session security enhancements
export class SessionSecurity {
    private static readonly SESSION_TIMEOUT = 30 * 60 * 1000; // 30 minutes
    private static readonly MAX_SESSION_DURATION = 8 * 60 * 60 * 1000; // 8 hours

    static validateSession(req: NextRequest): {
        valid: boolean;
        reason?: string;
        shouldRefresh?: boolean;
    } {
        // Check NextAuth session cookies
        const customerSessionToken = req.cookies.get('next-auth.session-token')?.value ||
            req.cookies.get('__Secure-next-auth.session-token')?.value;

        if (!customerSessionToken) {
            return { valid: false, reason: 'No NextAuth session cookie' };
        }

        const isValid = isValidNextAuthToken(customerSessionToken);
        if (!isValid) {
            return { valid: false, reason: 'NextAuth session expired or invalid' };
        }

        // For NextAuth, refresh logic is handled in JWT callback; here just validate
        // Assume no immediate refresh needed if valid
        return { valid: true };
    }

    static async generateSecureSessionId(): Promise<string> {
        return await generateRandomBytes(32);
    }

    static async createSessionCookie(sessionData: any): Promise<string> {
        const expires = Date.now() + this.SESSION_TIMEOUT;
        const sessionId = await this.generateSecureSessionId();
        const session = {
            ...sessionData,
            createdAt: sessionData.createdAt || Date.now(),
            expiresAt: expires,
            sessionId,
        };

        return btoa(JSON.stringify(session));
    }
}

// Security headers
export function addSecurityHeaders(response: NextResponse): NextResponse {
    // Content Security Policy - simplified to reduce header size for HTTP 431 fix
    const csp = [
        "default-src 'self'",
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https:",
        "style-src 'self' 'unsafe-inline' https:",
        "font-src 'self' https: data:",
        "img-src 'self' data: https: blob:",
        "connect-src 'self' https: wss:",
        "frame-src 'self' https:",
        "object-src 'none'",
        "base-uri 'self'",
        "form-action 'self'",
        "frame-ancestors 'none'",
    ].join('; ');

    response.headers.set('Content-Security-Policy', csp);
    response.headers.set('X-Content-Type-Options', 'nosniff');
    response.headers.set('X-Frame-Options', 'DENY');
    response.headers.set('X-XSS-Protection', '1; mode=block');
    response.headers.set('Referrer-Policy', 'strict-origin-when-cross-origin');
    response.headers.set('Permissions-Policy', 'camera=(), microphone=(), geolocation=()');

    // HSTS (only in production with HTTPS)
    if (process.env.NODE_ENV === 'production') {
        response.headers.set('Strict-Transport-Security', 'max-age=31536000; includeSubDomains; preload');
    }

    return response;
}

// Origin validation
export function validateOrigin(req: NextRequest, trustedOrigins: string[]): boolean {
    const origin = req.headers.get('origin');
    const referer = req.headers.get('referer');

    if (!origin && !referer) {
        // Allow same-origin requests without origin header
        return true;
    }

    const requestOrigin = origin || (referer ? new URL(referer).origin : '');

    return trustedOrigins.some(trusted => {
        if (trusted === requestOrigin) return true;

        // Allow localhost with any port in development
        if (process.env.NODE_ENV === 'development' &&
            requestOrigin.startsWith('http://localhost:') &&
            trusted.startsWith('http://localhost:')) {
            return true;
        }

        return false;
    });
}

// Main security middleware
export function createSecurityMiddleware(config: Partial<SecurityConfig> = {}) {
    const finalConfig = { ...defaultSecurityConfig, ...config };
    const rateLimiter = rateLimit(finalConfig.rateLimitConfig);

    return async (req: NextRequest): Promise<NextResponse> => {
        let response = NextResponse.next();

        // Add security headers
        if (finalConfig.secureHeaders) {
            response = addSecurityHeaders(response);
        }

        // Validate origin for state-changing requests
        if (['POST', 'PUT', 'DELETE', 'PATCH'].includes(req.method)) {
            if (!validateOrigin(req, finalConfig.trustedOrigins)) {
                return new NextResponse('Forbidden: Invalid origin', { status: 403 });
            }
        }

        // Rate limiting - skip for safe public GET requests to avoid over-limiting crawlers/health checks
        if (finalConfig.enableRateLimit && !(req.method === 'GET' && req.nextUrl.pathname === '/')) {
            const rateLimitResult = rateLimiter(req);

            if (!rateLimitResult.allowed) {
                const resetDate = new Date(rateLimitResult.resetTime);
                response = new NextResponse('Too Many Requests', {
                    status: 429,
                    headers: {
                        'Retry-After': Math.ceil((rateLimitResult.resetTime - Date.now()) / 1000).toString(),
                        'X-RateLimit-Limit': finalConfig.rateLimitConfig.maxRequests.toString(),
                        'X-RateLimit-Remaining': '0',
                        'X-RateLimit-Reset': resetDate.toISOString(),
                    }
                });
                return addSecurityHeaders(response);
            }

            // Add rate limit headers
            response.headers.set('X-RateLimit-Limit', finalConfig.rateLimitConfig.maxRequests.toString());
            response.headers.set('X-RateLimit-Remaining', rateLimitResult.remaining.toString());
            response.headers.set('X-RateLimit-Reset', new Date(rateLimitResult.resetTime).toISOString());
        } else if (req.method === 'GET' && req.nextUrl.pathname === '/') {
            // For root GET, use higher limits or log only
            response.headers.set('X-RateLimit-Limit', '1000');
            response.headers.set('X-RateLimit-Remaining', '999');
        }

        // CSRF protection for state-changing requests
        if (finalConfig.enableCSRF && ['POST', 'PUT', 'DELETE', 'PATCH'].includes(req.method)) {
            const csrfToken = req.headers.get('X-CSRF-Token') ||
                req.headers.get('x-csrf-token') ||
                req.nextUrl.searchParams.get('_token');

            if (!csrfToken || !CSRFProtection.validateCSRFToken(csrfToken)) {
                return new NextResponse('Forbidden: Invalid CSRF token', { status: 403 });
            }
        }

        // Session validation
        if (finalConfig.enableSessionSecurity) {
            // Skip session validation for public routes and auth pages to prevent loops
            const publicRoutes = [
                '/api/auth/login',
                '/api/auth/register',
                '/api/public',
                '/terms',
                '/privacy',
                '/products',
                '/about',
                '/contact',
                '/manifest.json'
            ];
            const authPages = [
                '/auth/login',
                '/auth',
                '/register',
                '/admin-login',
                '/terms',
                '/privacy',
                '/products',
                '/about',
                '/contact'
            ];
            const isPublicRoute = publicRoutes.some(route => req.nextUrl.pathname.startsWith(route));
            const isAuthPage = authPages.some(page => req.nextUrl.pathname === page || req.nextUrl.pathname.startsWith(page));

            if (!isPublicRoute && !isAuthPage) {
                const sessionValidation = SessionSecurity.validateSession(req);

                if (!sessionValidation.valid) {
                    // Redirect to login or return 401 for API routes
                    if (req.nextUrl.pathname.startsWith('/api/')) {
                        return new NextResponse('Unauthorized: Invalid session', { status: 401 });
                    } else {
                        return NextResponse.redirect(new URL('/auth/login', req.url));
                    }
                }

                // Refresh session if needed
                if (sessionValidation.shouldRefresh) {
                    const sessionCookie = req.cookies.get('session');
                    if (sessionCookie) {
                        try {
                            const sessionData = JSON.parse(atob(sessionCookie.value));
                            const newSessionValue = await SessionSecurity.createSessionCookie(sessionData);
                            response.cookies.set('session', newSessionValue, {
                                httpOnly: true,
                                secure: process.env.NODE_ENV === 'production',
                                sameSite: 'strict',
                                maxAge: SessionSecurity['SESSION_TIMEOUT'] / 1000,
                            });
                        } catch (error) {
                            console.error('Session refresh error:', error);
                        }
                    }
                }
            }
        }

        return response;
    };
}

// Cleanup function to run periodically
export function cleanupSecurityStore(): void {
    // Clean up expired rate limit entries
    const now = Date.now();
    for (const [key, entry] of rateLimitStore.entries()) {
        if (entry.resetTime < now) {
            rateLimitStore.delete(key);
        }
    }

    // Clean up expired CSRF tokens
    CSRFProtection.cleanupExpiredTokens();
}

// Security utilities for components
export const SecurityUtils = {
    generateCSRFToken: CSRFProtection.generateCSRFToken,
    validateCSRFToken: CSRFProtection.validateCSRFToken,
    generateSessionId: SessionSecurity.generateSecureSessionId,
    createSessionCookie: SessionSecurity.createSessionCookie,
    createSecureHeaders: addSecurityHeaders,
    validateOrigin,
    cleanupStore: cleanupSecurityStore,
};

export default createSecurityMiddleware;