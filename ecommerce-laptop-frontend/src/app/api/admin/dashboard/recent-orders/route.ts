import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
    try {
        // Get secure HTTP-only cookie
        const adminToken = request.cookies.get('admin-session')?.value;

        if (!adminToken) {
            return NextResponse.json(
                { success: false, error: 'Not authenticated' },
                { status: 401 }
            );
        }

        // Extract query parameters
        const { searchParams } = new URL(request.url);
        const page = searchParams.get('page') || '1';
        const limit = searchParams.get('limit') || '10';

        // Build backend URL with parameters
        const backendUrl = new URL('/api/admin/dashboard/recent-orders', process.env.NEXT_PUBLIC_API_URL);
        backendUrl.searchParams.set('page', page);
        backendUrl.searchParams.set('limit', limit);
        const status = searchParams.get('status');
        if (status) backendUrl.searchParams.set('status', status);

        // Forward request to backend with Bearer token from cookie
        const backendResponse = await fetch(backendUrl.toString(), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${adminToken}`
            },
        });

        if (!backendResponse.ok) {
            // Handle token expiry
            if (backendResponse.status === 401) {
                const response = NextResponse.json(
                    { success: false, error: 'Token expired' },
                    { status: 401 }
                );

                // Clear expired cookie
                response.cookies.set('admin-session', '', {
                    httpOnly: true,
                    secure: process.env.NODE_ENV === 'production',
                    sameSite: 'strict',
                    path: '/',
                    expires: new Date(0),
                });

                return response;
            }

            throw new Error(`Backend responded with ${backendResponse.status}`);
        }

        const data = await backendResponse.json();

        return NextResponse.json(data);

    } catch (error) {
        console.error('Recent orders proxy error:', error);
        return NextResponse.json(
            { success: false, error: 'Internal server error' },
            { status: 500 }
        );
    }
}