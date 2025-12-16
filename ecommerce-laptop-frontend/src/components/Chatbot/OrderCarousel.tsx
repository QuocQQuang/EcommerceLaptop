'use client';

import { Order } from '@/types/chat';
import { Package, ChevronLeft, ChevronRight, Clock, CheckCircle, XCircle } from 'lucide-react';
import { useState } from 'react';

interface OrderCarouselProps {
    orders: Order[];
}

export function OrderCarousel({ orders }: OrderCarouselProps) {
    const [currentPage, setCurrentPage] = useState(0);
    const itemsPerPage = 2; // Show 2 orders at a time
    const totalPages = Math.ceil(orders.length / itemsPerPage);

    const handlePrevious = () => {
        setCurrentPage((prev) => Math.max(0, prev - 1));
    };

    const handleNext = () => {
        setCurrentPage((prev) => Math.min(totalPages - 1, prev + 1));
    };

    if (!orders || orders.length === 0) return null;

    const startIndex = currentPage * itemsPerPage;
    const visibleOrders = orders.slice(startIndex, startIndex + itemsPerPage);

    const getStatusColor = (status: string) => {
        const s = status.toLowerCase();
        if (s.includes('delivered') || s.includes('shipped') || s.includes('completed')) return 'text-green-600 bg-green-50 border-green-200';
        if (s.includes('pending') || s.includes('processing')) return 'text-amber-600 bg-amber-50 border-amber-200';
        if (s.includes('cancelled')) return 'text-red-600 bg-red-50 border-red-200';
        return 'text-neutral-600 bg-neutral-50 border-neutral-200';
    };

    const getStatusIcon = (status: string) => {
        const s = status.toLowerCase();
        if (s.includes('delivered') || s.includes('completed')) return <CheckCircle size={14} />;
        if (s.includes('cancelled')) return <XCircle size={14} />;
        return <Clock size={14} />;
    };

    return (
        <div className="relative w-full max-w-md">
            {/* Header */}
            <div className="flex items-center gap-2 mb-2 px-1">
                <Package className="w-4 h-4 text-neutral-500" />
                <span className="text-xs font-medium text-neutral-500">
                    {orders.length} Order{orders.length > 1 ? 's' : ''} Found
                </span>
            </div>

            {/* Navigation Buttons */}
            {totalPages > 1 && (
                <>
                    <button
                        onClick={handlePrevious}
                        disabled={currentPage === 0}
                        className="absolute left-[-12px] top-1/2 -translate-y-1/2 z-10 bg-white hover:bg-neutral-50 border border-neutral-200 rounded-full p-1 shadow-sm disabled:opacity-0 transition-all"
                        aria-label="Previous orders"
                    >
                        <ChevronLeft className="w-3 h-3 text-neutral-600" />
                    </button>
                    <button
                        onClick={handleNext}
                        disabled={currentPage === totalPages - 1}
                        className="absolute right-[-12px] top-1/2 -translate-y-1/2 z-10 bg-white hover:bg-neutral-50 border border-neutral-200 rounded-full p-1 shadow-sm disabled:opacity-0 transition-all"
                        aria-label="Next orders"
                    >
                        <ChevronRight className="w-3 h-3 text-neutral-600" />
                    </button>
                </>
            )}

            {/* Orders Grid */}
            <div className="flex gap-2 overflow-hidden py-1">
                {visibleOrders.map((order) => (
                    <div
                        key={order.id}
                        className="flex-1 min-w-[200px] bg-white border border-neutral-200 rounded-xl p-3 shadow-sm hover:shadow-md transition-all flex flex-col"
                    >
                        <div className="flex justify-between items-start mb-2">
                            <div>
                                <h4 className="font-semibold text-sm text-neutral-900">Order #{order.id}</h4>
                                <span className="text-[10px] text-neutral-500">
                                    {new Date(order.createdAt).toLocaleDateString()}
                                </span>
                            </div>
                            <div className={`flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-medium border ${getStatusColor(order.status)}`}>
                                {getStatusIcon(order.status)}
                                <span>{order.status}</span>
                            </div>
                        </div>

                        <div className="flex-1 border-t border-neutral-100 py-2 my-1 space-y-1">
                            {order.items && order.items.slice(0, 2).map((item, idx) => (
                                <p key={idx} className="text-xs text-neutral-600 truncate">
                                     {item}
                                </p>
                            ))}
                            {order.items && order.items.length > 2 && (
                                <p className="text-[10px] text-neutral-400 italic">
                                    + {order.items.length - 2} more items
                                </p>
                            )}
                        </div>

                        <div className="flex justify-between items-center pt-2 border-t border-neutral-100">
                            <span className="text-xs text-neutral-500">{order.itemCount} items</span>
                            <span className="font-semibold text-sm text-neutral-900">
                                ${order.totalAmount.toLocaleString()}
                            </span>
                        </div>
                    </div>
                ))}
            </div>

            {/* Pagination Dots */}
            {totalPages > 1 && (
                <div className="flex justify-center gap-1 mt-2">
                    {Array.from({ length: totalPages }).map((_, index) => (
                        <button
                            key={index}
                            onClick={() => setCurrentPage(index)}
                            className={`w-1 h-1 rounded-full transition-all ${index === currentPage
                                ? 'bg-neutral-900 w-3'
                                : 'bg-neutral-300 hover:bg-neutral-400'
                                }`}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}
