import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
    try {
        const adminToken = request.cookies.get('admin-session')?.value;
        if (!adminToken) return NextResponse.json({ success: false, error: 'Not authenticated' }, { status: 401 });

        const { searchParams } = new URL(request.url);
        const format = searchParams.get('format') || 'csv';
        const dateFrom = searchParams.get('dateFrom');
        const dateTo = searchParams.get('dateTo');

        const backendUrl = new URL('/api/admin/dashboard/export-report', process.env.NEXT_PUBLIC_API_URL);
        backendUrl.searchParams.set('format', format);
        if (dateFrom) backendUrl.searchParams.set('dateFrom', dateFrom);
        if (dateTo) backendUrl.searchParams.set('dateTo', dateTo);

        const backendResponse = await fetch(backendUrl.toString(), {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${adminToken}`
            },
        });

        if (!backendResponse.ok) {
            return NextResponse.json(
                { success: false, message: `Backend responded with ${backendResponse.status}` },
                { status: backendResponse.status }
            );
        }

        const blob = await backendResponse.arrayBuffer();
        const contentType = backendResponse.headers.get('content-type') || (format === 'pdf' ? 'application/pdf' : 'text/csv');

        return new NextResponse(Buffer.from(blob), {
            status: 200,
            headers: {
                'Content-Type': contentType,
                'Content-Disposition': `attachment; filename="dashboard-report.${format}"`
            }
        });
    } catch (error) {
        console.error('Export report proxy error:', error);
        return NextResponse.json({ success: false, error: 'Internal server error' }, { status: 500 });
    }
}


