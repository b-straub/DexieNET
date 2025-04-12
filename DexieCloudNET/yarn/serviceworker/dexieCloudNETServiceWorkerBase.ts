/// <reference lib="webworker" />
import {
    DexieCloudNETBroadcastIn, DexieCloudNETBroadcastOut,
    DexieCloudNETSubscriptionChanged,
    DexieCloudNETSkipWaiting, DexieCloudNETReloadPage, DexieCloudNETUpdateFound
} from "./dexieCloudNETSWBroadcast";

declare const self: ServiceWorkerGlobalScope

interface WebPushNotification {
    title: string,
    body: string,
    navigate: string,
    silent: boolean,
    tag?: string,
    app_badge?: number,
    icon?: string,
    lang?: string,
    dir?: string
}

const broadcastIn = new BroadcastChannel(DexieCloudNETBroadcastIn);
const broadcastOut = new BroadcastChannel(DexieCloudNETBroadcastOut);
const rootUrl = new URL('./', location.origin + location.pathname).href;

self.addEventListener('push', async event => {
    event.waitUntil(doPush(event));
});
self.addEventListener('notificationclick', async event => {
    event.waitUntil(doNotificationClick(event));
});

self.onpushsubscriptionchange = (event) => {
    broadcastOut.postMessage({type: DexieCloudNETSubscriptionChanged});
};

broadcastIn.onmessage = async (event) => {
    if (event.data) {
        console.log(`Service onMessage: ${event.data.type}`);

        switch (event.data.type) {
            case DexieCloudNETSkipWaiting:
                await self.skipWaiting();
                broadcastOut.postMessage({type: DexieCloudNETReloadPage});
                break;
        }
    }
};

async function doPush(event: PushEvent) {
    let webPushNotification: WebPushNotification = event?.data?.json().notification;

    if (!webPushNotification) {
        if (!webPushNotification) {
            console.log("PushEventRecord: no payload");
            return;
        }
    }

    if (self.navigator.setAppBadge) {
        await self.navigator.setAppBadge(webPushNotification.app_badge);
    }

    await self.registration.showNotification(webPushNotification.title, {
        body: webPushNotification.body,
        data: webPushNotification.navigate,
        tag: webPushNotification.tag,
        icon: webPushNotification.icon,
        requireInteraction: !webPushNotification.silent
    });
}

async function doNotificationClick(event: NotificationEvent) {
    event.notification.close();
    console.log(`Open client: ${event.notification.data}`);
    await self.clients.openWindow(event.notification.data);
}

export function notifyUpdate() {
    broadcastOut.postMessage({type: DexieCloudNETUpdateFound});
}