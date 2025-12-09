import { cleanupSecurityStore } from '@/lib/security-middleware';
import { NextRequest, NextResponse } from 'next/server';

// Security incident types
interface SecurityIncident {
    type: 'csp_violation' | 'xss_attempt' | 'csrf_failure' | 'rate_limit_exceeded' | 'session_violation' | 'suspicious_activity';
    details: any;
    userAgent: string;
    ip: string;
    url: string;
    timestamp: string;
    severity: 'low' | 'medium' | 'high' | 'critical';
}

// In-memory storage for incidents (in production, use a database)
const securityIncidents: SecurityIncident[] = [];
const maxIncidents = 1000;

function getClientIP(req: NextRequest): string {
    const forwarded = req.headers.get('x-forwarded-for');
    const realIP = req.headers.get('x-real-ip');

    if (forwarded) {
        return forwarded.split(',')[0].trim();
    }

    if (realIP) {
        return realIP;
    }

    return 'unknown';
}

function determineSeverity(incident: Partial<SecurityIncident>): SecurityIncident['severity'] {
    switch (incident.type) {
        case 'csrf_failure':
        case 'session_violation':
            return 'high';
        case 'xss_attempt':
        case 'suspicious_activity':
            return 'medium';
        case 'rate_limit_exceeded':
            return 'medium';
        case 'csp_violation':
            return 'low';
        default:
            return 'low';
    }
}

// CSP Violation Report Handler
export async function POST(req: NextRequest) {
    try {
        const url = new URL(req.url);
        const endpoint = url.pathname.split('/').pop();

        if (endpoint === 'csp-report') {
            const report = await req.json();

            const incident: SecurityIncident = {
                type: 'csp_violation',
                details: report,
                userAgent: req.headers.get('user-agent') || 'unknown',
                ip: getClientIP(req),
                url: req.headers.get('referer') || 'unknown',
                timestamp: new Date().toISOString(),
                severity: 'low',
            };

            // Store incident
            securityIncidents.push(incident);
            if (securityIncidents.length > maxIncidents) {
                securityIncidents.shift();
            }

            // Log for monitoring
            console.warn('CSP Violation:', incident);

            return NextResponse.json({ status: 'reported' }, { status: 200 });
        }

        if (endpoint === 'incident-report') {
            const reportData = await req.json();

            const incident: SecurityIncident = {
                type: reportData.type || 'suspicious_activity',
                details: reportData.details || reportData,
                userAgent: req.headers.get('user-agent') || 'unknown',
                ip: getClientIP(req),
                url: reportData.url || req.headers.get('referer') || 'unknown',
                timestamp: new Date().toISOString(),
                severity: determineSeverity({ type: reportData.type }),
            };

            // Store incident
            securityIncidents.push(incident);
            if (securityIncidents.length > maxIncidents) {
                securityIncidents.shift();
            }

            // Log critical incidents immediately
            if (incident.severity === 'critical' || incident.severity === 'high') {
                console.error('High Severity Security Incident:', incident);

                // In production, send alerts to security team
                if (process.env.NODE_ENV === 'production') {
                    // Send to monitoring service, email alerts, etc.
                }
            }

            return NextResponse.json({ status: 'reported', severity: incident.severity }, { status: 200 });
        }

        if (endpoint === 'alert') {
            const alertData = await req.json();

            const incident: SecurityIncident = {
                type: alertData.type || 'suspicious_activity',
                details: alertData,
                userAgent: req.headers.get('user-agent') || 'unknown',
                ip: getClientIP(req),
                url: alertData.url || req.headers.get('referer') || 'unknown',
                timestamp: new Date().toISOString(),
                severity: determineSeverity({ type: alertData.type }),
            };

            securityIncidents.push(incident);
            if (securityIncidents.length > maxIncidents) {
                securityIncidents.shift();
            }

            console.log('Security Alert:', incident);

            return NextResponse.json({ status: 'alerted' }, { status: 200 });
        }

        return NextResponse.json({ error: 'Unknown endpoint' }, { status: 404 });

    } catch (error) {
        console.error('Security monitoring error:', error);
        return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
    }
}

// Get security incidents (admin only)
export async function GET(req: NextRequest) {
    try {
        const url = new URL(req.url);
        const endpoint = url.pathname.split('/').pop();

        // Simple admin check (in production, use proper authentication)
        const adminToken = req.cookies.get('admin-session')?.value;
        if (!adminToken) {
            return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
        }

        if (endpoint === 'incidents') {
            const limit = parseInt(url.searchParams.get('limit') || '50');
            const severity = url.searchParams.get('severity') as SecurityIncident['severity'] | null;
            const type = url.searchParams.get('type') as SecurityIncident['type'] | null;

            let filteredIncidents = [...securityIncidents];

            if (severity) {
                filteredIncidents = filteredIncidents.filter(i => i.severity === severity);
            }

            if (type) {
                filteredIncidents = filteredIncidents.filter(i => i.type === type);
            }

            // Sort by timestamp (newest first)
            filteredIncidents.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());

            return NextResponse.json({
                incidents: filteredIncidents.slice(0, limit),
                total: filteredIncidents.length,
                summary: {
                    total: securityIncidents.length,
                    byType: securityIncidents.reduce((acc, incident) => {
                        acc[incident.type] = (acc[incident.type] || 0) + 1;
                        return acc;
                    }, {} as Record<string, number>),
                    bySeverity: securityIncidents.reduce((acc, incident) => {
                        acc[incident.severity] = (acc[incident.severity] || 0) + 1;
                        return acc;
                    }, {} as Record<string, number>),
                },
            });
        }

        if (endpoint === 'health') {
            // Cleanup expired entries
            cleanupSecurityStore();

            return NextResponse.json({
                status: 'healthy',
                timestamp: new Date().toISOString(),
                incidentCount: securityIncidents.length,
                recentIncidents: securityIncidents
                    .filter(i => Date.now() - new Date(i.timestamp).getTime() < 24 * 60 * 60 * 1000)
                    .length,
            });
        }

        return NextResponse.json({ error: 'Unknown endpoint' }, { status: 404 });

    } catch (error) {
        console.error('Security monitoring GET error:', error);
        return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
    }
}