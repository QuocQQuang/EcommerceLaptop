import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
    try {
        const adminToken = request.cookies.get('admin-session')?.value;

        if (!adminToken) {
            return NextResponse.json(
                { success: false, error: 'Not authenticated' },
                { status: 401 }
            );
        }

        // Call backend to get user info
        const backendResponse = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/auth/me`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${adminToken}`
            },
        });

        if (!backendResponse.ok) {
            // Token might be expired, clear cookie
            const response = NextResponse.json(
                { success: false, error: 'Token expired' },
                { status: 401 }
            );

            response.cookies.set('admin-session', '', {
                httpOnly: true,
                secure: process.env.NODE_ENV === 'production',
                sameSite: 'strict',
                path: '/',
                expires: new Date(0),
            });

            return response;
        }

        const userData = await backendResponse.json();
        const profile = (userData && typeof userData === 'object') ? (userData.data || userData.user || userData) : userData;

        return NextResponse.json({
            success: true,
            user: profile
        });

    } catch (error) {
        console.error('Get admin user error:', error);
        return NextResponse.json(
            { success: false, error: 'Internal server error' },
            { status: 500 }
        );
    }
}