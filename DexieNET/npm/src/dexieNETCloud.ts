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

export function CurrentUserId(db: DB): string {
    return db.cloud.currentUserId;
}

export async function UserLogin(db: DB, email: string, grantType: "demo" | "otp" | undefined, userId?: string): Promise<void> {
    await db.cloud.login({ email: email, userId: userId, grant_type: grantType });
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