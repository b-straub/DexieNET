// Wrappers
export async function CollectionPrimarykeysByteArray(collection) {
    var buffer = await collection.primaryKeys();
    return buffer.map(x => new Uint8Array(x));
}
export async function CollectionKeysByteArray(collection) {
    var buffer = await collection.keys();
    return buffer.map(x => new Uint8Array(x));
}
export function CollectionFilter(collection, dotnetRef, filterIndex) {
    return collection.filter(item => dotnetRef.invokeMethod('Filter', item, filterIndex));
}
export function Modify(collection, changes) {
    let changesK = Object.fromEntries(Object.entries(changes).map(([k, v]) => [k, v === null ? undefined : v]));
    return collection.modify(changesK);
}
export async function CollectionUntil(collection, dotnetRef, includeStopEntry, untilIndex) {
    return await collection.until(item => dotnetRef.invokeMethod('Until', item, untilIndex), includeStopEntry);
}
export async function CollectionEach(collection, dotnetRef) {
    await collection.each(item => dotnetRef.invokeMethod('Each', item));
}
export async function CollectionEachKey(collection, dotnetRef) {
    await collection.eachKey(item => dotnetRef.invokeMethod('EachKey', item));
}
export async function CollectionEachPrimaryKey(collection, dotnetRef) {
    await collection.eachPrimaryKey(item => dotnetRef.invokeMethod('EachPrimaryKey', item));
}
export async function CollectionEachUniqueKey(collection, dotnetRef) {
    await collection.eachUniqueKey(item => dotnetRef.invokeMethod('EachUniqueKey', item));
}
export async function CollectionModify(collection, dotnetRef) {
    return await collection.modify((value, ref) => {
        var newItem = dotnetRef.invokeMethod('Modify', value);
        if (newItem === null) {
            delete ref.value;
        }
        else {
            ref.value = newItem;
        }
    });
}
//# sourceMappingURL=dexieNETCollection.js.map