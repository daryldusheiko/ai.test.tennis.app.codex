// Development deliberately leaves requests uncached. Published builds use the offline worker.
self.addEventListener('fetch', () => {});
