/*
DexieNETVersion.cs

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
using System.Reflection;

namespace DexieNET
{
    public sealed class Version : IDisposable
    {
        internal Dictionary<string, string> Stores { get; }

        internal Dictionary<string, (string StoreName, string Schema)> UpdateStores { get; }

        internal Transaction Transaction { get; }

        internal JSObject VersionJS { get; }

        private readonly DotNetObjectReference<Version> _dotnetRef;

        private Func<Transaction, Task>? _upgrade;

        public Version(DBBase db, Dictionary<string, string> stores, Dictionary<string, (string, string)> updateStores, IJSObjectReference reference)
        {
            VersionJS = new(db.DBBaseJS.Module, reference);
            _dotnetRef = DotNetObjectReference.Create(this);
            _upgrade = null;
            Transaction = new(db, false);
            Stores = stores;
            UpdateStores = updateStores;
        }

        [JSInvokable]
        public async ValueTask UpgradeCallback()
        {
            Transaction.SetJSO(Transaction.Module.Invoke<IJSObjectReference>("CurrentTransaction"));

            if (_upgrade is not null)
            {
                try
                {
                    await _upgrade(Transaction);
                }
                catch (Exception ex)
                {
                    Transaction.Abort(ex.Message);
                    throw new TransactionException(ex.Message);
                }
            }
        }

        public async ValueTask Upgrade(Func<Transaction, Task> upgrade)
        {
            _upgrade = upgrade;
            var reference = await VersionJS.Module.InvokeAsync<IJSObjectReference>("Upgrade", VersionJS.Reference, _dotnetRef);

            VersionJS.SetJSO(reference);
        }

        public void Dispose()
        {
            _dotnetRef.Dispose();
        }
    }

    public static class VersionExtensions
    {
        public static async ValueTask<Version> Version(this ValueTask<DBBase> dexieT, double versionNumber)
        {
            var dexie = await dexieT;
            return await dexie.Version(versionNumber);
        }

        public static async ValueTask Upgrade(this ValueTask<Version> versionT, Func<Transaction, Task> upgrade)
        {
            var version = await versionT;
            await version.Upgrade(upgrade);
        }

        public static async ValueTask<Version> Stores(this ValueTask<Version> versionT)
        {
            var version = await versionT;
            return await version.Stores();
        }

        public static async ValueTask<Version> Stores<T1>(this ValueTask<Version> versionT) where T1 : IDBStore
        {
            var version = await versionT;
            return await version.Stores<T1>();
        }

        public static async ValueTask<Version> Stores<T1, T2>(this ValueTask<Version> versionT) where T1 : IDBStore where T2 : IDBStore
        {
            var version = await versionT;
            return await version.Stores<T1, T2>();
        }

        public static async ValueTask<Version> Stores<T1, T2, T3>(this ValueTask<Version> versionT) where T1 : IDBStore where T2 : IDBStore where T3 : IDBStore
        {
            var version = await versionT;
            return await version.Stores<T1, T2, T3>();
        }

        public static async ValueTask<Version> Stores(this Version version)
        {
            var reference = await version.VersionJS.InvokeAsync<IJSObjectReference>("stores", version.Stores);
            return new Version(version.Transaction.DB, version.Stores, version.UpdateStores, reference);
        }

        public static async ValueTask<Version> Stores<T1>(this Version version) where T1 : IDBStore
        {
            Dictionary<string, string> stores = new();

            if (version.UpdateStores.TryGetValue(typeof(T1).Name.ToLowerInvariant(), out var store1))
            {
                stores.Add(store1.StoreName, store1.Schema);
            }
            else
            {
                throw new InvalidOperationException($"No schema found for {nameof(T1)}.");
            }

            var reference = await version.VersionJS.InvokeAsync<IJSObjectReference>("stores", stores);
            return new Version(version.Transaction.DB, version.Stores, version.UpdateStores, reference);
        }

        public static async ValueTask<Version> Stores<T1, T2>(this Version version) where T1 : IDBStore where T2 : IDBStore
        {
            Dictionary<string, string> stores = new();

            if (version.UpdateStores.TryGetValue(typeof(T1).Name.ToLowerInvariant(), out var store1))
            {
                stores.Add(store1.StoreName, store1.Schema);
            }
            else
            {
                throw new InvalidOperationException($"No schema found for {nameof(T1)}.");
            }

            if (version.UpdateStores.TryGetValue(typeof(T2).Name.ToLowerInvariant(), out var store2))
            {
                stores.Add(store2.StoreName, store2.Schema);
            }
            else
            {
                throw new InvalidOperationException($"No schema found for {nameof(T2)}.");
            }

            var reference = await version.VersionJS.InvokeAsync<IJSObjectReference>("stores", stores);
            return new Version(version.Transaction.DB, version.Stores, version.UpdateStores, reference);
        }

        public static async ValueTask<Version> Stores<T1, T2, T3>(this Version version) where T1 : IDBStore where T2 : IDBStore where T3 : IDBStore
        {
            Dictionary<string, string> stores = new();

            if (version.UpdateStores.TryGetValue(typeof(T1).Name.ToLowerInvariant(), out var store1))
            {
                stores.Add(store1.StoreName, store1.Schema);
            }
            else
            {
                throw new InvalidOperationException($"No schema found for {nameof(T1)}.");
            }

            if (version.UpdateStores.TryGetValue(typeof(T2).Name.ToLowerInvariant(), out var store2))
            {
                stores.Add(store2.StoreName, store2.Schema);
            }
            else
            {
                throw new InvalidOperationException($"No schema found for {nameof(T2)}.");
            }

            if (version.UpdateStores.TryGetValue(typeof(T3).Name.ToLowerInvariant(), out var store3))
            {
                stores.Add(store3.StoreName, store3.Schema);
            }
            else
            {
                throw new InvalidOperationException($"No schema found for {nameof(T3)}.");
            }

            var reference = await version.VersionJS.InvokeAsync<IJSObjectReference>("stores", stores);
            return new Version(version.Transaction.DB, version.Stores, version.UpdateStores, reference);
        }
    }
}