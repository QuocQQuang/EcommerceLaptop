import { NextRequest, NextResponse } from 'next/server';

interface AdminLoginRequest {
    email: string;
    password: string;
}

interface AdminUser {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: {
        id: string;
        name: string;
        permissions: string[];
    };
}

interface AdminLoginResponse {
    success: boolean;
    data?: {
        user: AdminUser;
        accessToken: string;
        refreshToken: string;
        expiresAt: string;
    };
    error?: string;
}

export async function POST(request: NextRequest) {
    try {
        const body: AdminLoginRequest = await request.json();

        // Call backend API for authentication
        const backendResponse = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                ...body,
                context: 'Admin'
            }),
        });

        if (!backendResponse.ok) {
            return NextResponse.json(
                { success: false, error: 'Authentication failed' },
                { status: 401 }
            );
        }

        const data: AdminLoginResponse = await backendResponse.json();

        if (!data.success || !data.data) {
            return NextResponse.json(
                { success: false, error: 'Authentication failed' },
                { status: 401 }
            );
        }

        const { user, accessToken, refreshToken, expiresAt } = data.data;

        // Create secure HTTP-only cookies
        const response = NextResponse.json({
            success: true,
            user: user
        });

        // Set HTTP-only cookies for admin session
        const cookieOptions = {
            httpOnly: true,
            secure: process.env.NODE_ENV === 'production',
            sameSite: 'strict' as const,
            // Use root path so Next API routes under /api can read the cookie
            path: '/',
            maxAge: 60 * 60 * 24 * 7, // 7 days
        };

        response.cookies.set('admin-session', accessToken, cookieOptions);
        response.cookies.set('admin-refresh', refreshToken, {
            ...cookieOptions,
            maxAge: 60 * 60 * 24 * 30, // 30 days for refresh token
        });

        return response;

    } catch (error) {
        console.error('Admin login error:', error);
        return NextResponse.json(
            { success: false, error: 'Internal server error' },
            { status: 500 }
        );
    }
}

export async function DELETE(request: NextRequest) {
    try {
        // Call backend logout
        const adminToken = request.cookies.get('admin-session')?.value;

        if (adminToken) {
            try {
                await fetch(`${process.env.NEXT_PUBLIC_API_URL}/auth/logout`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${adminToken}`
                    },
                    body: JSON.stringify({ context: 'admin' }),
                });
            } catch (error) {
                console.error('Backend logout error:', error);
            }
        }

        // Clear cookies
        const response = NextResponse.json({ success: true });

        response.cookies.set('admin-session', '', {
            httpOnly: true,
            secure: process.env.NODE_ENV === 'production',
            sameSite: 'strict',
            path: '/',
            expires: new Date(0),
        });

        response.cookies.set('admin-refresh', '', {
            httpOnly: true,
            secure: process.env.NODE_ENV === 'production',
            sameSite: 'strict',
            path: '/',
            expires: new Date(0),
        });

        return response;

    } catch (error) {
        console.error('Admin logout error:', error);
        return NextResponse.json(
            { success: false, error: 'Internal server error' },
            { status: 500 }
        );
    }
}