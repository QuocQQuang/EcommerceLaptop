// Role permissions templates for different user roles
import { PERMISSIONS } from './admin-api';

export const ROLE_PERMISSIONS: Record<string, string[]> = {
  Customer: [PERMISSIONS.PRODUCTS_READ],
  ProductAdmin: [
    PERMISSIONS.DASHBOARD_READ,
    PERMISSIONS.PRODUCTS_READ,
    PERMISSIONS.PRODUCTS_WRITE,
    PERMISSIONS.PRODUCTS_DELETE,
    PERMISSIONS.PRODUCTS_MANAGE,
  ],
  SalesAdmin: [
    PERMISSIONS.DASHBOARD_READ,
    PERMISSIONS.ORDERS_READ,
    PERMISSIONS.ORDERS_WRITE,
    PERMISSIONS.ORDERS_MANAGE,
    PERMISSIONS.USERS_READ,
  ],
  SystemAdmin: [
    ...Object.values(PERMISSIONS) // All permissions for SystemAdmin
  ],
};