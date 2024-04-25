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

import { DB } from "./dexieNETBase";
import { DXCUserInteraction } from "dexie-cloud-addon/dist/modern/types/DXCUserInteraction";
import { SyncState } from "dexie-cloud-addon/dist/modern/types/SyncState";
import { UserLogin } from "dexie-cloud-addon/dist/modern/db/entities/UserLogin";
import { DBRealmRole, Invite } from "dexie-cloud-addon";
import { PermissionChecker } from "dexie-cloud-addon/dist/modern/PermissionChecker";
import { Observable, Subscription } from 'rxjs';
import { PersistedSyncState } from "dexie-cloud-addon/dist/modern/db/entities/PersistedSyncState";

let _uiInteractions: { [key: number]: DXCUserInteraction } = {};

export function UnSubscribeJSObservable(disposable: Subscription): void {

    disposable.unsubscribe();
}

export function DotNetObservable<T>(observable: Observable<T>, action: (input: T) => any, dotnetRef: any, voidObservable: boolean = false): Subscription {

    return observable.subscribe({
        next: (v) => {
            if (voidObservable || v != undefined) {
                dotnetRef.invokeMethod('OnNext', action(v))
            }
        },
        error: (e) => dotnetRef.invokeMethod('OnError', e.message),
        complete: () => dotnetRef.invokeMethod('OnCompleted')
    });
}

// sync with DexieNETCloudUI.cs
export function SubscribeUserInteraction(db: DB, dotnetRef: any): Subscription {

    const action = (res: DXCUserInteraction) => {
        var type: number = -1;

        const fieldType = (orgType: string): number => {
            switch (orgType) {
                case 'text': return 0;
                case 'email': return 1;
                case 'otp': return 2;
                case 'password': return 3;
                default: throw "SubscribeUserInteraction, undefined field type!";
            }
        }

        var field: { type: number, label: string | null, placeholder: string | null } | null = null;

        switch (res.type) {
            case 'email':
                type = 0;
                field = { type: fieldType(res.fields.email.type), label: null, placeholder: res.fields.email.placeholder };
                break;
            case 'otp':
                type = 1;
                field = { type: fieldType(res.fields.otp.type), label: res.fields.otp.label, placeholder: null }
                break;
            case 'message-alert':
                type = 2;
                field = null;
                break;
            case 'logout-confirmation':
                type = 3;
                field = null;
                break;
            default: throw "SubscribeUserInteraction, undefined ui type!";
        }

        const alerts = Array<{ type: number, code: number, message: string, params: { [paramName: string]: string } }>();

        res.alerts.forEach(alert => {
            var alertType: number = -1;

            switch (alert.type) {
                case 'info': alertType = 0; break;
                case 'warning': alertType = 1; break;
                case 'error': alertType = 2; break;
                default: throw "SubscribeUserInteraction, undefined alert type!";
            }

            var messageCode: number = -1;

            switch (alert.messageCode) {
                case 'OTP_SENT': messageCode = 0; break;
                case 'INVALID_OTP': messageCode = 1; break;
                case 'INVALID_EMAIL': messageCode = 2; break;
                case 'LICENSE_LIMIT_REACHED': messageCode = 3; break;
                case 'GENERIC_INFO': messageCode = 4; break;
                case 'GENERIC_WARNING': messageCode = 5; break;
                case 'GENERIC_ERROR': messageCode = 6; break;
                case 'LOGOUT_CONFIRMATION': messageCode = 7; break;
                default: throw "SubscribeUserInteraction, undefined message code!";
            }

            alerts.push({ type: alertType, code: messageCode, message: alert.message, params: alert.messageParams });
        })

        const key = Date.now();
        _uiInteractions[key] = res;

        return { type: type, title: res.title, alerts: alerts, fields: field, key: key };
    };

    return DotNetObservable(db.cloud.userInteraction, action, dotnetRef);
}

export function ClearUserInteraction(): void {
    _uiInteractions = {};
}

export function OnSubmitUserInteraction(key: number, params: { [paramName: string]: string }) {

    const uiInteraction: DXCUserInteraction = _uiInteractions[key];

    if (uiInteraction != undefined) {
        switch (uiInteraction.type) {
            case 'email':
                uiInteraction.onSubmit({ email: params['email'] });
                break;
            case 'otp':
                uiInteraction.onSubmit({ otp: params['otp'] });
                break;
            case 'message-alert':
                uiInteraction.onSubmit(params);
                break;
            default: throw "OnSubmitUserInteraction, undefined ui type!";
        }

        delete _uiInteractions[key];
    }
}

export function OnCancelUserInteraction(key: number) {

    const uiInteraction: DXCUserInteraction = _uiInteractions[key];

    if (uiInteraction != undefined) {
        uiInteraction.onCancel();
        delete _uiInteractions[key];
    }
}

export function SubscribeWebSocketStatus(db: DB, dotnetRef: any): Subscription {

    const action = (res: string) => {
        return res;
    };

    return DotNetObservable(db.cloud.webSocketStatus, action, dotnetRef);
}

// sync with DexieNETSync.cs
export function SubscribeSyncState(db: DB, dotnetRef: any): Subscription {

    const action = (res: SyncState) => {

        const status = (orgType: string): number => {
            switch (orgType) {
                case 'not-started': return 0;
                case 'connecting': return 1;
                case 'connected': return 2;
                case 'disconnected': return 3;
                case 'error': return 4;
                case 'offline': return 5;
                default: throw "SubscribeUserInteraction, undefined field type!";
            }
        }

        const phase = (orgType: string): number => {
            switch (orgType) {
                case 'initial': return 0;
                case 'not-in-sync': return 1;
                case 'pushing': return 2;
                case 'pulling': return 3;
                case 'in-sync': return 4;
                case 'error': return 5;
                case 'offline': return 6;
                default: throw "SubscribeUserInteraction, undefined field type!";
            }
        }

        return { status: status(res.status), phase: phase(res.phase), progress: res.progress, error: res.error?.message };
    };

    return DotNetObservable(db.cloud.syncState, action, dotnetRef);
}

// sync with DexieNETSync.cs
export function SubscribePersistedSyncState(db: DB, dotnetRef: any): Subscription {

    const action = (res: PersistedSyncState) => {
        return res;
    };

    return DotNetObservable(db.cloud.persistedSyncState, action, dotnetRef);
}

export function SubscribeSyncComplete(db: DB, dotnetRef: any): Subscription {

    const action = (_: void) => {
        return true;
    };

    return DotNetObservable(db.cloud.events.syncComplete, action, dotnetRef, true);
}

// sync with DexieNETCloudUser.cs
export function SubscribeUserLogin(db: DB, dotnetRef: any): Subscription {

    const action = (res: UserLogin) => {

        let convert = (o : any): any => Object.fromEntries(Object.keys(o).map(key => [key, o[key].toString()]));

        const licenseType = (orgType: string): number => {
            switch (orgType) {
                case 'demo': return 0;
                case 'eval': return 1;
                case 'prod': return 2;
                case 'client': return 3;
                default: throw "SubscribeUserLogin, undefined license type!";
            }
        }

        const licenseStatus = (orgType: string): number => {
            switch (orgType) {
                case 'ok': return 0;
                case 'expired': return 1;
                case 'deactivated': return 2;
                default: throw "SubscribeUserLogin, undefined license status!";
            }
        }

        let license = res.license === undefined ? undefined :
            {
                type: licenseType(res.license.type), status: licenseStatus(res.license.status),
                validUntil: res.license.validUntil, evalDaysLeft: res.license.evalDaysLeft
            };

        return {
            userId: res.userId, name: res.name, email: res.email, claims: convert(res.claims), license: license,
            lastlogin: res.lastLogin, accessToken: res.accessToken, accessTokenExpiration: res.accessTokenExpiration,
            refreshToken: res.refreshToken, refreshTokenExpiration: res.refreshTokenExpiration,
            nonExportablePrivateKey: JSON.stringify(res.nonExportablePrivateKey), publicKey: JSON.stringify(res.publicKey),
            isLoggedIn: res.isLoggedIn === undefined ? false : res.isLoggedIn
        };
    };

    return DotNetObservable(db.cloud.currentUser, action, dotnetRef);
}

// sync with DexieNETCloudAccesss.cs
let _invites: { [key: string]: Invite } = {};

export function SubscribeInvites(db: DB, dotnetRef: any): Subscription {

    const action = (res: Invite[]) => {
        ClearInvites();
        res.forEach(i => _invites[i.id] = i);
        return res;
    };

    return DotNetObservable(db.cloud.invites, action, dotnetRef);
}

export function ClearInvites(): void {
    _invites = {};
}

export function AcceptInvite(key: string) {

    const invite: Invite = _invites[key];

    if (invite != undefined) {
        invite.accept();
    }
}

export function RejectInvite(key: string) {

    const invite: Invite = _invites[key];

    if (invite != undefined) {
        invite.reject();
    }
}

export function SubscribeRoles(db: DB, dotnetRef: any): Subscription {

    const action = (res: {[roleName: string]: DBRealmRole}) => {
        return res;
    };

    return DotNetObservable(db.cloud.roles, action, dotnetRef);
}

interface NETCloudEntity {
    owner?: string;
    realmId?: string;
}

let _permissionChecker: { [key: number]: PermissionChecker<NETCloudEntity> } = {};

export function SubscribePermissionChecker(db: DB, dotnetRef: any, tableName: string, item?: NETCloudEntity): Subscription {

    const key = Date.now();
    item ??= { owner: undefined, realmId: undefined };

    const action = (res: PermissionChecker<NETCloudEntity, string>) => {
        _permissionChecker[key] = res;
        return key;
    };

    return DotNetObservable(db.cloud.permissions(item, tableName), action, dotnetRef);
}

export function ClearPermissionChecker(key: number): void {

    if (_permissionChecker[key] != undefined) {
        delete _permissionChecker[key];
    }
}

export function PermissionCheckerAdd(key: string, ...tableNames: string[]): boolean {

    const pc: PermissionChecker<any, string> = _permissionChecker[key];

    if (pc != undefined) {
        return pc.add(...tableNames);
    }

    return false;
}

export function PermissionCheckerUpdate(key: string, ...keys: string[]): boolean {

    const pc: PermissionChecker<any, string> = _permissionChecker[key];

    if (pc != undefined) {
        return pc.update(...keys);
    }

    return false;
}

export function PermissionCheckerDelete(key: string): boolean {

    const pc: PermissionChecker<any, string> = _permissionChecker[key];

    if (pc != undefined) {
        return pc.delete();
    }

    return false;
}