/*
dexieNETCollections.ts

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

import { Collection, replacePrefix } from 'dexie';

// Wrappers
export async function CollectionPrimarykeysByteArray(collection: Collection): Promise<Uint8Array[]> {
    var buffer = await collection.primaryKeys();
    return buffer.map(x => new Uint8Array(x as any[]));
}

export async function CollectionKeysByteArray(collection: Collection): Promise<Uint8Array[]> {
    var buffer = await collection.keys();
    return buffer.map(x => new Uint8Array(x as any[]));
}

export function CollectionFilter(collection: Collection, dotnetRef: any, filterIndex: number): Collection {
    return collection.filter(item => dotnetRef.invokeMethod('Filter', item, filterIndex))
}

export async function Modify(collection: Collection, changes: { [keyPath: string]: any; }): Promise<number> {
    let changesK = Object.fromEntries(Object.entries(changes).map(([k, v]) => [k, v === null ? undefined : v]));
    return await collection.modify(changesK);
}

export function CollectionUntil(collection: Collection, dotnetRef: any, includeStopEntry: boolean, untilIndex: number): Collection {
    return collection.until(item => dotnetRef.invokeMethod('Until', item, untilIndex), includeStopEntry)
}

export async function CollectionEach(collection: Collection, dotnetRef: any): Promise<void> {
    await collection.each(item => dotnetRef.invokeMethod('Each', item))
}

export async function CollectionEachKey(collection: Collection, dotnetRef: any): Promise<void> {
    await collection.eachKey(item => dotnetRef.invokeMethod('EachKey', item))
}

export async function CollectionEachPrimaryKey(collection: Collection, dotnetRef: any): Promise<void> {
    await collection.eachPrimaryKey(item => dotnetRef.invokeMethod('EachPrimaryKey', item))
}

export async function CollectionEachUniqueKey(collection: Collection, dotnetRef: any): Promise<void> {
    await collection.eachUniqueKey(item => dotnetRef.invokeMethod('EachUniqueKey', item))
}

export async function CollectionModify(collection: Collection, dotnetRef: any): Promise<number> {
    return await collection.modify((value, ref) => {
        var newItem = dotnetRef.invokeMethod('Modify', value);
        if (newItem === null) {
            delete ref.value;
        }
        else {
            ref.value = newItem;
        }
    })
}

export async function CollectionModifyReplacePrefix(collection: Collection, keyPath: string, a: string, b: string): Promise<number> {
    return await collection.modify({[keyPath]: replacePrefix(a, b)});
}