/*
dexieCloudNETCloud.ts

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

import {getTiedRealmId} from "dexie-cloud-addon";
import {CloudDB} from "./dexieCloudNETBase";
import {DexieCloudSchema} from "dexie-cloud-common";
import {DexieCloudOptions} from "dexie-cloud-addon/dist/modern/DexieCloudOptions";
import {concatMap} from 'rxjs';
import {
    SetPushNotificationState,
    SetPushStatesToNone
} from "./dexieCloudNETPush";

export function ConfigureCloud(db: CloudDB, cloudOptions: DexieCloudOptions, applicationServerKey: string | undefined, dotnetRef: any): string | null {
    try {
        if (dotnetRef !== null) {
            cloudOptions.fetchTokens = async (tokenParams) => {
                return await dotnetRef.invokeMethodAsync('FetchTokens', tokenParams);
            }
        }
        db.cloud.configure(cloudOptions);

        if (applicationServerKey) {
            db.cloud.currentUser.pipe(
                concatMap(async ul => {
                    await SetPushNotificationState(ul, applicationServerKey);
                })
            ).subscribe();
        } else {
            SetPushStatesToNone();
        }
    } catch (err) {
        return err.message
    }
    
    return null;
}

export function AddOnVersion(db: CloudDB): string {
    return db.cloud.version;
}

export function CurrentUserId(db: CloudDB): string {
    return db.cloud.currentUserId;
}

export function Options(db: CloudDB): any {
    return db.cloud.options;
}

export function Schema(db: CloudDB): DexieCloudSchema | null {
    return db.cloud.schema;
}

export function UsingServiceWorker(db: CloudDB): boolean | undefined {
    return db.cloud.usingServiceWorker;
}

export async function UserLogin(db: CloudDB, email: string, grantType: "demo" | "otp" | undefined, userId?: string): Promise<string | undefined> {

    let httpError: string | undefined = undefined;

    try {
        await db.cloud.login({email: email, userId: userId, grant_type: grantType});
    } catch (e) {
        httpError = e.message;
    }

    return httpError;
}

// sync with DexieNETCloudSync.cs record Sync 
interface CloudSyncOptions {
    wait: boolean;
    purpose: number;
}

export async function Sync(db: CloudDB, syncOptions: CloudSyncOptions | undefined): Promise<void> {
    if (syncOptions) {
        await db.cloud.sync({purpose: syncOptions.purpose == 0 ? "push" : "pull", wait: syncOptions.wait});
    } else {
        await db.cloud.sync();
    }
}

export async function Logout(db: CloudDB, force?: boolean | undefined): Promise<void> {
    await db.cloud.logout({force: force})
}

export function GetTiedRealmID(id: string): string {
    return getTiedRealmId(id);
}

export async function AcceptInviteMember(db: CloudDB, id: string): Promise<void> {
    await db.members.update(id, {accepted: new Date(), rejected: undefined})
}

export async function RejectInviteMember(db: CloudDB, id: string): Promise<void> {
    await db.members.update(id, {rejected: new Date(), accepted: undefined})
}

export async function ClearInviteMember(db: CloudDB, id: string): Promise<void> {
    await db.members.update(id, {rejected: undefined, accepted: undefined})
}

// copied from Dexie.js/addons/dexie-cloud/src/authentication/logout.ts
export async function NumUnsyncedChanges(db: CloudDB) : Promise<number> {
    let unsyncCount = 0;

    // @ts-ignore
    await db.transaction('rw', db.tables, async (tx) => {
        // @ts-ignore
        const idbtrans: IDBTransaction & TXExpandos = tx.idbtrans;
        idbtrans.disableChangeTracking = true;
        idbtrans.disableAccessControl = true;
        const mutationTables = tx.storeNames.filter((tableName) =>
            tableName.endsWith('_mutations')
        );

        // Count unsynced changes
        const unsyncCounts = await Promise.all(
            mutationTables.map((mutationTable) => tx.table(mutationTable).count())
        );
        unsyncCount = unsyncCounts.reduce((a, b) => a + b, 0);
    });
    
    return unsyncCount;
}