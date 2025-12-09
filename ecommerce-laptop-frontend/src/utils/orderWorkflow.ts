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
    label: 'Ch xc nhn',
    description: 'n hng mi c to, ch xc nhn',
    allowedFrom: [] // Initial status
  },
  confirmed: {
    status: 'confirmed',
    label: ' xc nhn',
    description: 'n hng  c xc nhn v sn sng x l',
    allowedFrom: ['pending']
  },
  processing: {
    status: 'processing',
    label: 'ang x l',
    description: 'ang chun b hng v ng gi',
    allowedFrom: ['confirmed']
  },
  shipped: {
    status: 'shipped',
    label: 'ang giao',
    description: 'Hng  c gi i v ang trn ng giao',
    allowedFrom: ['processing']
  },
  delivered: {
    status: 'delivered',
    label: ' giao',
    description: 'n hng  c giao thnh cng',
    allowedFrom: ['shipped']
  },
  cancelled: {
    status: 'cancelled',
    label: ' hy',
    description: 'n hng  b hy',
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
      return `Khng th thay i trng thi t "${fromLabel}". y l trng thi cui cng.`;
    }
    
    const validLabels = validNext.map(t => `"${t.label}"`).join(', ');
    return `Khng th chuyn t "${fromLabel}" sang "${toLabel}". Cc trng thi hp l tip theo: ${validLabels}`;
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