import Dexie from 'dexie';
// Transaction
export async function Transaction(db, dotnetRef, tableNames, mode) {
    await db.transaction(mode, tableNames, () => dotnetRef.invokeMethod('TransactionCallback'));
}
// Version upgrade
export async function Upgrade(version, dotnetRef) {
    return version.upgrade(() => {
        dotnetRef.invokeMethod('UpgradeCallback');
    });
}
export function AbortTransaction() {
    Dexie.currentTransaction?.abort();
}
export function CurrentTransaction() {
    return Dexie.currentTransaction;
}
//# sourceMappingURL=dexieNETTransactions.js.map