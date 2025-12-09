/**
 * Utility functions for user-related operations
 */

/**
 * Safely converts user ID to number for comparison
 * @param userId - User ID from session (can be string or number)
 * @returns Parsed user ID as number, or 0 if invalid
 */
export function parseUserId(userId: string | number | undefined): number {
    if (typeof userId === 'number') {
        return userId;
    }

    if (typeof userId === 'string') {
        const parsed = parseInt(userId, 10);
        return isNaN(parsed) ? 0 : parsed;
    }

    return 0;
}

/**
 * Checks if user has access to an order
 * @param userId - User ID from session
 * @param orderUserId - User ID from order
 * @returns true if user has access, false otherwise
 */
export function hasOrderAccess(userId: string | number | undefined, orderUserId: number): boolean {
    const currentUserId = parseUserId(userId);
    return currentUserId > 0 && orderUserId === currentUserId;
}

/**
 * Validates user ID format and returns normalized value
 * @param userId - User ID to validate
 * @returns Object with isValid flag and normalized userId
 */
export function validateUserId(userId: string | number | undefined): {
    isValid: boolean;
    userId: number;
    originalValue: string | number | undefined;
} {
    const normalizedUserId = parseUserId(userId);

    return {
        isValid: normalizedUserId > 0,
        userId: normalizedUserId,
        originalValue: userId
    };
}
