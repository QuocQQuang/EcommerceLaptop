const CACHE_NAME = 'laptop-store-v1';
const urlsToCache = [
    '/',
    '/products',
    '/auth/login',
    '/cart',
    '/static/js/bundle.js',
    '/static/css/main.css',
    '/manifest.json'
];

// Install service worker
self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(function (cache) {
                console.log('Opened cache');
                return cache.addAll(urlsToCache);
            })
    );
});

// Fetch event
self.addEventListener('fetch', function (event) {
    event.respondWith(
        caches.match(event.request)
            .then(function (response) {
                // Return cached version or fetch from network
                if (response) {
                    return response;
                }
                return fetch(event.request);
            }
            )
    );
});

// Activate service worker
self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(function (cacheNames) {
            return Promise.all(
                cacheNames.map(function (cacheName) {
                    if (cacheName !== CACHE_NAME) {
                        console.log('Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
});

// Push notification event
self.addEventListener('push', function (event) {
    const options = {
        body: event.data ? event.data.text() : 'New notification from Laptop Store',
        icon: '/icon-192x192.png',
        badge: '/icon-72x72.png',
        vibrate: [100, 50, 100],
        data: {
            dateOfArrival: Date.now(),
            primaryKey: 1
        },
        actions: [
            {
                action: 'explore',
                title: 'Xem ngay',
                icon: '/icon-check.png'
            },
            {
                action: 'close',
                title: 'ng',
                icon: '/icon-close.png'
            }
        ]
    };

    event.waitUntil(
        self.registration.showNotification('Laptop Store', options)
    );
});

// Notification click event
self.addEventListener('notificationclick', function (event) {
    event.notification.close();

    if (event.action === 'explore') {
        // Open the app
        event.waitUntil(
            clients.openWindow('/')
        );
    } else if (event.action === 'close') {
        // Just close the notification
        event.notification.close();
    } else {
        // Default action - open the app
        event.waitUntil(
            clients.openWindow('/')
        );
    }
});

// Background sync for offline cart persistence
self.addEventListener('sync', function (event) {
    if (event.tag === 'cart-sync') {
        event.waitUntil(syncCart());
    }
});

async function syncCart() {
    try {
        // Get cart data from IndexedDB or localStorage
        const cartData = await getStoredCartData();

        if (cartData && cartData.length > 0) {
            // Sync with server when online
            await fetch('/api/cart/sync', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(cartData)
            });
        }
    } catch (error) {
        console.log('Cart sync failed:', error);
    }
}

async function getStoredCartData() {
    // Implementation would depend on your storage strategy
    // This is a placeholder
    return [];
}