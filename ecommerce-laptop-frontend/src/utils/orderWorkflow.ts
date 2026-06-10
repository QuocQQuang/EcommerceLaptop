// Order Workflow Management Utility
// Defines valid status transitions and validation logic

export type OrderStatus = 'pending' | 'confirmed' | 'processing' | 'shipped' | 'delivered' | 'cancelled';

export interface StatusTransition {
  status: OrderStatus;
  label: string;
  description: string;
  allowedFrom: OrderStatus[];
}

// Define the complete workflow with Vietnamese labels and descriptions
export const ORDER_WORKFLOW: Record<OrderStatus, StatusTransition> = {
  pending: {
    status: 'pending',
    label: 'Chờ xác nhận',
    description: 'Đơn hàng mới được tạo, chờ xác nhận',
    allowedFrom: [] // Initial status
  },
  confirmed: {
    status: 'confirmed',
    label: 'Đã xác nhận',
    description: 'Đơn hàng đã được xác nhận và sẵn sàng xử lý',
    allowedFrom: ['pending']
  },
  processing: {
    status: 'processing',
    label: 'Đang xử lý',
    description: 'Đang chuẩn bị hàng và đóng gói',
    allowedFrom: ['confirmed']
  },
  shipped: {
    status: 'shipped',
    label: 'Đang giao',
    description: 'Hàng đã được gửi đi và đang trên đường giao',
    allowedFrom: ['processing']
  },
  delivered: {
    status: 'delivered',
    label: 'Đã giao',
    description: 'Đơn hàng đã được giao thành công',
    allowedFrom: ['shipped']
  },
  cancelled: {
    status: 'cancelled',
    label: 'Đã hủy',
    description: 'Đơn hàng đã bị hủy',
    allowedFrom: ['pending', 'confirmed', 'processing'] // Cannot cancel after shipped
  }
};

/**
 * Get valid next statuses that can be transitioned to from current status
 */
export const getValidNextStatuses = (currentStatus: OrderStatus): StatusTransition[] => {
  return Object.values(ORDER_WORKFLOW).filter(transition =>
    transition.allowedFrom.includes(currentStatus)
  );
};

/**
 * Check if a status transition is valid
 */
export const isValidTransition = (fromStatus: OrderStatus, toStatus: OrderStatus): boolean => {
  const targetTransition = ORDER_WORKFLOW[toStatus];
  return targetTransition.allowedFrom.includes(fromStatus);
};

/**
 * Get user-friendly error message for invalid transitions
 */
export const getTransitionErrorMessage = (fromStatus: OrderStatus, toStatus: OrderStatus): string => {
  const fromLabel = ORDER_WORKFLOW[fromStatus]?.label || fromStatus;
  const toLabel = ORDER_WORKFLOW[toStatus]?.label || toStatus;
  
  if (!isValidTransition(fromStatus, toStatus)) {
    const validNext = getValidNextStatuses(fromStatus);
    
    if (validNext.length === 0) {
      return `Không thể thay đổi trạng thái từ "${fromLabel}". Đây là trạng thái cuối cùng.`;
    }
    
    const validLabels = validNext.map(t => `"${t.label}"`).join(', ');
    return `Không thể chuyển từ "${fromLabel}" sang "${toLabel}". Các trạng thái hợp lệ tiếp theo: ${validLabels}`;
  }
  
  return '';
};

/**
 * Get workflow step information for UI display
 */
export const getWorkflowSteps = (): StatusTransition[] => {
  // Return in logical order for progress display
  return [
    ORDER_WORKFLOW.pending,
    ORDER_WORKFLOW.confirmed,
    ORDER_WORKFLOW.processing,
    ORDER_WORKFLOW.shipped,
    ORDER_WORKFLOW.delivered
  ];
};

/**
 * Get completion percentage based on current status
 */
export const getStatusProgress = (status: OrderStatus): number => {
  const progressMap: Record<OrderStatus, number> = {
    pending: 0,
    confirmed: 20,
    processing: 40,
    shipped: 80,
    delivered: 100,
    cancelled: 0
  };
  
  return progressMap[status] || 0;
};

/**
 * Check if status represents a final state
 */
export const isFinalStatus = (status: OrderStatus): boolean => {
  return status === 'delivered' || status === 'cancelled';
};
