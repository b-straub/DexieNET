import Dexie, { Table, Transaction, TransactionMode, Version } from 'dexie';
import { DB } from "./dexieNETBase";

// Transaction
export async function Transaction(db: DB, dotnetRef: any, tableNames: string[], mode: TransactionMode): Promise<void> {
    await db.transaction(mode, tableNames, () => dotnetRef.invokeMethod('TransactionCallback'));
}

// Version upgrade
export async function Upgrade(version: Version, dotnetRef: any): Promise<Version> {
    return version.upgrade(() => {
        dotnetRef.invokeMethod('UpgradeCallback');
    });
}

export function AbortTransaction(): void {
    Dexie.currentTransaction?.abort();
}

export function CurrentTransaction(): any {
    return Dexie.currentTransaction;
}

export function TopLevelTransaction(db: DB, table: Table, mode: TransactionMode, dotnetRef: any): void {
    db.transaction(mode, table, () => dotnetRef.invokeMethod('TransactionCallback'));
}