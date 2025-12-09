import { CartItem } from '@/types/api';

const CART_STORAGE_KEY = 'laptop-store-cart';
const OFFLINE_ACTIONS_KEY = 'laptop-store-offline-actions';

export interface OfflineAction {
    id: string;
    type: 'ADD_TO_CART' | 'UPDATE_QUANTITY' | 'REMOVE_FROM_CART' | 'CLEAR_CART';
    data: unknown;
    timestamp: number;
}

// IndexedDB wrapper for cart persistence
class CartStorage {
    private dbName = 'LaptopStoreDB';
    private dbVersion = 1;
    private storeName = 'cart';

    async initDB(): Promise<IDBDatabase> {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.dbVersion);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result);

            request.onupgradeneeded = (event) => {
                const db = (event.target as IDBOpenDBRequest).result;

                if (!db.objectStoreNames.contains(this.storeName)) {
                    const store = db.createObjectStore(this.storeName, { keyPath: 'id' });
                    store.createIndex('productId', 'productId', { unique: false });
                    store.createIndex('timestamp', 'timestamp', { unique: false });
                }
            };
        });
    }

    async saveCartItems(items: CartItem[]): Promise<void> {
        try {
            const db = await this.initDB();
            const transaction = db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);

            // Clear existing items
            await store.clear();

            // Add new items
            for (const item of items) {
                await store.add({
                    ...item,
                    timestamp: Date.now()
                });
            }

            return new Promise<void>((resolve, reject) => {
                transaction.oncomplete = () => resolve();
                transaction.onerror = () => reject(transaction.error);
            });
        } catch (error) {
            console.error('Failed to save cart to IndexedDB:', error);
            // Fallback to localStorage
            this.saveToLocalStorage(items);
        }
    }

    async loadCartItems(): Promise<CartItem[]> {
        try {
            const db = await this.initDB();
            const transaction = db.transaction([this.storeName], 'readonly');
            const store = transaction.objectStore(this.storeName);
            const request = store.getAll();

            return new Promise((resolve, reject) => {
                request.onsuccess = () => {
                    const items = request.result.map(item => ({
                        id: item.id,
                        productId: item.productId,
                        product: item.product,
                        quantity: item.quantity,
                        unitPrice: item.unitPrice,
                        totalPrice: item.totalPrice
                    }));
                    resolve(items);
                };
                request.onerror = () => reject(request.error);
            });
        } catch (error) {
            console.error('Failed to load cart from IndexedDB:', error);
            // Fallback to localStorage
            return this.loadFromLocalStorage();
        }
    }

    private saveToLocalStorage(items: CartItem[]): void {
        try {
            localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
        } catch (error) {
            console.error('Failed to save cart to localStorage:', error);
        }
    }

    private loadFromLocalStorage(): CartItem[] {
        try {
            const stored = localStorage.getItem(CART_STORAGE_KEY);
            return stored ? JSON.parse(stored) : [];
        } catch (error) {
            console.error('Failed to load cart from localStorage:', error);
            return [];
        }
    }

    async clearCart(): Promise<void> {
        try {
            const db = await this.initDB();
            const transaction = db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            await store.clear();
        } catch (error) {
            console.error('Failed to clear cart from IndexedDB:', error);
        }

        localStorage.removeItem(CART_STORAGE_KEY);
    }
}

// Offline action queue
class OfflineActionQueue {
    private actions: OfflineAction[] = [];

    constructor() {
        this.loadActions();
    }

    addAction(type: OfflineAction['type'], data: unknown): void {
        const action: OfflineAction = {
            id: Date.now().toString() + Math.random().toString(36).substr(2, 9),
            type,
            data,
            timestamp: Date.now()
        };

        this.actions.push(action);
        this.saveActions();

        // Register background sync if available
        if ('serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype) {
            navigator.serviceWorker.ready.then(registration => {
                return (registration as unknown as { sync: { register: (tag: string) => Promise<void> } }).sync.register('cart-sync');
            }).catch(error => {
                console.log('Background sync registration failed:', error);
            });
        }
    }

    getActions(): OfflineAction[] {
        return [...this.actions];
    }

    clearActions(): void {
        this.actions = [];
        this.saveActions();
    }

    removeAction(actionId: string): void {
        this.actions = this.actions.filter(action => action.id !== actionId);
        this.saveActions();
    }

    private saveActions(): void {
        try {
            localStorage.setItem(OFFLINE_ACTIONS_KEY, JSON.stringify(this.actions));
        } catch (error) {
            console.error('Failed to save offline actions:', error);
        }
    }

    private loadActions(): void {
        try {
            const stored = localStorage.getItem(OFFLINE_ACTIONS_KEY);
            this.actions = stored ? JSON.parse(stored) : [];
        } catch (error) {
            console.error('Failed to load offline actions:', error);
            this.actions = [];
        }
    }
}

// Offline cart manager
export class OfflineCartManager {
    private storage = new CartStorage();
    private actionQueue = new OfflineActionQueue();

    async saveCart(items: CartItem[]): Promise<void> {
        await this.storage.saveCartItems(items);
    }

    async loadCart(): Promise<CartItem[]> {
        return await this.storage.loadCartItems();
    }

    async clearCart(): Promise<void> {
        await this.storage.clearCart();
    }

    addOfflineAction(type: OfflineAction['type'], data: any): void {
        this.actionQueue.addAction(type, data);
    }

    async syncWithServer(): Promise<void> {
        if (!navigator.onLine) {
            console.log('Cannot sync: offline');
            return;
        }

        const actions = this.actionQueue.getActions();
        if (actions.length === 0) {
            return;
        }

        try {
            // Send actions to server
            const response = await fetch('/api/cart/sync', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ actions })
            });

            if (response.ok) {
                this.actionQueue.clearActions();
                console.log('Cart synced successfully');
            } else {
                console.error('Cart sync failed:', response.statusText);
            }
        } catch (error) {
            console.error('Cart sync error:', error);
        }
    }

    // Initialize offline functionality
    async initialize(): Promise<CartItem[]> {
        // Load cart from storage on app start
        const storedCart = await this.loadCart();

        // Listen for online events to sync
        window.addEventListener('online', () => {
            this.syncWithServer();
        });

        return storedCart;
    }
}

// Singleton instance
export const offlineCartManager = new OfflineCartManager();

// Utility functions
export function isOnline(): boolean {
    return navigator.onLine;
}

export function getConnectionType(): string {
    const connection = (navigator as unknown as { connection?: { effectiveType: string } }).connection || (navigator as unknown as { mozConnection?: { effectiveType: string } }).mozConnection || (navigator as unknown as { webkitConnection?: { effectiveType: string } }).webkitConnection;
    return connection ? connection.effectiveType : 'unknown';
}

export function shouldUseOfflineMode(): boolean {
    if (!isOnline()) return true;

    const connectionType = getConnectionType();
    // Use offline mode for slow connections
    return connectionType === 'slow-2g' || connectionType === '2g';
}