import { extractAdminSession, handleBackendError } from '@/utils/admin-proxy-utils';
import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
    try {
        // Get secure HTTP-only cookie
        const adminToken = extractAdminSession(request.headers.get('cookie'));

        if (!adminToken) {
            return NextResponse.json(
                { success: false, error: 'Not authenticated' },
                { status: 401 }
            );
        }

        // Extract query parameters
        const { searchParams } = new URL(request.url);
        const dateFrom = searchParams.get('dateFrom');
        const dateTo = searchParams.get('dateTo');

        // Build backend URL with parameters
        const backendUrl = new URL('/api/admin/dashboard/kpis', process.env.NEXT_PUBLIC_API_URL || '');
        if (dateFrom) backendUrl.searchParams.set('dateFrom', dateFrom);
        if (dateTo) backendUrl.searchParams.set('dateTo', dateTo);

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

            const errorResult = handleBackendError({
                response: { status: backendResponse.status },
                message: `Backend responded with ${backendResponse.status}`
            });

            return NextResponse.json(errorResult, { status: errorResult.status });
        }

        const data = await backendResponse.json();

        return NextResponse.json(data);

    } catch (error) {
        console.error('Dashboard KPIs proxy error:', error);
        const errorResult = handleBackendError(error);
        return NextResponse.json(errorResult, { status: errorResult.status });
    }
}