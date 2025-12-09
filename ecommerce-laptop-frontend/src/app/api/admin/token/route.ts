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

        // Return token for backend API calls (only accessible by same-origin requests)
        return NextResponse.json({
            success: true,
            token: adminToken
        });

    } catch (error) {
        console.error('Get admin token error:', error);
        return NextResponse.json(
            { success: false, error: 'Internal server error' },
            { status: 500 }
        );
    }
}