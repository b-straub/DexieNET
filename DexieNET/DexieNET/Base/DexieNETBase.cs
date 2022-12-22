/*
DexieNETBase.cs

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

using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;

namespace DexieNET
{
    public interface IDBStore
    {
    }

    public interface IDBBase
    {
        public static abstract IDBBase Create(IJSInProcessObjectReference module, IJSObjectReference reference);

        public static abstract string Name { get; }
    }

    public enum PersistanceType
    {
        Default = -1,
        Never = 0,
        Persisted = 1,
        Prompt = 2,
    }

    public record struct StorageEstimate(double Quota, double Usage);

    public sealed class Persistance : JSObject
    {
        public Persistance(IJSInProcessObjectReference module, IJSObjectReference? reference) : base(module, reference)
        {
        }

        public async ValueTask<PersistanceType> GetPersistanceType()
        {
            return await Module.InvokeAsync<PersistanceType>("InitStoragePersistence");
        }

        public async ValueTask<StorageEstimate> GetStorageEstimate()
        {
            return await Module.InvokeAsync<StorageEstimate>("ShowEstimatedQuota");
        }

        public async ValueTask<bool> RequestPersistance()
        {
            return await Module.InvokeAsync<bool>("Persist");
        }
    }

    public abstract class DBBase
    {
        public abstract ValueTask<Version> Version(double versionNumber);
        internal Transaction? CurrentTransaction { get; private set; }
        internal bool LiveQueryRunning { get; set; } = false;
        internal bool TransactionParallel { get; private set; }
        internal JSObject DBBaseJS { get; }

        protected DBBase(IJSInProcessObjectReference module, IJSObjectReference reference)
        {
            TransactionParallel = false;
            DBBaseJS = new(module, reference);
        }

        [MemberNotNullWhen(returnValue: true, member: nameof(CurrentTransaction))]
        internal bool StartTransaction(bool parallel)
        {
            CurrentTransaction = new(this);
            if (CurrentTransaction is null)
            {
                return false;
            }

            TransactionParallel = parallel;
            return true;
        }

        [MemberNotNullWhen(returnValue: true, member: nameof(CurrentTransaction))]
        internal bool StartTransaction(HashSet<string> tableNames, TAMode mode)
        {
            CurrentTransaction = new(this, tableNames, mode);
            if (CurrentTransaction is null)
            {
                return false;
            }

            TransactionParallel = false;
            return true;
        }

        internal void StopTransaction()
        {
            if (CurrentTransaction is not null)
            {
                CurrentTransaction.TransactionBase = null;
            }

            TransactionParallel = false;
            CurrentTransaction = null;
        }

        public Persistance Persistance()
        {
            return new Persistance(DBBaseJS.Module, null);
        }
    }

    public sealed class DexieNETFactory<T> : IAsyncDisposable where T : IDBBase
    {
        private readonly Lazy<Task<IJSInProcessObjectReference>> _moduleTask;
        public DexieNETFactory(IJSRuntime jsRuntime)
        {
            if (jsRuntime is not IJSInProcessRuntime)
            {
                throw new InvalidOperationException("This IndexedDB wrapper is only designed for Webassembly usage!");
            }
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSInProcessObjectReference>(
               "import", @"./_content/DexieNET/js/dexieNET.js").AsTask());
        }

        public async ValueTask<T> Create()
        {
            var module = await _moduleTask.Value;
            var reference = await module.InvokeAsync<IJSObjectReference>("Create", T.Name);

            return (T)T.Create(module, reference);
        }

        public async ValueTask Delete()
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("Delete", T.Name);
        }

        public async ValueTask DisposeAsync()
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }

    public static class DBExtensions
    {
        public static async ValueTask<LiveQuery<T>> LiveQuery<T>(this DBBase db, Func<ValueTask<T>> query, params IObservable<Unit>[] observables)
        {
            var lq = new LiveQuery<T>(db, query, observables);
            await db.DBBaseJS.Module.InvokeVoidAsync("LiveQuery", lq.DotnetRef, lq.ID);
            return lq;
        }

        public static async ValueTask Transaction(this DBBase db, Func<TransactionBase?, Task> create)
        {
            if (db.LiveQueryRunning)
            {
                throw new InvalidOperationException("Transactions not allowed inside a 'LiveQuery' yet.");
            }

            if (db.TransactionParallel)
            {
                db.StopTransaction();
                throw new InvalidOperationException("Nested parallel transactions are not supported.");
            }

            try
            {
                if (db.CurrentTransaction is null)
                {
                    if (db.StartTransaction(false))
                    {
                        await create(null);
                        await db.CurrentTransaction.Commit(create);
                        db.StopTransaction();
                    }
                    else
                    {
                        throw new ArgumentNullException(nameof(db));
                    }
                }
                else
                {
                    try
                    {
                        await create(db.CurrentTransaction.TransactionBase);
                    }
                    catch (Exception ex)
                    {
                        if (db.CurrentTransaction?.TransactionBase is not null)
                        {
                            db.CurrentTransaction.TransactionBase.FirstError ??= ex.Message;
                            await db.CurrentTransaction.TransactionBase.Abort();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string message = db.CurrentTransaction?.TransactionBase?.FirstError ?? ex.Message;
                db.StopTransaction();
                throw new TransactionException(message);
            }
        }

        internal static void AbortTransaction(this DBBase db)
        {
            if (db.CurrentTransaction is not null)
            {
                db.DBBaseJS.Module.InvokeVoid("AbortTransaction");
                db.StopTransaction();
            }
        }

        public static async ValueTask Transaction(this DBBase db, params Func<TransactionBase?, Task>[] creates)
        {
            if (db.LiveQueryRunning)
            {
                throw new InvalidOperationException("Transactions not allowed inside a 'LiveQuery' yet.");
            }

            List<Task> tasks = new();
            List<(Transaction transaction, Func<TransactionBase?, Task> create)> transactions = new();

            foreach (var create in creates)
            {
                if (db.StartTransaction(true))
                {
                    await create(db.CurrentTransaction.TransactionBase);
                    transactions.Add((db.CurrentTransaction, create));
                    db.StopTransaction();
                }
                else
                {
                    throw new ArgumentNullException(nameof(db));
                }
            }

            foreach (var (transaction, create) in transactions)
            {
                tasks.Add(transaction.Commit(create).AsTask());
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                string? message = null;

                foreach (var (transaction, create) in transactions)
                {
                    message ??= transaction.TransactionBase?.FirstError;
                }

                message ??= ex.Message;
                throw new TransactionException(message);
            }
        }

        public static async ValueTask<T> Open<T>(this T dexie) where T : DBBase, IDBBase
        {
            var reference = await dexie.DBBaseJS.InvokeAsync<IJSObjectReference>("open");
            return (T)T.Create(dexie.DBBaseJS.Module, reference);
        }

        public static async ValueTask<bool> IsOpen(this DBBase dexie)
        {
            return await dexie.DBBaseJS.InvokeAsync<bool>("isOpen");
        }

        public static async ValueTask Close(this DBBase dexie)
        {
            await dexie.DBBaseJS.InvokeVoidAsync("close");
        }

        public static async ValueTask Delete(this DBBase dexie)
        {
            await dexie.DBBaseJS.InvokeVoidAsync("delete");
        }

        public static async ValueTask<string> Name(this DBBase dexie)
        {
            return await dexie.DBBaseJS.Module.InvokeAsync<string>("Name");
        }

        public static async ValueTask<double> Version(this DBBase dexie)
        {
            return await dexie.DBBaseJS.Module.InvokeAsync<double>("Version", dexie.DBBaseJS.Reference);
        }
    }
}