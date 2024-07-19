// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).

import { notifyUpdate } from './_content/DexieCloudNET/js/dexieCloudNETServiceWorker.js';

self.addEventListener('fetch', () => {
});

self.addEventListener('install', async () => {
    const { installing, waiting, active } = await self.registration;
    if (waiting || active) {
        notifyUpdate();
    }
});