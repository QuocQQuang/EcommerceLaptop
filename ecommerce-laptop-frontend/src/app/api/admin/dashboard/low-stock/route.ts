import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
    try {
        const adminToken = request.cookies.get('admin-session')?.value;
        if (!adminToken) return NextResponse.json({ success: false, error: 'Not authenticated' }, { status: 401 });

        const { searchParams } = new URL(request.url);
        const threshold = searchParams.get('threshold') || '10';

        const backendUrl = new URL('/api/admin/dashboard/low-stock', process.env.NEXT_PUBLIC_API_URL);
        backendUrl.searchParams.set('threshold', threshold);

        const backendResponse = await fetch(backendUrl.toString(), {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${adminToken}`
            },
        });

        if (!backendResponse.ok) {
            return NextResponse.json(
                { success: false, message: `Backend responded with ${backendResponse.status}` },
                { status: backendResponse.status }
            );
        }

        const data = await backendResponse.json();
        return NextResponse.json(data);
    } catch (error) {
        console.error('Low stock proxy error:', error);
        return NextResponse.json({ success: false, error: 'Internal server error' }, { status: 500 });
    }
}


