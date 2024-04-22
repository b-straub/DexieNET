/*
dexieNET.js

Copyright(c) 2022 Bernhard Straub

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

import Dexie from 'dexie';

import DexieCloudDB from 'dexie';
import DexieCloud from "dexie-cloud-addon";
import { DexieCloudOptions } from "dexie-cloud-addon/dist/modern/DexieCloudOptions";
import { Observable, Subscription } from 'rxjs';

// @ts-ignore
export class DB extends DexieCloudDB {
    constructor(name: string, cloudSync: boolean) {
        if (cloudSync) {
            super(name, { addons: [DexieCloud] });
        }
        else {
            super(name);
        }
    }
}

export function Create(name: string, cloudSync: boolean): DB {
    let db = new DB(name, cloudSync);
    return db;
}

export function ConfigureCloud(db: DB, cloudOptions: DexieCloudOptions): string | null {
    try {
        db.cloud.configure(cloudOptions);
    }
    catch (err) {
        return err.message
    }

    return null;
}

export function Delete(name: string): Promise<void> {
    return Dexie.delete(name);
}

export function Name(): string {
    return Dexie.name;
}

export function Version(db: DB): number {
    return db.verno;
}

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