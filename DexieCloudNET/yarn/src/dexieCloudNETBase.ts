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

import DexieCloudDB from 'dexie';
import DexieCloud from "dexie-cloud-addon";

// @ts-ignore
export class CloudDB extends DexieCloudDB {
    constructor(name: string) {
        super(name, { addons: [DexieCloud] });
    }
}

export function CreateCloud(name: string): CloudDB {
    let db = new CloudDB(name);
    return db;
}