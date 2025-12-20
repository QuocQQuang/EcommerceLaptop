'use client';

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useSecurityHeaders, useSecurityMonitoring } from '@/hooks/useSecurityMiddleware';
import {
    Activity,
    AlertTriangle,
    Clock,
    Globe,
    MapPin,
    RefreshCw,
    Shield,
    TrendingDown,
    TrendingUp
} from 'lucide-react';
import { useEffect, useState } from 'react';

interface SecurityIncident {
    type: 'csp_violation' | 'xss_attempt' | 'csrf_failure' | 'rate_limit_exceeded' | 'session_violation' | 'suspicious_activity';
    details: any;
    userAgent: string;
    ip: string;
    url: string;
    timestamp: string;
    severity: 'low' | 'medium' | 'high' | 'critical';
}

interface SecuritySummary {
    incidents: SecurityIncident[];
    total: number;
    summary: {
        total: number;
        byType: Record<string, number>;
        bySeverity: Record<string, number>;
    };
}

const severityColors = {
    low: 'text-green-600 bg-green-50 border-green-200',
    medium: 'text-yellow-600 bg-yellow-50 border-yellow-200',
    high: 'text-orange-600 bg-orange-50 border-orange-200',
    critical: 'text-red-600 bg-red-50 border-red-200',
};

const severityIcons = {
    low: <Shield className="h-4 w-4" />,
    medium: <AlertTriangle className="h-4 w-4" />,
    high: <AlertTriangle className="h-4 w-4" />,
    critical: <AlertTriangle className="h-4 w-4" />,
};

const typeLabels = {
    csp_violation: 'CSP Violation',
    xss_attempt: 'XSS Attempt',
    csrf_failure: 'CSRF Failure',
    rate_limit_exceeded: 'Rate Limit Exceeded',
    session_violation: 'Session Violation',
    suspicious_activity: 'Suspicious Activity',
};

export default function SecurityDashboard() {
    const [incidents, setIncidents] = useState<SecuritySummary | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [selectedFilter, setSelectedFilter] = useState<{
        severity?: string;
        type?: string;
    }>({});
    const [autoRefresh, setAutoRefresh] = useState(true);

    const { securityMetrics, cspViolations, reportSecurityIncident } = useSecurityHeaders();
    const { securityAlerts, isMonitoring } = useSecurityMonitoring();

    const fetchIncidents = async () => {
        try {
            setIsLoading(true);
            const params: any = {
                limit: 100,
                page: 1
            };

            if (selectedFilter.severity) {
                params.severity = selectedFilter.severity;
            }
            if (selectedFilter.type) {
                params.eventType = selectedFilter.type;
            }

            const response = await adminSecurityService.getSecurityEvents(params);

            if (response && response.items) {
                // Map SecurityEvent to SecurityIncident format expected by dashboard
                const mappedIncidents: SecurityIncident[] = response.items.map(event => ({
                    type: (event.eventType as any) || 'suspicious_activity',
                    details: event.details || {},
                    userAgent: 'N/A', // Not always available in list view
                    ip: event.ipAddress || 'Unknown',
                    url: 'N/A', // Not always available
                    timestamp: event.createdAt,
                    severity: event.severity
                }));

                const summary: SecuritySummary = {
                    incidents: mappedIncidents,
                    total: response.totalCount,
                    summary: {
                        total: response.totalCount,
                        byType: {},
                        bySeverity: {} // Would need aggregation from backend or manual count
                    }
                };

                // Manual aggregation for summary since backend doesn't provide it in list response
                mappedIncidents.forEach(inc => {
                    const typeKey = inc.type;
                    const sevKey = inc.severity;
                    summary.summary.byType[typeKey] = (summary.summary.byType[typeKey] || 0) + 1;
                    summary.summary.bySeverity[sevKey] = (summary.summary.bySeverity[sevKey] || 0) + 1;
                });

                setIncidents(summary);
            } else {
                console.error('Failed to fetch security incidents');
            }
        } catch (error) {
            console.error('Error fetching security incidents:', error);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchIncidents();
    }, [selectedFilter]);

    useEffect(() => {
        if (autoRefresh) {
            const interval = setInterval(fetchIncidents, 30000); // Refresh every 30 seconds
            return () => clearInterval(interval);
        }
    }, [autoRefresh, selectedFilter]);

    const getTrend = (type: string) => {
        if (!incidents) return null;

        const recentIncidents = incidents.incidents.filter(
            i => i.type === type &&
                Date.now() - new Date(i.timestamp).getTime() < 24 * 60 * 60 * 1000
        ).length;

        const previousIncidents = incidents.incidents.filter(
            i => i.type === type &&
                Date.now() - new Date(i.timestamp).getTime() >= 24 * 60 * 60 * 1000 &&
                Date.now() - new Date(i.timestamp).getTime() < 48 * 60 * 60 * 1000
        ).length;

        if (recentIncidents > previousIncidents) {
            return <TrendingUp className="h-4 w-4 text-red-500" />;
        } else if (recentIncidents < previousIncidents) {
            return <TrendingDown className="h-4 w-4 text-green-500" />;
        }
        return null;
    };

    const formatTimestamp = (timestamp: string) => {
        const date = new Date(timestamp);
        const now = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffMins = Math.floor(diffMs / (1000 * 60));
        const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
        const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins}m ago`;
        if (diffHours < 24) return `${diffHours}h ago`;
        return `${diffDays}d ago`;
    };

    if (isLoading && !incidents) {
        return (
            <div className="flex items-center justify-center h-64">
                <RefreshCw className="h-6 w-6 animate-spin" />
                <span className="ml-2">Loading security dashboard...</span>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold">Security Dashboard</h1>
                    <p className="text-gray-600">Monitor and analyze security incidents in real-time</p>
                </div>
                <div className="flex items-center space-x-2">
                    <Button
                        variant={autoRefresh ? "default" : "outline"}
                        size="sm"
                        onClick={() => setAutoRefresh(!autoRefresh)}
                    >
                        <Activity className="h-4 w-4 mr-2" />
                        Auto Refresh
                    </Button>
                    <Button
                        variant="outline"
                        size="sm"
                        onClick={fetchIncidents}
                        disabled={isLoading}
                    >
                        <RefreshCw className={`h-4 w-4 mr-2 ${isLoading ? 'animate-spin' : ''}`} />
                        Refresh
                    </Button>
                </div>
            </div>

            {/* Real-time Monitoring Status */}
            <Alert>
                <Shield className="h-4 w-4" />
                <AlertTitle>Security Monitoring Status</AlertTitle>
                <AlertDescription className="flex items-center space-x-4">
                    <span>Monitoring: {isMonitoring ? ' Active' : ' Inactive'}</span>
                    <span>CSP Violations: {securityMetrics.cspViolations}</span>
                    <span>XSS Attempts: {securityMetrics.xssAttempts}</span>
                    <span>Live Alerts: {securityAlerts.length}</span>
                </AlertDescription>
            </Alert>

            {/* Summary Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                {incidents && Object.entries(incidents.summary.bySeverity).map(([severity, count]) => (
                    <Card key={severity}>
                        <CardContent className="p-4">
                            <div className="flex items-center justify-between">
                                <div>
                                    <p className="text-sm font-medium text-gray-600 capitalize">{severity}</p>
                                    <p className="text-2xl font-bold">{count}</p>
                                </div>
                                <div className={`p-2 rounded-full ${severityColors[severity as keyof typeof severityColors]}`}>
                                    {severityIcons[severity as keyof typeof severityIcons]}
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                ))}
            </div>

            <Tabs defaultValue="incidents" className="space-y-4">
                <TabsList>
                    <TabsTrigger value="incidents">Recent Incidents</TabsTrigger>
                    <TabsTrigger value="analytics">Analytics</TabsTrigger>
                    <TabsTrigger value="csp">CSP Violations</TabsTrigger>
                    <TabsTrigger value="alerts">Live Alerts</TabsTrigger>
                </TabsList>

                <TabsContent value="incidents" className="space-y-4">
                    {/* Filters */}
                    <div className="flex space-x-2">
                        <select
                            value={selectedFilter.severity || ''}
                            onChange={(e) => setSelectedFilter(prev => ({ ...prev, severity: e.target.value || undefined }))}
                            className="px-3 py-2 border rounded-md"
                        >
                            <option value="">All Severities</option>
                            <option value="critical">Critical</option>
                            <option value="high">High</option>
                            <option value="medium">Medium</option>
                            <option value="low">Low</option>
                        </select>
                        <select
                            value={selectedFilter.type || ''}
                            onChange={(e) => setSelectedFilter(prev => ({ ...prev, type: e.target.value || undefined }))}
                            className="px-3 py-2 border rounded-md"
                        >
                            <option value="">All Types</option>
                            {Object.entries(typeLabels).map(([key, label]) => (
                                <option key={key} value={key}>{label}</option>
                            ))}
                        </select>
                    </div>

                    {/* Incidents List */}
                    <div className="space-y-2">
                        {incidents?.incidents.map((incident, index) => (
                            <Card key={index}>
                                <CardContent className="p-4">
                                    <div className="flex items-start justify-between">
                                        <div className="flex-1">
                                            <div className="flex items-center space-x-2 mb-2">
                                                <Badge className={severityColors[incident.severity]}>
                                                    {severityIcons[incident.severity]}
                                                    <span className="ml-1 capitalize">{incident.severity}</span>
                                                </Badge>
                                                <Badge variant="outline">
                                                    {typeLabels[incident.type]}
                                                </Badge>
                                                <span className="text-sm text-gray-500 flex items-center">
                                                    <Clock className="h-3 w-3 mr-1" />
                                                    {formatTimestamp(incident.timestamp)}
                                                </span>
                                            </div>
                                            <div className="text-sm space-y-1">
                                                <div className="flex items-center space-x-2">
                                                    <MapPin className="h-3 w-3 text-gray-400" />
                                                    <span>IP: {incident.ip}</span>
                                                    <Globe className="h-3 w-3 text-gray-400" />
                                                    <span className="truncate max-w-md">URL: {incident.url}</span>
                                                </div>
                                                <div className="text-gray-600">
                                                    <details className="cursor-pointer">
                                                        <summary className="hover:text-gray-800">View Details</summary>
                                                        <pre className="mt-2 p-2 bg-gray-100 rounded text-xs overflow-auto">
                                                            {JSON.stringify(incident.details, null, 2)}
                                                        </pre>
                                                    </details>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                        {incidents?.incidents.length === 0 && (
                            <div className="text-center py-8 text-gray-500">
                                No security incidents found matching your filters.
                            </div>
                        )}
                    </div>
                </TabsContent>

                <TabsContent value="analytics" className="space-y-4">
                    <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                        <Card>
                            <CardHeader>
                                <CardTitle>Incidents by Type</CardTitle>
                                <CardDescription>Distribution of security incidents</CardDescription>
                            </CardHeader>
                            <CardContent>
                                {incidents && Object.entries(incidents.summary.byType).map(([type, count]) => (
                                    <div key={type} className="flex items-center justify-between py-2">
                                        <div className="flex items-center space-x-2">
                                            <span className="text-sm">{typeLabels[type as keyof typeof typeLabels]}</span>
                                            {getTrend(type)}
                                        </div>
                                        <Badge variant="outline">{count}</Badge>
                                    </div>
                                ))}
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader>
                                <CardTitle>Recent Activity</CardTitle>
                                <CardDescription>Security events in the last 24 hours</CardDescription>
                            </CardHeader>
                            <CardContent>
                                <div className="space-y-2">
                                    <div className="flex justify-between">
                                        <span>CSP Violations</span>
                                        <span className="font-medium">{cspViolations.length}</span>
                                    </div>
                                    <div className="flex justify-between">
                                        <span>XSS Attempts</span>
                                        <span className="font-medium">{securityMetrics.xssAttempts}</span>
                                    </div>
                                    <div className="flex justify-between">
                                        <span>Live Alerts</span>
                                        <span className="font-medium">{securityAlerts.length}</span>
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    </div>
                </TabsContent>

                <TabsContent value="csp" className="space-y-4">
                    <Card>
                        <CardHeader>
                            <CardTitle>Content Security Policy Violations</CardTitle>
                            <CardDescription>Real-time CSP violation reports</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="space-y-2 max-h-96 overflow-auto">
                                {cspViolations.map((violation, index) => (
                                    <div key={index} className="p-3 border rounded-lg">
                                        <div className="flex items-center justify-between mb-2">
                                            <Badge variant="outline">{violation.effectiveDirective}</Badge>
                                            <span className="text-xs text-gray-500">{formatTimestamp(violation.timestamp)}</span>
                                        </div>
                                        <div className="text-sm text-gray-600">
                                            <p><strong>Blocked URI:</strong> {violation.blockedURI}</p>
                                            <p><strong>Document:</strong> {violation.documentURI}</p>
                                        </div>
                                    </div>
                                ))}
                                {cspViolations.length === 0 && (
                                    <div className="text-center py-4 text-gray-500">
                                        No CSP violations detected
                                    </div>
                                )}
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>

                <TabsContent value="alerts" className="space-y-4">
                    <Card>
                        <CardHeader>
                            <CardTitle>Live Security Alerts</CardTitle>
                            <CardDescription>Real-time security monitoring alerts</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="space-y-2 max-h-96 overflow-auto">
                                {securityAlerts.map((alert, index) => (
                                    <Alert key={index}>
                                        <AlertTriangle className="h-4 w-4" />
                                        <AlertTitle className="capitalize">{alert.type.replace('_', ' ')}</AlertTitle>
                                        <AlertDescription className="flex items-center justify-between">
                                            <span>{alert.content && alert.content.substring(0, 100)}...</span>
                                            <span className="text-xs">{formatTimestamp(alert.timestamp)}</span>
                                        </AlertDescription>
                                    </Alert>
                                ))}
                                {securityAlerts.length === 0 && (
                                    <div className="text-center py-4 text-gray-500">
                                        No live alerts - system is secure
                                    </div>
                                )}
                            </div>
                        </CardContent>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    );
}