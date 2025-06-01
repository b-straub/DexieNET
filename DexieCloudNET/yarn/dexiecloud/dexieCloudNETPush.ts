/*
dexieCloudNETPush.ts

Copyright(c) 2024 Bernhard Straub

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

'DexieNET' used with permission of David Fahlander 
*/

import {CloudDB, CurrentDB} from "./dexieCloudNETBase";

import {BehaviorSubject, Subscription, Observable, from, switchMap} from 'rxjs';
import {md5} from 'js-md5';
import {DotNetObservable} from "./dexieCloudNETObservables";
import {UserLogin} from "dexie-cloud-addon";
import {liveQuery} from 'dexie';
import {
    DexieCloudNETBroadcastIn, DexieCloudNETBroadcastOut, DexieCloudNETReloadPage,
    DexieCloudNETSkipWaiting, DexieCloudNETSubscriptionChanged, DexieCloudNETUpdateFound
} from "../serviceworker/dexieCloudNETSWBroadcast";

interface PushInformation {
    applicationServerKey: string,
    subscriptionId: string | null | undefined,
    id: number
}

interface DexieCloudPushSubscription {
    id: string,
    expired: boolean,
    pushURL: string,
    declarative: boolean,
    subscription: PushSubscriptionJSON
}

interface DexieCloudPushNotification {
    id: string,
    expired: boolean
}

enum NotificationState {
    None,
    Denied,
    Unsubscribed,
    UnsubscribedRemote,
    Subscribed,
}

enum ServiceWorkerState {
    None,
    UpdateFound,
    ReloadPage
}


interface ServiceWorkerNotification {
    state: ServiceWorkerState,
}

const ServiceWorkerNotificationNone: ServiceWorkerNotification = {
    state: ServiceWorkerState.None,
};

const PushSubscriptionsTableName: string = "pushSubscriptions";
const PushNotificationsTableName: string = "pushNotifications";
const PushInformationsTableName: string = "pushInformations";
const PushInformationKey: number = 1;

const NotificationStateSubject: BehaviorSubject<NotificationState> = new BehaviorSubject<NotificationState>(NotificationState.None);
const ServiceWorkerNotificationSubject: BehaviorSubject<ServiceWorkerNotification> = new BehaviorSubject<ServiceWorkerNotification>(
    ServiceWorkerNotificationNone);

const BroadcastIn = new BroadcastChannel(DexieCloudNETBroadcastOut);
const BroadcastOut = new BroadcastChannel(DexieCloudNETBroadcastIn);

let worker = await navigator.serviceWorker.getRegistration();
// @ts-ignore
let pushManager: PushManager | undefined = window.pushManager !== undefined ? window.pushManager : worker?.pushManager;
let SubscriptionCountSubscription: Subscription | undefined = undefined;
let PushURL: string | undefined = undefined;

export async function SubscribePush(): Promise<boolean> {
    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    let subscription = await pushManager?.getSubscription();

    if (!subscription) {
        let pushInformation = await GetPushInformation();

        if (!pushInformation?.applicationServerKey) {
            throw ("ApplicationServerKey is null or undefined or invalid")
        }

        try {
            const options: PushSubscriptionOptionsInit = {
                userVisibleOnly: true,
                applicationServerKey: pushInformation.applicationServerKey
            };

            subscription = await pushManager?.subscribe(options);
        } catch (error) {
            if (error.name === 'NotAllowedError') {
                return false;
            }
            throw error;
        }
    }

    await UpdatePushDatabases(false, false);
    return subscription != null;
}

export async function UnSubscribePush(remote: boolean = false): Promise<boolean> {
    const subscription = await pushManager?.getSubscription();

    if (subscription) {
        await subscription.unsubscribe();
        await UpdatePushDatabases(false, remote);
        ServiceWorkerNotificationSubject.next(ServiceWorkerNotificationNone);
        return true;
    }

    return false;
}

export async function AskForNotificationPermission(): Promise<boolean> {

    if (Notification.permission !== "granted" && Notification.permission !== "denied") {
        await Notification.requestPermission();
    }

    if (Notification.permission === "denied") {
        NotificationStateSubject.next(NotificationState.Denied);
    }

    return Notification.permission === "granted";
}

export async function SetPushNotificationState(ul: UserLogin, pushURL: string, applicationServerKey: string) {
    if (!ul.isLoggedIn) {
        NotificationStateSubject.next(NotificationState.None);
        return;
    }

    if (Notification.permission === "denied") {
        NotificationStateSubject.next(NotificationState.Denied);
        return;
    }

    PushURL = pushURL;
    await InitPushInformation(applicationServerKey);
    await UpdatePushDatabases(true, false);
}

export function SetPushStatesToNone() {
    NotificationStateSubject.next(NotificationState.None);
    ServiceWorkerNotificationSubject.next(ServiceWorkerNotificationNone);
}

// sync with DexieCloudNETAccess.cs - PushNotificationState
enum PushNotificationState {
    Valid,
    Invalid,
    Expired
}

// sync with DexieCloudNETPush.cs
export function SubscribeNotificationState(_: CloudDB, dotnetRef: any): Subscription {
    const action = (res: NotificationState) => {
        return res;
    };

    return DotNetObservable(NotificationStateSubject, action, dotnetRef);
}

export function SubscribeServiceWorkerNotification(_: CloudDB, dotnetRef: any): Subscription {

    const action = (res: ServiceWorkerNotification) => {
        console.log(`ServiceWorkerNotification: ${res.state}`);
        return res;
    };

    return DotNetObservable(ServiceWorkerNotificationSubject, action, dotnetRef);
}

async function UpdatePushDatabases(initial: boolean, remote: boolean) {
    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    let pushInformation = await GetPushInformation();

    if (!pushInformation) {
        throw ("PushInformation is null or undefined")
    }

    const subscription = await pushManager?.getSubscription();
    await UpdateSubscription(initial, pushInformation, subscription);

    //await CurrentDB.cloud.sync();
    if (subscription) {
        NotificationStateSubject.next(NotificationState.Subscribed);
    } else {
        NotificationStateSubject.next(remote ? NotificationState.UnsubscribedRemote : NotificationState.Unsubscribed);
    }
}

export async function ExpireAllPushSubscriptions() {
    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    // @ts-ignore
    await CurrentDB.transaction("rw", [PushSubscriptionsTableName, PushNotificationsTableName], async () => {

        if (!CurrentDB) {
            throw ("CurrentDB is null or undefined")
        }

        const subscriptionsTable = CurrentDB.table<DexieCloudPushSubscription, string>(PushSubscriptionsTableName);
        await subscriptionsTable.toCollection().modify(subscription => {
            subscription.expired = true;
        });

        const notificationsTable = CurrentDB.table<DexieCloudPushNotification, string>(PushNotificationsTableName);
        await notificationsTable.toCollection().modify(notification => {
            notification.expired = true;
        });
    });
}

async function InitPushInformation(applicationServerKey: string) {
    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    const subscription = await pushManager?.getSubscription();
    const subscriptionId = GetSubscriptionID(subscription);

    const pushInformation: PushInformation = {
        id: PushInformationKey, applicationServerKey: applicationServerKey, subscriptionId: subscriptionId
    }

    const pushInformationsTable = CurrentDB.table<PushInformation, number>(PushInformationsTableName);
    await pushInformationsTable.put(pushInformation);
}

async function GetPushInformation(): Promise<PushInformation | undefined> {
    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    const pushInformationsTable = CurrentDB.table<PushInformation, number>(PushInformationsTableName);
    return pushInformationsTable.get(PushInformationKey);
}

async function UpdateSubscription(initial: boolean, pushInformation: PushInformation, subscription: PushSubscription | null | undefined) {
    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    const subscriptionsTable = CurrentDB.table<DexieCloudPushSubscription, string>(PushSubscriptionsTableName);
    const pushInformationsTable = CurrentDB.table<PushInformation, number>(PushInformationsTableName);

    if (!subscription) {
        await UnsubscribeLiveQueries();

        if (pushInformation.subscriptionId) {
            await subscriptionsTable.where({":id": pushInformation.subscriptionId}).modify({"expired": true});
            pushInformation.subscriptionId = null;
            await pushInformationsTable.put(pushInformation);

            if (initial) { // subscription has been removed by browser but did exist -> notify
                NotificationStateSubject.next(NotificationState.UnsubscribedRemote);
            }
        }
        return;
    }

    const subscriptionId = GetSubscriptionID(subscription);

    if (subscriptionId && PushURL) {
        if (!initial) {
             const subscriptionItem: DexieCloudPushSubscription = {
                 // @ts-ignore
                id: subscriptionId, expired: false, pushURL: PushURL, declarative: window.pushManager !== undefined, subscription: subscription.toJSON()
            };

            await subscriptionsTable.put(subscriptionItem);
            pushInformation.subscriptionId = subscriptionId;
            await pushInformationsTable.put(pushInformation);
        }

        await SubscribeLiveQueries(subscriptionId);
    }
}

function GetSubscriptionID(subscription: PushSubscription | null | undefined) {
    return subscription?.endpoint != null ? md5(subscription!.endpoint) : null;
}

async function UnsubscribeLiveQueries() {
    SubscriptionCountSubscription?.unsubscribe();
    SubscriptionCountSubscription = undefined;
}

async function SubscribeLiveQueries(subscriptionId: string) {
    await UnsubscribeLiveQueries();

    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    const subscriptionsTable = CurrentDB.table(PushSubscriptionsTableName);
    const observablePushSubscriptions: Observable<DexieCloudPushSubscription[]> = from(liveQuery(() =>
        subscriptionsTable.toArray()
    ));

    SubscriptionCountSubscription = observablePushSubscriptions.pipe(
        switchMap(async subs => {
            const count = subs.filter(s =>
                s.id == subscriptionId && !s.expired).length;
            if (count == 0) {
                await UnSubscribePush(true);
            }
        })
    ).subscribe();
}

export function UpdateServiceWorker() {
    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    BroadcastOut.postMessage({type: DexieCloudNETSkipWaiting});
}

export async function SetAppBadge(count: number): Promise<void> {
    if (navigator.setAppBadge) {
        await navigator.setAppBadge(count);
    }
}

BroadcastIn.onmessage = async (event) => {
    if (event.data) {
        console.log(`New SW event: ${event.data.type}`)
        switch (event.data.type) {
            case DexieCloudNETSubscriptionChanged:
                const pushInformation = await GetPushInformation();
                if (pushInformation?.subscriptionId) {
                    await UnSubscribePush();
                    await SubscribePush();
                }
                break;
            case DexieCloudNETUpdateFound:
                ServiceWorkerNotificationSubject.next({
                    state: ServiceWorkerState.UpdateFound
                });
                break;
            case DexieCloudNETReloadPage:
                ServiceWorkerNotificationSubject.next({
                    state: ServiceWorkerState.ReloadPage
                });
                break;
        }
    }
};