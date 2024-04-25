/*
dexieNET.js

Copyright(c) 2023 Bernhard Straub

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

import { getTiedRealmId } from "dexie-cloud-addon";
import { DB } from "./dexieNETBase";
import { DexieCloudSchema } from "dexie-cloud-common";
import { DexieCloudOptions } from "dexie-cloud-addon/dist/modern/DexieCloudOptions";

export function ConfigureCloud(db: DB, cloudOptions: DexieCloudOptions, dotnetRef: any): string | null {
    try {
        if (dotnetRef !== null)
        {
            cloudOptions.fetchTokens = async (tokenParams) => {
                return await dotnetRef.invokeMethodAsync('FetchTokens', tokenParams);
            }
        }
        db.cloud.configure(cloudOptions);
    }
    catch (err) {
        return err.message
    }

    return null;
}

export function AddOnVersion(db: DB): string {
    return db.cloud.version;
}

export function CurrentUserId(db: DB): string {
    return db.cloud.currentUserId;
}

export function Options(db: DB): any {
    return db.cloud.options;
}

export function Schema(db: DB): DexieCloudSchema | null {
    return db.cloud.schema;
}

export function UsingServiceWorker(db: DB): boolean | undefined {
    return db.cloud.usingServiceWorker;
}

export async function UserLogin(db: DB, email: string, grantType: "demo" | "otp" | undefined, userId?: string): Promise<string | undefined> {

    let httpError: string | undefined = undefined;

    try {
        await db.cloud.login({ email: email, userId: userId, grant_type: grantType });
    }
    catch (e) {
        httpError = e.message;
    }

    return httpError;
}

// sync with DexieNETCloudSync.cs record Sync 
interface CloudSyncOptions {
    wait: boolean;
    purpose: number;
}

export async function Sync(db: DB, sync: CloudSyncOptions ): Promise<void> {
    await db.cloud.sync({purpose: sync.purpose == 0 ? "push" : "pull", wait: sync.wait });
}

export async function Logout(db: DB, force?: boolean | undefined): Promise<void> {
    await db.cloud.logout({ force: force });
}

export function GetTiedRealmID(id: string): string {
    return getTiedRealmId(id);
}

export async function AcceptInviteMember(db: DB, id: string): Promise<void> {
    await db.members.update(id, { accepted: new Date(), rejected: undefined })
}

export async function RejectInviteMember(db: DB, id: string): Promise<void> {
    await db.members.update(id, { rejected: new Date(), accepted: undefined })
}

export async function ClearInviteMember(db: DB, id: string): Promise<void> {
    await db.members.update(id, { rejected: undefined, accepted: undefined })
}