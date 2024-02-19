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