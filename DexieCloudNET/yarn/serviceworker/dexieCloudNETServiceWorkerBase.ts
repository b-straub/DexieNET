/// <reference lib="webworker" />
import {liveQuery, Observable} from "dexie";

import {
    BadgeEventsTableName, ClickedEventsTableName, DexieCloudNETBroadcastIn, DexieCloudNETBroadcastOut,
    DexieCloudNETSubscriptionChanged,
    DexieCloudNETSkipWaiting, DexieCloudNETReloadPage, DexieCloudNETUpdateFound, DexieCloudNETClickLock,
    PushEventRecord,
    PushEventsDB, 
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

interface NotificationData {
    setBadge: boolean,
    pushEvent: PushEventRecord
}

const broadcastIn = new BroadcastChannel(DexieCloudNETBroadcastIn);
const broadcastOut = new BroadcastChannel(DexieCloudNETBroadcastOut);
const rootUrl = new URL('./', location.origin + location.pathname).href;

self.addEventListener('push', async event => {
    event.waitUntil(doPush(event));
});

self.addEventListener('notificationclick', async function (event) {
    event.waitUntil(
        doNotificationClick(event)
    );
});

self.onpushsubscriptionchange = (event) => {
    broadcastOut.postMessage({
        type: DexieCloudNETSubscriptionChanged
    });
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

async function storeClickedEvent(pushEvent: PushEventRecord) {
    try {
        const clickedEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(ClickedEventsTableName);
        await clickedEventsTable.add(pushEvent);
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
            navigator.setAppBadge(count).then();
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

    const notificationData: NotificationData = {setBadge: pushNotification.setBadge === true, pushEvent: pushEvent};

    await self.registration.showNotification(pushNotification.title, {
        body: pushNotification.message,
        data: notificationData,
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
        broadcastOut.postMessage({type: DexieCloudNETClickLock, lock: true});
        event.preventDefault();
        event.notification.close();
        
        const notificationData: NotificationData | undefined = event.notification.data;
        if (!notificationData) {
            if (event.notification.title !== "Debug") {
                await ShowError("Notificationclick with no pushEvent data!");
            }
            return;
        }

        const matchedClients = await self.clients.matchAll({type: 'window', includeUncontrolled: true});
        
        let processed = false;
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
        
        notificationData.pushEvent.timeStampUtc = new Date(Date.now()).toISOString();
        await storeClickedEvent(notificationData.pushEvent);

        broadcastOut.postMessage({type: DexieCloudNETClickLock, lock: false});

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

