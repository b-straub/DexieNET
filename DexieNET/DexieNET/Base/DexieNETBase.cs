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
using JSException = Microsoft.JSInterop.JSException;

namespace DexieNET
{
    public interface IDBStore
    {
    }

    public interface IDBBase
    {
        public static abstract IDBBase Create(IJSInProcessObjectReference module, IJSInProcessObjectReference reference, bool cloudSync);
        public static abstract string Name { get; }
    }

    public enum PersistenceType
    {
        Default = -1,
        Never = 0,
        Persisted = 1,
        Prompt = 2,
    }

    public record StorageEstimate(double Quota, double Usage);

    public sealed class Persistance(IJSInProcessObjectReference module, IJSInProcessObjectReference? reference) : DexieJSObject(module, reference)
    {
        public async ValueTask<PersistenceType> GetPersistanceType()
        {
            return await Module.InvokeAsync<PersistenceType>("InitStoragePersistence");
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
        public abstract Version Version(double versionNumber);
        public bool CloudSync { get; }
        public abstract string[] UnsyncedTables { get; }

        internal bool LiveQueryRunning { get; set; } = false;
        internal DexieJSObject DBBaseJS { get; }
        internal Transaction? CurrentTransaction { get; set; }
        internal Stack<Transaction> TransactionCollectStack { get; }
        internal Dictionary<int, Transaction> TransactionDict { get; }

        internal Stack<Task> TransactionTasks { get; }

        internal TAState TransactionState { get; set; } = TAState.Collecting;

        protected DBBase(IJSInProcessObjectReference module, IJSInProcessObjectReference reference, bool cloudSync)
        {
            DBBaseJS = new(module, reference);
            TransactionCollectStack = new();
            TransactionDict = [];
            TransactionTasks = new();
            CloudSync = cloudSync;
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
            if (!OperatingSystem.IsBrowser())
            {
                throw new InvalidOperationException("This IndexedDB wrapper is only designed for Webassembly usage!");
            }
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSInProcessObjectReference>(
               "import", @"./_content/DexieNET/js/dexieNET.js").AsTask());
        }

        public async ValueTask<T> Create(bool cloudSync = false)
        {
            var module = await _moduleTask.Value;
            var reference = await module.InvokeAsync<IJSInProcessObjectReference>("Create", T.Name, cloudSync);

            return (T)T.Create(module, reference, cloudSync);
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
        public static LiveQuery<T> LiveQuery<T>(this DBBase db, Func<ValueTask<T>> query)
        {
            var lq = new LiveQuery<T>(db, query);
            db.DBBaseJS.Module.InvokeVoid("LiveQuery", lq.DotnetRef, lq.ID);
            return lq;
        }

        public static async Task Transaction(this DBBase db, Func<Transaction, Task> create, TAType type = TAType.Nested)
        {
            bool parallel = type is TAType.Parallel;
            bool topLevel = type is TAType.TopLevel;

            try
            {
                if (db.TransactionState is not TAState.Collecting && db.TransactionState is not TAState.ParallelCollecting)
                {
                    if (topLevel)
                    {
                        if (db.TransactionState is TAState.Executing)
                        {
                            if (db.TransactionDict.TryGetValue(create.Method.GetHashCode(), out Transaction? t))
                            {
                                db.CurrentTransaction = t;
                                t.Commit(create);
                            }
                            else
                            {
                                throw new InvalidOperationException("Transaction not found.");
                            }
                        }
                    }
                    else
                    {
                        if (db.CurrentTransaction is null)
                        {
                            throw new InvalidOperationException("No transaction running.");
                        }

                        await create(db.CurrentTransaction);
                    }
                }
                else
                {
                    if (db.TransactionCollectStack.Count == 0)
                    {
                        db.TransactionDict.Clear();
                        db.TransactionTasks.Clear();

                        db.TransactionState = parallel ? TAState.ParallelCollecting : TAState.Collecting;

                        db.CurrentTransaction = new(db, parallel);
                        db.TransactionCollectStack.Push(db.CurrentTransaction);
                        db.TransactionDict.TryAdd(create.Method.GetHashCode(), db.CurrentTransaction);

                        db.CurrentTransaction.StartCollect();
                        await create(db.CurrentTransaction);
                        db.CurrentTransaction.StopCollect();

                        db.TransactionState = parallel ? TAState.ParallelExecuting : TAState.Executing;

                        db.CurrentTransaction = db.TransactionCollectStack.Pop();

                        if (db.TransactionCollectStack.Count != 0)
                        {
                            throw new InvalidOperationException("Not all nested transactions have been processed.");
                        }

                        if (db.TransactionState is TAState.Executing)
                        {
                            if (db.TransactionDict.TryGetValue(create.Method.GetHashCode(), out Transaction? t))
                            {
                                db.CurrentTransaction = t;
                                if (db.LiveQueryRunning)
                                {
                                    t.Commit(create);
                                }
                                else
                                {
                                    await t.CommitAsync(create);
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException("Transaction not found.");
                            }
                        }

                        if (db.TransactionState is TAState.ParallelExecuting)
                        {
                            db.CurrentTransaction = null;
                            await Task.WhenAll([.. db.TransactionTasks]);
                        }

                        db.TransactionState = TAState.Collecting;
                    }
                    else if (db.CurrentTransaction is not null)
                    {
                        if (db.CurrentTransaction.Collecting)
                        {
                            if (topLevel)
                            {
                                db.CurrentTransaction = new(db, false);
                                db.TransactionCollectStack.Push(db.CurrentTransaction);
                                db.TransactionDict.TryAdd(create.Method.GetHashCode(), db.CurrentTransaction);

                                db.CurrentTransaction.StartCollect();
                                await create(db.CurrentTransaction);
                                if (db.TransactionState is TAState.ParallelCollecting)
                                {
                                    db.TransactionTasks.Push(db.CurrentTransaction.CommitAsync(create).AsTask());
                                }
                                db.CurrentTransaction.StopCollect();

                                db.TransactionCollectStack.Pop();
                                db.CurrentTransaction = db.TransactionCollectStack.Peek();
                            }
                            else
                            {
                                db.TransactionDict.TryAdd(create.Method.GetHashCode(), db.CurrentTransaction);
                                await create(db.CurrentTransaction);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Transactions not collecting.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!parallel && db.TransactionDict.TryGetValue(create.Method.GetHashCode(), out Transaction? t))
                {
                    db.CurrentTransaction = t;
                }

                if (db.CurrentTransaction is not null)
                {
                    db.CurrentTransaction.Abort(ex.Message);
                    var message = db.CurrentTransaction?.Error ?? ex.Message;

                    db.TransactionCollectStack.Clear();
                    db.TransactionTasks.Clear();
                    db.TransactionState = TAState.Collecting;
                    db.CurrentTransaction = null;

                    if (ex.GetType() == typeof(JSException))
                    {
                        throw new TransactionException(message);
                    }
                    else
                    {
                        throw;
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        public static async ValueTask<T> Open<T>(this T dexie) where T : DBBase, IDBBase
        {
            var reference = await dexie.DBBaseJS.InvokeAsync<IJSInProcessObjectReference>("open");
            return (T)T.Create(dexie.DBBaseJS.Module, reference, dexie.CloudSync);
        }

        public static bool IsOpen(this DBBase dexie)
        {
            return dexie.DBBaseJS.Invoke<bool>("isOpen");
        }

        public static void Close(this DBBase dexie)
        {
            dexie.DBBaseJS.InvokeVoid("close");
        }

        public static async ValueTask Delete(this DBBase dexie)
        {
            await dexie.DBBaseJS.InvokeVoidAsync("delete");
        }

        public static string Name(this DBBase dexie)
        {
            return dexie.DBBaseJS.Module.Invoke<string>("Name");
        }

        public static double Version(this DBBase dexie)
        {
            return dexie.DBBaseJS.Module.Invoke<double>("Version", dexie.DBBaseJS.Reference);
        }
    }
}