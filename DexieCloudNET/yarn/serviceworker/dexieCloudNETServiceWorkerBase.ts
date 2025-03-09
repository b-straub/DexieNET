/// <reference lib="webworker" />
import {
    BadgeEventsTableName, ClickedEventsTableName, DexieCloudNETBroadcastIn, DexieCloudNETBroadcastOut,
    DexieCloudNETSubscriptionChanged,
    DexieCloudNETSkipWaiting, DexieCloudNETReloadPage, DexieCloudNETUpdateFound, DexieCloudNETClickLock,
    PushEventRecord,
    PushEventsDB
} from "./dexieCloudNETSWBroadcast";

declare const self: ServiceWorkerGlobalScope

interface PushNotificationRecord {
    title: string,
    message: string,
    icon: string,
    requireInteraction: boolean,
    setBadge?: boolean,
    payloadJson?: string,
    tag?: string,
}

const broadcastIn = new BroadcastChannel(DexieCloudNETBroadcastIn);
const broadcastOut = new BroadcastChannel(DexieCloudNETBroadcastOut);
const rootUrl = new URL('./', location.origin + location.pathname).href;

self.addEventListener('push', async event => {
    event.waitUntil(doPush(event));
});
self.addEventListener('notificationclick', async event => {
    let isSafari = /^((?!chrome|android).)*safari/i.test(navigator.userAgent);
    if (!isSafari) {
        event.waitUntil(doNotificationClick(event));
    }
    else {
        await storeClickedEvent(event);
    }
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

async function storeClickedEvent(event: NotificationEvent) {
    try {
        const pushEvent: PushEventRecord | undefined = event.notification.data;
        if (!pushEvent) {
            if (event.notification.title !== "Debug") {
                await ShowError("Notificationclick with no pushEvent data!");
            }
            return;
        }

        await navigator.locks.request(DexieCloudNETClickLock, async () => {
            const clickedEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(ClickedEventsTableName);
            await clickedEventsTable.add(pushEvent);
        });
    } catch (error) {
        await ShowError(error);
    }
}

async function storeBadgeEvent(pushEvent: PushEventRecord) {
    try {
        if (pushEvent.tag) {
            const badgeEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(BadgeEventsTableName);
            const keys = (await badgeEventsTable.where({tag: pushEvent.tag}).toArray())
                .filter(e => e.id !== undefined)
                .map(e => e.id!);

            await badgeEventsTable.bulkDelete(keys);
        }

        const badgeEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(BadgeEventsTableName);
        await badgeEventsTable.add(pushEvent);

        if (navigator.setAppBadge) {
            const count = await badgeEventsTable.count();
            await navigator.setAppBadge(count);
        }
    } catch (error) {
        await ShowError(error);
    }
}

async function doPush(event: PushEvent) {
    const pushNotification: PushNotificationRecord = event?.data?.json();

    if (!pushNotification) {
        console.log("PushEventRecord: no payload");
        return;
    }

    const pushEvent: PushEventRecord = {
        payloadJson: pushNotification.payloadJson,
        timeStampUtc: new Date(Date.now()).toISOString(),
        tag: pushNotification.tag,
        id: undefined
    }

    await self.registration.showNotification(pushNotification.title, {
        body: pushNotification.message,
        data: pushEvent,
        icon: pushNotification.icon,
        tag: pushNotification.tag,
        requireInteraction: pushNotification.requireInteraction
    });

    if (pushNotification.setBadge === true) {
        await storeBadgeEvent(pushEvent);
    }
}

async function doNotificationClick(event: NotificationEvent) {
    try {
        event.notification.close();
        let processed = false;

        const matchedClients = await self.clients.matchAll({type: 'window', includeUncontrolled: true});

        for (let client of matchedClients) {
            if (client.url.indexOf(rootUrl) >= 0) {
                try {
                    if (client.focus) {
                        if (!client.focused) {
                            await client.focus();
                            console.log(`Focus client: ${rootUrl}`);
                        }
                        processed = true;
                        break;
                    }
                } catch (error) {
                    const errorMessage = error instanceof Error ? error.message : "focus error";
                    console.log(errorMessage);
                }
            }
        }

        if (!processed && self.clients.openWindow) {
            console.log(`Open client: ${rootUrl}`);
            const window = await self.clients.openWindow(rootUrl);
            if (window) {
                console.log(`Has window of type: ${window.type}`);
            }
        }

        await storeClickedEvent(event);
        
    } catch (error) {
        await ShowError(error);
    }
}

async function ShowError(error: unknown) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    await self.registration.showNotification("Debug", {
        body: errorMessage,
        icon: 'icon-512.png',
        requireInteraction: true
    });
}

export function notifyUpdate() {
    broadcastOut.postMessage({type: DexieCloudNETUpdateFound});
}