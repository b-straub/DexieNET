﻿import { Collection } from 'dexie';

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

export function Modify(collection: Collection, changes: { [keyPath: string]: any; }): Promise<number> {
    let changesK = Object.fromEntries(Object.entries(changes).map(([k, v]) => [k, v === null ? undefined : v]));
    return collection.modify(changesK);
}

export async function CollectionUntil(collection: Collection, dotnetRef: any, includeStopEntry: boolean, untilIndex: number): Promise<Collection> {
    return await collection.until(item => dotnetRef.invokeMethod('Until', item, untilIndex), includeStopEntry)
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