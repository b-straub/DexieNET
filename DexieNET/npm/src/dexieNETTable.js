// Wrappers
export async function AddByteArray(table, item, key) {
    let buffer = await table.add(item, key);
    return new Uint8Array(buffer);
}
export async function BulkAddByteArray(table, item, keys, allKeys) {
    let options = allKeys ? { allKeys: allKeys } : undefined;
    let buffer = await table.bulkAdd(item, keys, options);
    return allKeys == true ? buffer.map(x => new Uint8Array(x)) : new Array(new Uint8Array(buffer));
}
export async function PutByteArray(table, item, key) {
    let buffer = await table.put(item, key);
    return new Uint8Array(buffer);
}
export async function BulkPutByteArray(table, item, keys, allKeys) {
    let options = allKeys ? { allKeys: allKeys } : undefined;
    let buffer = await table.bulkPut(item, keys, options);
    return allKeys == true ? buffer.map(x => new Uint8Array(x)) : new Array(new Uint8Array(buffer));
}
//# sourceMappingURL=dexieNETTable.js.map