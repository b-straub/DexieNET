import Dexie, { Transaction, TransactionMode, Version } from 'dexie';
import { DB } from "./dexieNETBase";

// Version upgrade
export async function Upgrade(version: Version, dotnetRef: any): Promise<Version> {
    return version.upgrade(_ => {
        dotnetRef.invokeMethod('UpgradeCallback');
    });
}

export function AbortCurrentTransaction(): void {
    Dexie.currentTransaction?.abort();
}

export function AbortTransaction(transaction: Transaction | null): void {
    transaction?.abort();
}

export function CurrentTransaction(): any | null {
    return Dexie.currentTransaction;
}

// @ts-ignore
export function TopLevelTransaction(db: DB, tables: string[], mode: TransactionMode, dotnetRef: any): void {
    db.transaction(mode, tables, _ => dotnetRef.invokeMethod('TransactionCallback'));
}

export async function TopLevelTransactionAsync(db: DB, tables: string[], mode: TransactionMode, dotnetRef: any): Promise<void> {
    await db.transaction(mode, tables, _ => dotnetRef.invokeMethod('TransactionCallback'));
}

export async function TransactioWaitFor(dotnetRef: any): Promise<void> {
    await Dexie.waitFor(async () => await dotnetRef.invokeMethodAsync('TransactionWaitForCallback'));
}