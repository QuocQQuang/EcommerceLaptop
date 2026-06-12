'use client';

import api from '@/lib/api';
import logger from '@/lib/logger';
import { useSession } from 'next-auth/react';
import { usePathname } from 'next/navigation';
import { useEffect, useRef, useState } from 'react';

interface LogEntry {
    timestamp: string;
    level: string;
    message: string;
    meta?: any;
    url?: string;
    userAgent?: string;
}

function DebugLoggerContent({ session, status }: { session: any; status: string }) {
    const [logs, setLogs] = useState<LogEntry[]>([]);
    const [isVisible, setIsVisible] = useState(false);
    const [filter, setFilter] = useState('all');
    const [copiedMap, setCopiedMap] = useState<Record<string, boolean>>({});
    const [copyAllFeedback, setCopyAllFeedback] = useState<string | null>(null);
    const [isHydrated, setIsHydrated] = useState(false);

    // Draggable bubble position (distance from right and bottom in px)
    const [pos, setPos] = useState<{ right: number; bottom: number }>({ right: 16, bottom: 16 });
    const dragRef = useRef<{ dragging: boolean; startX: number; startY: number; startRight: number; startBottom: number }>({ dragging: false, startX: 0, startY: 0, startRight: 0, startBottom: 0 });

    useEffect(() => {
        // Set hydration state and load position from localStorage
        setIsHydrated(true);

        try {
            const raw = localStorage.getItem('debugLoggerPos');
            if (raw) {
                setPos(JSON.parse(raw));
            }
        } catch (e) {
            // Keep default position
        }

        // Load logs from localStorage
        const loadLogs = () => {
            setLogs(logger.getLogs());
        };

        loadLogs();

        // Refresh logs every 2 seconds
        const interval = setInterval(loadLogs, 2000);

        return () => clearInterval(interval);
    }, []);

    useEffect(() => {
        // persist position
        try {
            localStorage.setItem('debugLoggerPos', JSON.stringify(pos));
        } catch (e) {
            // ignore
        }
    }, [pos]);

    const filteredLogs = logs.filter(log => {
        if (filter === 'all') return true;
        return log.level === filter;
    });

    const filteredLogsLimited = filteredLogs.slice(-200).reverse();

    const clearLogs = () => {
        logger.clearLogs();
        setLogs([]);
    };

    const copyTextForLog = (log: LogEntry) => {
        const parts = [];
        parts.push(`[${new Date(log.timestamp).toLocaleString()}] ${log.level.toUpperCase()}`);
        parts.push(log.message);
        if (log.meta) parts.push(typeof log.meta === 'string' ? log.meta : JSON.stringify(log.meta, null, 2));
        if (log.url) parts.push(`URL: ${log.url}`);
        if (log.userAgent) parts.push(`UA: ${log.userAgent}`);
        return parts.join('\n');
    };

    const handleCopyLog = async (log: LogEntry, key: string) => {
        try {
            await navigator.clipboard.writeText(copyTextForLog(log));
            setCopiedMap((m) => ({ ...m, [key]: true }));
            setTimeout(() => setCopiedMap((m) => ({ ...m, [key]: false })), 1800);
        } catch (e) {
            // fallback: do nothing
        }
    };

    const handleCopyAll = async () => {
        try {
            const text = filteredLogs.map(copyTextForLog).join('\n\n----\n\n');
            await navigator.clipboard.writeText(text);
            setCopyAllFeedback(`Copied ${filteredLogs.length} logs`);
            setTimeout(() => setCopyAllFeedback(null), 1800);
        } catch (e) {
            setCopyAllFeedback('Copy failed');
            setTimeout(() => setCopyAllFeedback(null), 1800);
        }
    };

    // Drag handlers (pointer-based)
    const onPointerDown = (e: React.PointerEvent) => {
        const p = dragRef.current;
        p.dragging = true;
        p.startX = e.clientX;
        p.startY = e.clientY;
        p.startRight = pos.right;
        p.startBottom = pos.bottom;
        (e.target as Element).setPointerCapture(e.pointerId);
    };

    const onPointerMove = (e: React.PointerEvent) => {
        const p = dragRef.current;
        if (!p.dragging) return;
        const dx = e.clientX - p.startX;
        const dy = e.clientY - p.startY;
        // Adjust right/bottom (invert x to move left when dragging right)
        const newRight = Math.max(8, p.startRight - dx);
        const newBottom = Math.max(8, p.startBottom - dy);
        setPos({ right: newRight, bottom: newBottom });
    };

    const onPointerUp = (e: React.PointerEvent) => {
        const p = dragRef.current;
        p.dragging = false;
        try { (e.target as Element).releasePointerCapture(e.pointerId); } catch (err) { }
    };

    const addTestItemToCart = async () => {
        logger.info(' Adding test item to cart');
        try {
            // Add a test product to cart
            const addItemRequest = {
                productId: 1, // Assuming product ID 1 exists
                quantity: 1
            };

            const response = await api.post('/cart/items', addItemRequest);

            logger.info(' Test item added to cart successfully', {
                status: response.status,
                response: response.data
            });
        } catch (error: any) {
            logger.error(' Failed to add test item to cart', {
                message: error.message,
                status: error.response?.status,
                statusText: error.response?.statusText,
                responseData: error.response?.data
            });
        }
    };

    const testOrderCreation = async () => {
        logger.info(' Testing Order Creation manually');
        logger.info(' Session status', {
            status,
            hasSession: !!session,
            hasAccessToken: !!session?.accessToken,
            tokenLength: session?.accessToken?.length
        });

        try {
            // First get cart to get cartId using api client
            logger.info(' Getting cart data...');
            const cartResponse = await api.get('/cart');
            let cartData = cartResponse.data;

            // Debug: log full cart response structure
            logger.debug(' Full cart response structure', {
                keys: Object.keys(cartData),
                fullData: cartData
            });

            // Try different field names for cart ID
            let cartId = cartData.CartId || cartData.cartId || cartData.Id || cartData.id;

            if (!cartId) {
                logger.error(' No cart ID found in response', {
                    availableFields: Object.keys(cartData),
                    cartData: cartData,
                    needsCartCreation: !cartData.Items || cartData.Items.length === 0
                });

                // If cart is empty and no ID, try to add an item first
                if (!cartData.Items || cartData.Items.length === 0) {
                    logger.info(' Cart is empty, adding test item first...');
                    await addTestItemToCart();

                    // Retry getting cart
                    const retryCartResponse = await api.get('/cart');
                    const retryCartData = retryCartResponse.data;
                    const retryCartId = retryCartData.CartId || retryCartData.cartId || retryCartData.Id || retryCartData.id;

                    if (!retryCartId) {
                        logger.error(' Still no cart ID after adding item', { retryCartData });
                        return;
                    }

                    logger.info(' Got cart ID after adding item', { cartId: retryCartId });

                    // Update variables for order creation
                    cartData = retryCartData;
                    cartId = retryCartId;
                } else {
                    return;
                }
            }

            logger.info(' Got cart data for order test', {
                cartId: cartId,
                itemCount: cartData.summary?.itemCount,
                hasItems: cartData.items?.length > 0
            });

            // Now try to create order using api client
            const orderRequest = {
                shippingAddress: "123 Test Street, Test City, Test Province, Vietnam",
                paymentMethod: "COD",
                orderStatus: "Pending"
            };

            logger.info(' Sending order creation request', {
                cartId: cartId,
                request: orderRequest
            });

            const orderResponse = await api.post(`/orders/from-cart/${cartId}`, orderRequest);
            const orderData = orderResponse.data;

            logger.info(' Order creation test successful', {
                status: orderResponse.status,
                orderId: orderData.id,
                orderNumber: orderData.orderNumber
            });
        } catch (error: any) {
            logger.error(' Order creation test error', {
                message: error.message,
                status: error.response?.status,
                statusText: error.response?.statusText,
                responseData: error.response?.data,
                stack: error.stack
            });
        }
    };

    const testApiConnection = async () => {
        logger.info(' Testing basic API connection');
        try {
            // Test basic API connectivity with a simple health/status endpoint
            const response = await api.get('/health');
            logger.info(' API connection successful', {
                status: response.status,
                message: response.data?.message || 'OK',
                timestamp: new Date().toISOString()
            });
        } catch (error: any) {
            // If /health doesn't exist, try /status
            try {
                const statusResponse = await api.get('/status');
                logger.info(' API connection successful (status endpoint)', {
                    status: statusResponse.status,
                    message: statusResponse.data?.message || 'OK',
                    timestamp: new Date().toISOString()
                });
            } catch (statusError: any) {
                logger.error(' API connection test failed', {
                    message: error.message || statusError.message,
                    status: error.response?.status || statusError.response?.status,
                    endpoint: '/health or /status'
                });
            }
        }
    };

    const testCartApi = async () => {
        logger.info(' Testing Cart API manually');
        logger.info(' Session status', {
            status,
            hasSession: !!session,
            hasAccessToken: !!session?.accessToken
        });

        try {
            const response = await api.get('/cart');
            const data = response.data;

            logger.info(' Cart API test successful', {
                status: response.status,
                cartId: data.cartId,
                itemCount: data.summary?.itemCount,
                total: data.summary?.totalAmount
            });
        } catch (error: any) {
            logger.error(' Cart API test failed', {
                message: error.message,
                status: error.response?.status,
                statusText: error.response?.statusText,
                responseData: error.response?.data
            });
        }
    };

    const testAdminDashboard = async () => {
        logger.info(' Testing Admin Dashboard APIs vi logging chi tit');
        try {
            // Test dashboard KPIs
            const kpisResponse = await api.get('/api/admin/dashboard/kpis');
            const kpisData = kpisResponse.data;
            logger.info(' Dashboard KPIs loaded', {
                status: kpisResponse.status,
                totalRevenue: kpisData?.totalRevenue,
                totalOrders: kpisData?.totalOrders,
                totalCustomers: kpisData?.totalCustomers,
                totalProducts: kpisData?.totalProducts,
                revenueChange: kpisData?.revenueChange,
                ordersChange: kpisData?.ordersChange,
                customersChange: kpisData?.customersChange,
                productsChange: kpisData?.productsChange,
                fullResponse: kpisData
            });

            // Test recent orders
            const ordersResponse = await api.get('/api/admin/dashboard/recent-orders');
            const ordersData = ordersResponse.data;
            logger.info(' Recent orders loaded', {
                status: ordersResponse.status,
                ordersCount: ordersData?.orders?.length || 0,
                orders: ordersData?.orders?.map((order: any) => ({
                    id: order.id,
                    customerName: order.customerName,
                    totalAmount: order.totalAmount,
                    status: order.status,
                    createdAt: order.createdAt
                })) || [],
                fullResponse: ordersData
            });

            // Test sales trend
            const salesResponse = await api.get('/api/admin/dashboard/sales-trend');
            const salesData = salesResponse.data;
            logger.info(' Sales trend loaded', {
                status: salesResponse.status,
                dataPoints: salesData?.data?.length || 0,
                dateRange: salesData?.data ? {
                    first: salesData.data[0]?.date,
                    last: salesData.data[salesData.data.length - 1]?.date
                } : null,
                totalRevenue: salesData?.data?.reduce((sum: number, point: any) => sum + (point.revenue || 0), 0) || 0,
                sampleDataPoints: salesData?.data?.slice(0, 3) || [], // Show first 3 data points
                fullResponse: salesData
            });

            // Test security events summary
            try {
                const securityResponse = await api.get('/admin/security/security-events?page=1&pageSize=5');
                const securityData = securityResponse.data;
                logger.info(' Security events summary loaded', {
                    status: securityResponse.status,
                    eventsCount: securityData?.events?.length || 0,
                    recentEvents: securityData?.events?.map((event: any) => ({
                        id: event.id,
                        type: event.type,
                        severity: event.severity,
                        createdAt: event.createdAt
                    })) || [],
                    fullResponse: securityData
                });
            } catch (securityError: any) {
                logger.warn(' Security events test failed (non-critical)', {
                    message: securityError.message,
                    status: securityError.response?.status
                });
            }

            logger.info('Admin Dashboard test completed successfully - tất cả API responses đã có log chi tiết');

        } catch (error: any) {
            logger.error(' Admin Dashboard test failed', {
                message: error.message,
                status: error.response?.status,
                statusText: error.response?.statusText,
                responseData: error.response?.data,
                url: error.config?.url
            });
        }
    };

    const testAdminProducts = async () => {
        logger.info(' Testing Admin Products API');
        try {
            const response = await api.get('/products/admin?page=1&pageSize=10');
            logger.info(' Admin products loaded', {
                status: response.status,
                data: response.data
            });
        } catch (error: any) {
            logger.error(' Admin Products test failed', {
                message: error.message,
                status: error.response?.status,
                responseData: error.response?.data
            });
        }
    };

    const testAdminSecurity = async () => {
        logger.info(' Testing Admin Security APIs');
        try {
            // Test security events
            const eventsResponse = await api.get('/admin/security/security-events');
            logger.info(' Security events loaded', {
                status: eventsResponse.status,
                data: eventsResponse.data
            });

            // Test IP blocks
            const ipBlocksResponse = await api.get('/admin/security/ip-rules');
            logger.info(' IP blocks loaded', {
                status: ipBlocksResponse.status,
                data: ipBlocksResponse.data
            });
        } catch (error: any) {
            logger.error(' Admin Security test failed', {
                message: error.message,
                status: error.response?.status,
                responseData: error.response?.data
            });
        }
    };

    const testAdminOrders = async () => {
        logger.info(' Testing Admin Orders API');
        try {
            const response = await api.get('/admin/orders?page=1&pageSize=10');
            logger.info(' Admin orders loaded', {
                status: response.status,
                data: response.data
            });
        } catch (error: any) {
            logger.error(' Admin Orders test failed', {
                message: error.message,
                status: error.response?.status,
                responseData: error.response?.data
            });
        }
    };

    const testAdminLogs = async () => {
        logger.info(' Testing Admin Logs API');
        try {
            const response = await api.get('/admin/security/audit-logs?skip=0&take=10');
            logger.info(' Admin logs loaded', {
                status: response.status,
                data: response.data
            });
        } catch (error: any) {
            logger.error(' Admin Logs test failed', {
                message: error.message,
                status: error.response?.status,
                responseData: error.response?.data
            });
        }
    };

    // Small draggable bubble when closed
    if (!isVisible) {
        return (
            <div
                className="fixed z-50"
                style={isHydrated ? { right: pos.right, bottom: pos.bottom } : { right: "16px", bottom: "16px" }}
            >
                <div
                    onPointerDown={onPointerDown}
                    onPointerMove={onPointerMove}
                    onPointerUp={onPointerUp}
                    className="flex items-center gap-2 bg-gray-900/95 text-green-300 font-mono text-xs px-3 py-2 rounded-full shadow-lg cursor-grab select-none"
                    role="button"
                    aria-label={`Open debug logs (${logs.length})`}
                    title="Drag to move  click to open"
                    onClick={() => setIsVisible(true)}
                >
                    <div className="w-3 h-3 rounded-full bg-green-400/80 animate-pulse" />
                    <div className="px-1"></div>
                    <div className="font-semibold">{logs.length}</div>
                </div>
            </div>
        );
    }

    return (
        <div
            className="fixed z-50 w-[28rem] max-w-[90vw] h-[22rem] bg-gray-900 text-gray-100 font-mono text-xs rounded-lg shadow-2xl flex flex-col transition-transform duration-200 ease-out transform scale-100"
            style={{ right: pos.right, bottom: pos.bottom }}
        >
            {/* Header */}
            <div className="bg-gray-800 px-3 py-2 rounded-t-lg flex justify-between items-center">
                <div className="flex items-center gap-2">
                    <span className="font-bold"> Debug</span>
                    <span className="text-sm text-gray-400">({logs.length})</span>
                    <span className="ml-2 text-xs text-gray-400">{copyAllFeedback ?? ''}</span>
                </div>
                <div className="flex gap-1 items-center flex-wrap">
                    <button
                        onClick={clearLogs}
                        className="bg-gray-600 text-white px-2 py-0.5 rounded text-xs hover:bg-gray-700"
                        title="Clear logs"
                    >
                        Clear
                    </button>
                    <button
                        onClick={handleCopyAll}
                        className="bg-gray-700 text-white px-2 py-0.5 rounded text-xs hover:bg-gray-600"
                        title="Copy all (filtered)"
                    >
                        Copy All
                    </button>
                    <button
                        onClick={() => setIsVisible(false)}
                        className="bg-gray-600 text-white px-2 py-0.5 rounded text-xs hover:bg-gray-700"
                        title="Close"
                    >
                        
                    </button>
                </div>
            </div>

            {/* Filters */}
            <div className="px-3 py-2 bg-gray-800 border-b border-gray-700 flex items-center gap-2">
                <select
                    value={filter}
                    onChange={(e) => setFilter(e.target.value)}
                    className="bg-gray-700 text-white text-xs px-2 py-1 rounded"
                >
                    <option value="all">All ({logs.length})</option>
                    <option value="error">Errors ({logs.filter(l => l.level === 'error').length})</option>
                    <option value="warn">Warnings ({logs.filter(l => l.level === 'warn').length})</option>
                    <option value="info">Info ({logs.filter(l => l.level === 'info').length})</option>
                    <option value="debug">Debug ({logs.filter(l => l.level === 'debug').length})</option>
                </select>
                <div className="ml-auto text-xs text-gray-400">Showing {filteredLogs.length} logs</div>
            </div>

            {/* Logs */}
            <div className="flex-1 overflow-y-auto p-2 space-y-2 debug-logger-scroll" style={{ scrollbarWidth: 'thin' }}>
                {filteredLogsLimited.length === 0 ? (
                    <div className="text-gray-500 text-center py-4">No logs available</div>
                ) : (
                    filteredLogsLimited.map((log, index) => {
                        const key = `${log.timestamp}-${index}`;
                        const bgClass = log.level === 'error' ? 'bg-red-900/40 text-red-200' :
                            log.level === 'warn' ? 'bg-yellow-900/40 text-yellow-200' :
                                log.level === 'info' ? 'bg-blue-900/30 text-blue-200' :
                                    'bg-gray-800/30 text-gray-200';

                        return (
                            <div key={key} className={`rounded-lg p-2 flex flex-col gap-1 ${bgClass}`}>
                                <div className="flex items-start justify-between gap-2">
                                    <div className="font-semibold text-[11px]">
                                        [{new Date(log.timestamp).toLocaleTimeString()}] {log.level.toUpperCase()}
                                    </div>
                                    <div className="flex items-center gap-1">
                                        <button
                                            onClick={() => handleCopyLog(log, key)}
                                            className="text-xs bg-gray-700/60 hover:bg-gray-700 px-2 py-0.5 rounded"
                                        >
                                            {copiedMap[key] ? 'Copied' : 'Copy'}
                                        </button>
                                    </div>
                                </div>
                                <div className="break-words text-sm text-gray-100">{log.message}</div>
                                {log.meta && (
                                    <div className="mt-1 text-gray-300 bg-gray-900/40 p-2 rounded text-[11px] overflow-auto">
                                        {typeof log.meta === 'string' ? log.meta : JSON.stringify(log.meta, null, 2)}
                                    </div>
                                )}
                            </div>
                        );
                    })
                )}
            </div>

            <style jsx>{`
                .debug-logger-scroll::-webkit-scrollbar { height:8px; width:8px; }
                .debug-logger-scroll::-webkit-scrollbar-thumb { background: rgba(148,163,184,0.24); border-radius: 999px; }
                .debug-logger-scroll::-webkit-scrollbar-track { background: transparent; }
            `}</style>

            {/* Footer */}
            <div className="bg-gray-800 px-3 py-1 rounded-b-lg text-xs text-gray-400">
                <div>API: {process.env.NEXT_PUBLIC_API_URL || 'Not configured'}</div>
                <div>Auth: {status === 'loading' ? 'Loading...' : status === 'authenticated' ? ' Logged in' : ' Not logged in'}</div>
                {session?.accessToken && (
                    <div>Token: {session.accessToken.substring(0, 20)}... ({session.accessToken.length} chars)</div>
                )}
            </div>
        </div>
    );
}

// Main wrapper component that handles conditional SessionProvider usage
export default function DebugLogger() {
    const pathname = usePathname();
    const isAdminRoute = pathname?.startsWith('/admin') || pathname?.startsWith('/admin-login');

    // For admin routes, don't use NextAuth session
    if (isAdminRoute) {
        return <DebugLoggerContent session={null} status="unauthenticated" />;
    }

    // For non-admin routes, use NextAuth session
    return <DebugLoggerWithSession />;
}

// Component that uses NextAuth session for non-admin routes
function DebugLoggerWithSession() {
    const { data: session, status } = useSession();
    return <DebugLoggerContent session={session} status={status} />;
}
