/**
 * Service Worker for RS_system PWA
 * Implements offline-first architecture with strategic caching
 * Version: 1.0.0
 */

const CACHE_VERSION = 'rs-system-v1.0.0';
const STATIC_CACHE = `${CACHE_VERSION}-static`;
const DYNAMIC_CACHE = `${CACHE_VERSION}-dynamic`;
const API_CACHE = `${CACHE_VERSION}-api`;

// Critical resources to cache on install
const STATIC_ASSETS = [
    '/',
    '/Home/Index',
    '/Colaboracion/Create',
    '/Colaboracion/Index',
    '/css/site.css',
    '/css/bootstrap.min.css',
    '/css/bootstrap-icons.min.css',
    '/js/site.js',
    '/js/colaboraciones-offline-db.js',
    '/js/colaboraciones-sync.js',
    '/lib/jquery/dist/jquery.min.js',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/manifest.json',
    '/Assets/icon-192x192.png',
    '/Assets/icon-512x512.png'
];

// Install event - cache static assets
self.addEventListener('install', (event) => {
    console.log('[Service Worker] Installing...');
    
    event.waitUntil(
        caches.open(STATIC_CACHE)
            .then((cache) => {
                console.log('[Service Worker] Caching static assets');
                return cache.addAll(STATIC_ASSETS);
            })
            .then(() => {
                console.log('[Service Worker] Installation complete');
                return self.skipWaiting(); // Activate immediately
            })
            .catch((error) => {
                console.error('[Service Worker] Installation failed:', error);
            })
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
    console.log('[Service Worker] Activating...');
    
    event.waitUntil(
        caches.keys()
            .then((cacheNames) => {
                return Promise.all(
                    cacheNames
                        .filter((name) => {
                            // Delete old version caches
                            return name.startsWith('rs-system-') && name !== STATIC_CACHE && name !== DYNAMIC_CACHE && name !== API_CACHE;
                        })
                        .map((name) => {
                            console.log('[Service Worker] Deleting old cache:', name);
                            return caches.delete(name);
                        })
                );
            })
            .then(() => {
                console.log('[Service Worker] Activation complete');
                return self.clients.claim(); // Take control immediately
            })
    );
});

// Fetch event - implement caching strategies
self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);
    
    // Skip chrome extension and non-HTTP requests
    if (!url.protocol.startsWith('http')) {
        return;
    }
    
    // API requests - Network First, fallback to offline indicator
    if (url.pathname.includes('/api/') || 
        url.pathname.includes('/Colaboracion/Sync') ||
        url.pathname.includes('/Colaboracion/BuscarMiembros') ||
        url.pathname.includes('/Colaboracion/ObtenerUltimosPagos')) {
        
        event.respondWith(networkFirstStrategy(request, API_CACHE));
        return;
    }
    
    // POST requests - Network Only (never cache)
    if (request.method === 'POST') {
        event.respondWith(
            fetch(request).catch(() => {
                return new Response(
                    JSON.stringify({ 
                        success: false, 
                        offline: true, 
                        message: 'Sin conexión. Por favor intente más tarde.' 
                    }),
                    { 
                        headers: { 'Content-Type': 'application/json' },
                        status: 503
                    }
                );
            })
        );
        return;
    }
    
    // Static assets - Cache First, fallback to Network
    if (isStaticAsset(url.pathname)) {
        event.respondWith(cacheFirstStrategy(request, STATIC_CACHE));
        return;
    }
    
    // Dynamic content (HTML pages) - Network First, fallback to Cache
    event.respondWith(networkFirstStrategy(request, DYNAMIC_CACHE));
});

/**
 * Cache First Strategy
 * Try cache first, fallback to network, then cache the response
 */
function cacheFirstStrategy(request, cacheName) {
    return caches.match(request)
        .then((cachedResponse) => {
            if (cachedResponse) {
                return cachedResponse;
            }
            
            return fetch(request)
                .then((networkResponse) => {
                    // Clone the response
                    const responseToCache = networkResponse.clone();
                    
                    caches.open(cacheName)
                        .then((cache) => {
                            cache.put(request, responseToCache);
                        });
                    
                    return networkResponse;
                })
                .catch((error) => {
                    console.error('[Service Worker] Fetch failed:', error);
                    // Return offline page if available
                    return caches.match('/offline.html') || new Response('Offline');
                });
        });
}

/**
 * Network First Strategy
 * Try network first, fallback to cache
 */
function networkFirstStrategy(request, cacheName) {
    return fetch(request)
        .then((networkResponse) => {
            // Clone and cache the response
            const responseToCache = networkResponse.clone();
            
            caches.open(cacheName)
                .then((cache) => {
                    cache.put(request, responseToCache);
                });
            
            return networkResponse;
        })
        .catch((error) => {
            console.log('[Service Worker] Network failed, trying cache:', error);
            
            return caches.match(request)
                .then((cachedResponse) => {
                    if (cachedResponse) {
                        return cachedResponse;
                    }
                    
                    // If API request and no cache, return offline indicator
                    if (request.url.includes('/api/') || request.url.includes('/Colaboracion/')) {
                        return new Response(
                            JSON.stringify({ offline: true }),
                            { 
                                headers: { 'Content-Type': 'application/json' },
                                status: 503
                            }
                        );
                    }
                    
                    throw error;
                });
        });
}

/**
 * Check if request is for a static asset
 */
function isStaticAsset(pathname) {
    const staticExtensions = ['.css', '.js', '.jpg', '.jpeg', '.png', '.gif', '.svg', '.woff', '.woff2', '.ttf', '.eot', '.ico'];
    return staticExtensions.some(ext => pathname.endsWith(ext));
}

// Background Sync for future enhancement
self.addEventListener('sync', (event) => {
    console.log('[Service Worker] Background sync triggered:', event.tag);
    
    if (event.tag === 'sync-colaboraciones') {
        event.waitUntil(
            // This will be handled by colaboraciones-sync.js
            self.registration.showNotification('Sincronización completada', {
                body: 'Las colaboraciones offline se han sincronizado exitosamente.',
                icon: '/Assets/icon-192x192.png',
                badge: '/Assets/icon-192x192.png'
            })
        );
    }
});

// Message handler for cache updates
self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
    
    if (event.data && event.data.type === 'CLEAR_CACHE') {
        event.waitUntil(
            caches.keys().then((cacheNames) => {
                return Promise.all(
                    cacheNames.map((cacheName) => caches.delete(cacheName))
                );
            })
        );
    }
});

console.log('[Service Worker] Loaded and ready');
