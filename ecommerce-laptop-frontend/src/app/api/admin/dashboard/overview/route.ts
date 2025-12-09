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

        const { searchParams } = new URL(request.url);
        const dateFrom = searchParams.get('dateFrom');
        const dateTo = searchParams.get('dateTo');

        const backendUrl = new URL('/api/admin/dashboard/overview', process.env.NEXT_PUBLIC_API_URL);
        if (dateFrom) backendUrl.searchParams.set('dateFrom', dateFrom);
        if (dateTo) backendUrl.searchParams.set('dateTo', dateTo);

        const backendResponse = await fetch(backendUrl.toString(), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${adminToken}`
            },
        });

        if (!backendResponse.ok) {
            if (backendResponse.status === 401) {
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
            return NextResponse.json(
                { success: false, message: `Backend responded with ${backendResponse.status}` },
                { status: backendResponse.status }
            );
        }

        const data = await backendResponse.json();
        return NextResponse.json(data);
    } catch (error) {
        console.error('Dashboard overview proxy error:', error);
        return NextResponse.json(
            { success: false, error: 'Internal server error' },
            { status: 500 }
        );
    }
}


