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
    BadgeEventsTableName,
    ClickedEventsTableName,
    DexieCloudNETBroadcastIn,
    DexieCloudNETBroadcastOut, DexieCloudNETClickLock, DexieCloudNETReloadPage,
    DexieCloudNETSkipWaiting,
    DexieCloudNETSubscriptionChanged,
    DexieCloudNETUpdateFound,
    PushEventRecord,
    PushEventsDB,
} from "../serviceworker/dexieCloudNETSWBroadcast";

interface PushInformation {
    applicationServerKey: string,
    subscriptionId: string | null | undefined,
    id: number
}

interface DexieCloudPushSubscription {
    id: string,
    expired: boolean,
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
    ReloadPage,
    PushNotifications
}

enum ChangeSource {
    Push,
    Visibility,
    ServiceWorkerGeneric,
    ServiceWorkerClicked,
    ServiceWorkerBadge
}

interface ServiceWorkerNotification {
    state: ServiceWorkerState,
    clicked: number,
    badge: number,
    changeSource: ChangeSource
}

const ServiceWorkerNotificationNone: ServiceWorkerNotification = {
    state: ServiceWorkerState.None,
    clicked: 0,
    badge: 0,
    changeSource: ChangeSource.Push
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
let SubscriptionCountSubscription: Subscription | undefined = undefined;
let SubscriptionClickedSubscription: Subscription | undefined = undefined;
let SubscriptionBadgeSubscription: Subscription | undefined = undefined;
let clickLock = false;

export async function SubscribePush(): Promise<boolean> {
    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    let subscription = await worker?.pushManager?.getSubscription();

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

            subscription = await worker?.pushManager?.subscribe(options);
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
    const subscription = await worker?.pushManager?.getSubscription();

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

export async function SetPushNotificationState(ul: UserLogin, applicationServerKey: string) {
    if (!ul.isLoggedIn) {
        NotificationStateSubject.next(NotificationState.None);
        return;
    }

    if (Notification.permission === "denied") {
        NotificationStateSubject.next(NotificationState.Denied);
        return;
    }

    await InitPushInformation(applicationServerKey);
    await UpdatePushDatabases(true, false);
    await CheckPushEvents(ChangeSource.Push);
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

    const subscription = await worker?.pushManager?.getSubscription();
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

    const subscription = await worker?.pushManager?.getSubscription();
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

            const clickedEventsTable = PushEventsDB.table(ClickedEventsTableName);
            await clickedEventsTable.clear();
        }
        return;
    }

    const subscriptionId = GetSubscriptionID(subscription);

    if (subscriptionId) {
        if (!initial) {
            const subscriptionItem: DexieCloudPushSubscription = {
                id: subscriptionId, expired: false, subscription: subscription.toJSON()
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
    SubscriptionClickedSubscription?.unsubscribe();
    SubscriptionClickedSubscription = undefined;
    SubscriptionBadgeSubscription?.unsubscribe();
    SubscriptionBadgeSubscription = undefined;
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

    const badgeEventsTable = PushEventsDB.table(BadgeEventsTableName);
    const observableBadgeEvents: Observable<number> = from(liveQuery(() => badgeEventsTable.count()));
    SubscriptionBadgeSubscription = observableBadgeEvents.subscribe(count => {
        console.log(`New BadgeEvents from LiveQuery: ${count}`)
        ServiceWorkerNotificationSubject.next({
            state: ServiceWorkerState.PushNotifications,
            clicked: 0,
            badge: count,
            changeSource: ChangeSource.ServiceWorkerBadge
        })
    });

    const clickedEventsTable = PushEventsDB.table(ClickedEventsTableName);
    const observableClickedEvents: Observable<number> = from(liveQuery(() => clickedEventsTable.count()));
    SubscriptionClickedSubscription = observableClickedEvents.subscribe(count => {
        console.log(`New ClickedEvents from LiveQuery: ${count}`)
        ServiceWorkerNotificationSubject.next({
            state: ServiceWorkerState.PushNotifications,
            clicked: count,
            badge: 0,
            changeSource: ChangeSource.ServiceWorkerClicked
        })
    });
}

async function CheckPushEvents(changeSource: ChangeSource) {
    if (clickLock) {
        return;
    }
    
    const clickedEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(ClickedEventsTableName);
    const clickedEventsCount = await clickedEventsTable.count();
    console.log(`New ClickedEvents from ${ChangeSource[changeSource]}: ${clickedEventsCount}`)

    const badgeEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(BadgeEventsTableName);
    const badgeEventsCount = await badgeEventsTable.count();
    console.log(`New BadgeEvents from ${ChangeSource[changeSource]}: ${badgeEventsCount}`)

    if (clickedEventsCount > 0 || badgeEventsCount > 0 || changeSource === ChangeSource.ServiceWorkerBadge) {
        ServiceWorkerNotificationSubject.next({
            state: ServiceWorkerState.PushNotifications,
            clicked: clickedEventsCount,
            badge: badgeEventsCount,
            changeSource: changeSource
        });
    }
}

export function UpdateServiceWorker() {
    if (!CurrentDB) {
        throw ("CurrentDB is null or undefined")
    }

    BroadcastOut.postMessage({type: DexieCloudNETSkipWaiting});
}

export async function GetClickedEvents(): Promise<PushEventRecord[]> {
    const clickedEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(ClickedEventsTableName);
    return clickedEventsTable.toArray();
}

export async function DeleteClickedEvents(ids: number[]): Promise<void> {
    const clickedEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(ClickedEventsTableName);
    await clickedEventsTable.bulkDelete(ids);
}

export async function GetBadgeEvents(): Promise<PushEventRecord[]> {
    const badgeEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(BadgeEventsTableName);
    return badgeEventsTable.toArray();
}

export async function DeleteBadgeEvents(ids: number[]): Promise<void> {
    const badgeEventsTable = PushEventsDB.table<PushEventRecord, number, PushEventRecord>(BadgeEventsTableName);
    await badgeEventsTable.bulkDelete(ids);

    if (navigator.setAppBadge) {
        const count = await badgeEventsTable.count();
        navigator.setAppBadge(count).then();
    }
}

// https://stackoverflow.com/a/64979288/3708778
const getState = () => {
    if (document.visibilityState === 'hidden') {
        return 'hidden';
    }
    if (document.hasFocus()) {
        return 'active';
    }
    return 'passive';
};

let displayState = getState();

const onDisplayStateChanged = async () => {
    const nextState = getState();
    const prevState = displayState;

    if (nextState !== prevState) {
        console.log(`State changed: ${prevState} >>> ${nextState}`);
        displayState = nextState;

        if (nextState === 'active') {
            await CheckPushEvents(ChangeSource.Visibility);
        }
    }
};

['pageshow', 'focus', 'blur', 'visibilitychange', 'resume'].forEach((type) => {
    window.addEventListener(type, onDisplayStateChanged, {capture: true});
});

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
                    state: ServiceWorkerState.UpdateFound,
                    clicked: 0,
                    badge: 0,
                    changeSource: ChangeSource.ServiceWorkerGeneric
                });
                break;
            case DexieCloudNETReloadPage:
                ServiceWorkerNotificationSubject.next({
                    state: ServiceWorkerState.ReloadPage,
                    clicked: 0,
                    badge: 0,
                    changeSource: ChangeSource.ServiceWorkerGeneric
                });
                break;
            case DexieCloudNETClickLock:
                console.log(`DexieCloudNETClickLock: ${String(event.data.lock)}`);
                clickLock = event.data.lock;
                break;
        }
    }
};