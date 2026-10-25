self.importScripts('./service-worker-assets.js');
const base = new URL('./', self.location.href);
const cachePrefix = 'social-tennis-' + base.pathname + '-';
const cacheName = cachePrefix + self.assetsManifest.version;
const indexUrl = new URL('index.html', base).href;
const assets = self.assetsManifest.assets.filter(asset => !asset.url.endsWith('.br') && !asset.url.endsWith('.gz') && asset.url !== 'service-worker.js');
self.addEventListener('install', event => event.waitUntil((async () => {
    const cache = await caches.open(cacheName);
    await cache.addAll(assets.map(asset => new Request(new URL(asset.url, base), { integrity: asset.hash, cache: 'no-cache' })));
    // The page reloads when a complete update takes control; device data remains untouched.
    await self.skipWaiting();
})()));
self.addEventListener('activate', event => event.waitUntil((async () => {
    for (const key of await caches.keys()) {
        if (key.startsWith(cachePrefix) && key !== cacheName) await caches.delete(key);
    }
    await self.clients.claim();
})()));
self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;
    const url = new URL(event.request.url);
    if (url.origin !== base.origin || !url.pathname.startsWith(base.pathname)) return;
    event.respondWith((async () => {
        const cache = await caches.open(cacheName);
        if (event.request.mode === 'navigate') return await cache.match(indexUrl) || fetch(event.request);
        return await cache.match(event.request) || fetch(event.request);
    })());
});
