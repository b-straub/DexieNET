import {Dexie} from "dexie";

export const DexieCloudNETBroadcastOut = "SW_TO_DEXIECLOUDNET";
export const DexieCloudNETBroadcastIn = "SW_FROM_DEXIECLOUDNET";

export const DexieCloudNETUpdateFound = "SW_UPDATE_FOUND";
export const DexieCloudNETReloadPage = "SW_RELOAD_PAGE";
export const DexieCloudNETSubscriptionChanged = "SW_SUBSCRIPTION_CHANGED";
export const DexieCloudNETSkipWaiting = "SW_SKIP_WAITING";
export const DexieCloudNETClickLock = "SW_CLICK_LOCK";

export interface PushEventRecord {
    timeStampUtc: string,
    payloadJson?: string,
    tag?: string
    id?: number
}

export const ClickedEventsTableName: string = "clickedEvents";
export const BadgeEventsTableName: string = "badgeEvents";

export const PushEventsDB = new Dexie("pushEventsDB");

PushEventsDB.version(1).stores({
    clickedEvents: "++id",
    badgeEvents: "++id, tag"
});