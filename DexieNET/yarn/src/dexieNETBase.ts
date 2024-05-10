/*
dexieNET.ts

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

import Dexie from 'dexie';

// @ts-ignore
export class DB extends Dexie {
    constructor(name: string) {
        super(name);
    }
}

export function Create(name: string): DB {
    let db = new DB(name);
    return db;
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