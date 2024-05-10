/*
dexieNETTable.ts

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

import { IndexableType, IndexableTypeArrayReadonly, Table } from 'dexie';

// Wrappers
export async function AddByteArray(table: Table, item: any, key?: IndexableType): Promise<Uint8Array> {
    let buffer: any = await table.add(item, key);
    return new Uint8Array(buffer);
}

export async function BulkAddByteArray(table: Table, item: IndexableTypeArrayReadonly, keys?: Uint8Array[], allKeys?: boolean): Promise<Uint8Array[]> {
    let options = allKeys ? { allKeys: allKeys } : undefined
    let buffer = await table.bulkAdd(item, keys, options);
    return allKeys == true ? (buffer as any[]).map(x => new Uint8Array(x)) : new Array(new Uint8Array(buffer as any));
}

export async function PutByteArray(table: Table, item: any, key?: IndexableType): Promise<Uint8Array> {
    let buffer: any = await table.put(item, key);
    return new Uint8Array(buffer);
}

export async function BulkPutByteArray(table: Table, item: IndexableTypeArrayReadonly, keys?: Uint8Array[], allKeys?: boolean): Promise<Uint8Array[]> {
    let options = allKeys ? { allKeys: allKeys } : undefined
    let buffer = await table.bulkPut(item, keys, options);
    return allKeys == true ? (buffer as any[]).map(x => new Uint8Array(x)) : new Array(new Uint8Array(buffer as any));
}