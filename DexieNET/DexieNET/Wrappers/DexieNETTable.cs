/*
DexieNETTable.cs

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
using System.Linq.Expressions;
using System.Text.Json;

namespace DexieNET
{
    public interface IGuidStore<T> where T : IDBStore
    {
        public T AssignPrimaryKey();
    }

    public interface ITable
    {
        public string Name { get; }
        public bool CloudSync { get; }
    }

    internal sealed class TableJS : DexieJSObject
    {
        public TableJS(IJSInProcessObjectReference module, IJSInProcessObjectReference reference) : base(module, reference)
        {
        }
    }

    public class Table<T, I> : ITable where T : IDBStore
    {
        public Expression<Func<T, I>> PrimaryKey => _ => DefaultPrimaryKey;
        public string Name { get; }
        public bool CloudSync { get; }

        internal DBBase DB { get; }
        internal bool PKGuid { get; }
        internal string[] Keys { get; }
        internal string[] MultiEntry { get; }
        internal ITypeConverter TypeConverter { get; }
        internal I DefaultPrimaryKey { get; }

        internal bool TransactionCollectMode => (DB.CurrentTransaction is not null && DB.CurrentTransaction.Collecting);
        internal TableJS TableJS { get; }

        private readonly Dictionary<Type, object> _emptyCollection;
        private readonly Dictionary<Type, object> _emptyWhereClause;

        public Table(DBBase db, IJSInProcessObjectReference reference, string name, ITypeConverter typeConverter, string[] keys, string[] multiEntry, bool pkGuid, bool cloudSync)
        {
            if (!typeof(I).IsAllowedPrimaryIndexType())
            {
                throw new InvalidOperationException($"{typeof(I).Name} can not be used as primary index.");
            }

            if (cloudSync && !db.CloudSync)
            {
                throw new InvalidOperationException($"Table {name} has CloudSync schema but DB is not cloud based.");
            }

            TableJS = new(db.DBBaseJS.Module, reference);
            DB = db;
            Name = name;
            PKGuid = pkGuid;
            TypeConverter = typeConverter;
            Keys = keys;
            MultiEntry = multiEntry;
            CloudSync = cloudSync;
            _emptyCollection = new();
            _emptyWhereClause = new();
            DefaultPrimaryKey = HelperExtensions.GetDefaultPrimaryKey<I>();
        }

        internal bool IsMultiEntryKey(string key)
        {
            return MultiEntry.Contains(key);
        }

        internal bool IsMultiEntryKey(string[] keys)
        {
            return MultiEntry.Intersect(keys).Any();
        }

        internal bool AddTableInfo((string Name, TAMode Mode) tableInfo)
        {
            if (DB.CurrentTransaction is not null)
            {
                return DB.CurrentTransaction.AddTableInfo(tableInfo);
            }

            return false;
        }

        internal Collection<T, I, Q> EmptyCollection<Q>(params string[] keyArray)
        {
            if (_emptyCollection.TryGetValue(typeof(Q), out var collection))
            {
                return (Collection<T, I, Q>)collection;
            }

            collection = new Collection<T, I, Q>(this, null, keyArray);
            _emptyCollection.Add(typeof(Q), collection);
            return (Collection<T, I, Q>)collection;
        }

        internal WhereClause<T, I, Q> EmptyWhereClause<Q>(params string[] keyArray)
        {
            if (_emptyWhereClause.TryGetValue(typeof(Q), out var whereClause))
            {
                return (WhereClause<T, I, Q>)whereClause;
            }

            whereClause = new WhereClause<T, I, Q>(this, null, keyArray);
            _emptyWhereClause.Add(typeof(Q), whereClause);
            return (WhereClause<T, I, Q>)whereClause;
        }

        internal Collection<T, I, Q> ToCollection<Q>()
        {
            if (AddTableInfo((Name, TAMode.Read)))
            {
                return EmptyCollection<Q>(Keys.First());
            }

            var reference = TableJS.Invoke<IJSInProcessObjectReference>("toCollection");
            return new(this, reference, Keys.First());
        }
    }

    public static class TableExtensions
    {
        #region Cloud
        public static IUsePermissions<T> CreateUsePermissions<T, I>(this Table<T, I> table) where T : IDBStore, IDBCloudEntity
        {
            return UsePermissions<T, I>.Create(table);
        }
        #endregion

        #region Add
        public static async ValueTask<I> Add<T, I>(this Table<T, I> table, T? item, I primaryKey) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return table.DefaultPrimaryKey;
            }

            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var jsi = item.FromObject();

            if (typeof(I).Equals(typeof(byte[])))
            {
                return await table.TableJS.Module.InvokeAsync<I>("AddByteArray", table.TableJS.Reference, jsi, primaryKey);
            }

            var keyQ = KeyFactory.AsQuery(primaryKey, table.TypeConverter);
            var key = await table.TableJS.InvokeAsync<JsonElement>("add", jsi, keyQ);
            var query = new DBQuery<T, I, I>(table.TypeConverter);
            return query.AsObject<I>(key);
        }

        public static async ValueTask<I> Add<T, I>(this Table<T, I> table, T? item) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return table.DefaultPrimaryKey;
            }

            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (table.PKGuid)
            {
                item = ((IGuidStore<T>)item).AssignPrimaryKey();
            }

            var jsi = item.FromObject();
            return await table.TableJS.InvokeAsync<I>("add", jsi);
        }
        #endregion

        #region BulkAdd
        public static async ValueTask<IEnumerable<I>> BulkAdd<T, I>(this Table<T, I> table, IEnumerable<T> items, IEnumerable<I> primaryKeys, bool allKeys = false) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return Enumerable.Empty<I>();
            }

            var jsi = items.FromObject();

            if (typeof(I).Equals(typeof(byte[])))
            {
                return await table.TableJS.Module.InvokeAsync<IEnumerable<I>>("BulkAddByteArray", table.TableJS.Reference, jsi, primaryKeys, allKeys);
            }

            Dictionary<string, bool> options = new() { { "allKeys", allKeys } };
            var queries = primaryKeys.Select(p => KeyFactory.AsQuery(p, table.TypeConverter));

            if (allKeys)
            {
                var keys = await table.TableJS.InvokeAsync<JsonElement>("bulkAdd", jsi, queries, options);
                var query = new DBQuery<T, I, I>(table.TypeConverter);
                return query.AsEnumerable(keys);
            }
            else
            {
                var key = await table.TableJS.InvokeAsync<JsonElement>("bulkAdd", jsi, queries, options);
                var query = new DBQuery<T, I, I>(table.TypeConverter);
                return new[] { query.AsObject<I>(key) };
            }
        }

        public static async ValueTask<IEnumerable<I>> BulkAdd<T, I>(this Table<T, I> table, IEnumerable<T> items, bool allKeys = false) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return Enumerable.Empty<I>();
            }

            Dictionary<string, bool> options = new() { { "allKeys", allKeys } };

            if (table.PKGuid)
            {
                items = items.Select(i => ((IGuidStore<T>)i).AssignPrimaryKey());
            }

            var jsi = items.FromObject();

            if (allKeys)
            {
                var keys = await table.TableJS.InvokeAsync<JsonElement>("bulkAdd", jsi, options);
                var query = new DBQuery<T, I, I>(table.TypeConverter);
                return query.AsEnumerable(keys);
            }
            else
            {
                var key = await table.TableJS.InvokeAsync<JsonElement>("bulkAdd", jsi, options);
                var query = new DBQuery<T, I, I>(table.TypeConverter);
                return new[] { query.AsObject<I>(key) };
            }
        }
        #endregion

        #region BulkDelete
        public static async ValueTask BulkDelete<T, I>(this Table<T, I> table, IEnumerable<I> primaryKeys) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return;
            }

            await table.TableJS.InvokeVoidAsync("bulkDelete", primaryKeys);
        }
        #endregion

        #region BulkGet
        public static async ValueTask<IEnumerable<T>> BulkGet<T, I>(this Table<T, I> table, IEnumerable<I> primaryKeys) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<T>();
            }

            var queries = primaryKeys.Select(p => KeyFactory.AsQuery(p, table.TypeConverter));
            return await table.TableJS.InvokeAsync<IEnumerable<T>>("bulkGet", queries);
        }
        #endregion

        #region BulkPut
        public static async ValueTask<IEnumerable<I>> BulkPut<T, I>(this Table<T, I> table, IEnumerable<T> items, IEnumerable<I> primaryKeys, bool allKeys = false) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return Enumerable.Empty<I>();
            }

            var jsi = items.FromObject();

            if (typeof(I).Equals(typeof(byte[])))
            {
                return await table.TableJS.Module.InvokeAsync<IEnumerable<I>>("BulkPutByteArray", table.TableJS.Reference, jsi, primaryKeys, allKeys);
            }

            Dictionary<string, bool> options = new() { { "allKeys", allKeys } };
            var queries = primaryKeys.Select(p => KeyFactory.AsQuery(p, table.TypeConverter));

            if (allKeys)
            {
                var keys = await table.TableJS.InvokeAsync<JsonElement>("bulkPut", jsi, queries, options);
                var query = new DBQuery<T, I, I>(table.TypeConverter);
                return query.AsEnumerable(keys);
            }
            else
            {
                var key = await table.TableJS.InvokeAsync<JsonElement>("bulkPut", jsi, queries, options);
                var query = new DBQuery<T, I, I>(table.TypeConverter);
                return new[] { query.AsObject<I>(key) };
            }
        }

        public static async ValueTask<IEnumerable<I>> BulkPut<T, I>(this Table<T, I> table, IEnumerable<T> items, bool allKeys = false) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return Enumerable.Empty<I>();
            }

            Dictionary<string, bool> options = new() { { "allKeys", allKeys } };

            if (table.PKGuid)
            {
                items = items.Select(i => ((IGuidStore<T>)i).AssignPrimaryKey());
            }

            var jsi = items.FromObject();

            if (allKeys)
            {
                var keys = await table.TableJS.InvokeAsync<JsonElement>("bulkPut", jsi, options);
                var query = new DBQuery<T, I, I>(table.TypeConverter);
                return query.AsEnumerable(keys);
            }
            else
            {
                var key = await table.TableJS.InvokeAsync<JsonElement>("bulkPut", jsi, options);
                var query = new DBQuery<T, I, I>(table.TypeConverter);
                return new[] { query.AsObject<I>(key) };
            }
        }
        #endregion

        #region Clear
        public static async ValueTask Clear<T, I>(this Table<T, I> table) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return;
            }

            await table.TableJS.InvokeVoidAsync("clear");
        }
        #endregion

        #region Count
        public static async ValueTask<double> Count<T, I>(this Table<T, I> table) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return 0;
            }

            return await table.TableJS.InvokeAsync<double>("count");
        }

        public static async ValueTask<R?> Count<T, I, R>(this Table<T, I> table, Func<double, R?> callback) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return default;
            }

            var count = await table.TableJS.InvokeAsync<double>("count");
            return callback(count);
        }
        #endregion

        #region Delete
        public static async ValueTask Delete<T, I>(this Table<T, I> table, I primaryKey) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return;
            }

            await table.TableJS.InvokeVoidAsync("delete", primaryKey);
        }
        #endregion

        #region Filter
        public static Collection<T, I, I> Filter<T, I>(this Table<T, I> table, Func<T, bool> filter) where T : IDBStore
        {
            var collection = table.ToCollection<I>();

            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return collection;
            }

            collection = collection.Filter(filter);
            return collection;
        }
        #endregion

        #region Get
        public static async ValueTask<T?> Get<T, I>(this Table<T, I> table, I primaryKey) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return default;
            }

            var keyQ = KeyFactory.AsQuery(primaryKey, table.TypeConverter);
            return await table.TableJS.InvokeAsync<T?>("get", keyQ);
        }

        internal static async ValueTask<T?> Get<T, I, Q>(this Table<T, I> table, Query<T, Q> query) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return default;
            }

            var queryConverted = table.TypeConverter?.Convert(query) ?? query;
            return await table.TableJS.InvokeAsync<T?>("get", queryConverted);
        }

        public static async ValueTask<T?> Get<T, I, Q>(this Table<T, I> table, Expression<Func<T, Q>> query, Q value) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Query(query, value);
            return await table.Get(queryF);
        }

        public static async ValueTask<T?> Get<T, I, Q1, Q2>(this Table<T, I> table,
            Expression<Func<T, Q1>> query1, Q1 value1, Expression<Func<T, Q2>> query2, Q2 value2) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Query(query1, value1, query2, value2);
            return await table.Get(queryF);
        }

        public static async ValueTask<T?> Get<T, I, Q1, Q2, Q3>(this Table<T, I> table,
            Expression<Func<T, Q1>> query1, Q1 value1, Expression<Func<T, Q2>> query2, Q2 value2, Expression<Func<T, Q3>> query3, Q3 value3) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Query(query1, value1, query2, value2, query3, value3);
            return await table.Get(queryF);
        }
        #endregion

        #region Limit
        public static Collection<T, I, I> Limit<T, I>(this Table<T, I> table, double count) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.ToCollection<I>();
            }

            return table.ToCollection<I>().Limit(count);
        }
        #endregion

        #region Offset
        public static Collection<T, I, I> Offset<T, I>(this Table<T, I> table, double count) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.ToCollection<I>();
            }

            return table.ToCollection<I>().Offset(count);
        }
        #endregion

        #region OrderBy
        public static Collection<T, I, Q> OrderBy<T, I, Q>(this Table<T, I> table, Expression<Func<T, Q>> query) where T : IDBStore
        {
            var key = query.GetKey();
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.EmptyCollection<Q>(key);
            }

            var reference = table.TableJS.Invoke<IJSInProcessObjectReference>("orderBy", key);
            return new(table, reference, key);
        }

        public static Collection<T, I, Q> OrderBy<T, I, Q>(this Table<T, I> table, Expression<Func<T, IEnumerable<Q>>> query) where T : IDBStore
        {
            var key = query.GetKey();

            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.EmptyCollection<Q>(key);
            }

            var reference = table.TableJS.Invoke<IJSInProcessObjectReference>("orderBy", query.GetKey());
            return new(table, reference, key);
        }

        public static Collection<T, I, (Q1, Q2)> OrderBy<T, I, Q1, Q2>(this Table<T, I> table, Expression<Func<T, Q1>> query1, Expression<Func<T, Q2>> query2) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.EmptyCollection<(Q1, Q2)>(query1.GetKey(), query2.GetKey());
            }

            object keys = new[] { query1.GetKey(), query2.GetKey() };

            var reference = table.TableJS.Invoke<IJSInProcessObjectReference>("orderBy", keys);
            return new(table, reference, query1.GetKey(), query2.GetKey());
        }

        public static Collection<T, I, (Q1, Q2, Q3)> OrderBy<T, I, Q1, Q2, Q3>(this Table<T, I> table, Expression<Func<T, Q1>> query1,
            Expression<Func<T, Q2>> query2, Expression<Func<T, Q3>> query3) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.EmptyCollection<(Q1, Q2, Q3)>(query1.GetKey(), query2.GetKey(), query3.GetKey());
            }

            object keys = new[] { query1.GetKey(), query2.GetKey(), query3.GetKey() };

            var reference = table.TableJS.Invoke<IJSInProcessObjectReference>("orderBy", keys);
            return new(table, reference, query1.GetKey(), query2.GetKey(), query3.GetKey());
        }
        #endregion

        #region Put
        public static async ValueTask<I> Put<T, I>(this Table<T, I> table, T? item, I primaryKey) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return table.DefaultPrimaryKey;
            }

            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var jsi = item.FromObject();

            if (typeof(I).Equals(typeof(byte[])))
            {
                return await table.TableJS.Module.InvokeAsync<I>("PutByteArray", table.TableJS.Reference, jsi, primaryKey);
            }

            var keyQ = KeyFactory.AsQuery(primaryKey, table.TypeConverter);
            var key = await table.TableJS.InvokeAsync<JsonElement>("put", jsi, keyQ);
            var query = new DBQuery<T, I, I>(table.TypeConverter);
            return query.AsObject<I>(key);
        }

        public static async ValueTask<I> Put<T, I>(this Table<T, I> table, T? item) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return table.DefaultPrimaryKey;
            }

            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (table.PKGuid)
            {
                item = ((IGuidStore<T>)item).AssignPrimaryKey();
            }

            var jsi = item.FromObject();
            return await table.TableJS.InvokeAsync<I>("put", jsi); ;
        }
        #endregion

        #region Reverse
        public static Collection<T, I, I> Reverse<T, I>(this Table<T, I> table) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.ToCollection<I>();
            }

            return table.ToCollection<I>().Reverse();
        }
        #endregion

        #region ToArray
        public static async ValueTask<IEnumerable<T>> ToArray<T, I>(this Table<T, I> table) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<T>();
            }

            return await table.TableJS.InvokeAsync<IEnumerable<T>>("toArray");
        }
        #endregion

        #region ToCollection
        public static Collection<T, I, I> ToCollection<T, I>(this Table<T, I> table) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.ToCollection<I>();
            }

            return table.ToCollection<I>();
        }
        #endregion

        #region Update
        internal static async ValueTask<bool> Update<T, I>(this Table<T, I> table, I primaryKey, Update<T> update) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.ReadWrite)))
            {
                return false;
            }

            var res = await table.TableJS.InvokeAsync<int>("update", primaryKey, update);
            return res > 0;
        }

        public static async ValueTask<bool> Update<T, I, Q>(this Table<T, I> table, I primaryKey, Expression<Func<T, Q>> query, Q value) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Update(query, value);
            return await table.Update(primaryKey, queryF);
        }

        public static async ValueTask<bool> Update<T, I, Q1, Q2>(this Table<T, I> table, I primaryKey,
            Expression<Func<T, Q1>> query1, Q1 value1, Expression<Func<T, Q2>> query2, Q2 value2) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Update(query1, value1, query2, value2);
            return await table.Update(primaryKey, queryF);
        }

        public static async ValueTask<bool> Update<T, I, Q1, Q2, Q3>(this Table<T, I> table, I primaryKey,
            Expression<Func<T, Q1>> query1, Q1 value1, Expression<Func<T, Q2>> query2, Q2 value2, Expression<Func<T, Q3>> query3, Q3 value3) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Update(query1, value1, query2, value2, query3, value3);
            return await table.Update(primaryKey, queryF);
        }
        #endregion

        #region Where
        internal static Collection<T, I, Q> Where<T, I, Q>(this Table<T, I> table, Query<T, Q> query) where T : IDBStore
        {
            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.EmptyCollection<Q>(query.Keys.ToArray());
            }

            var queryConverted = table.TypeConverter?.Convert(query) ?? query;
            var reference = table.TableJS.Invoke<IJSInProcessObjectReference>("where", queryConverted);
            return new(table, reference, query.Keys.ToArray());
        }

        public static Collection<T, I, Q> Where<T, I, Q>(this Table<T, I> table, Expression<Func<T, Q>> query, Q value) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Query(query, value);
            return table.Where(queryF);
        }

        public static Collection<T, I, (Q1, Q2)> Where<T, I, Q1, Q2>(this Table<T, I> table,
            Expression<Func<T, Q1>> query1, Q1 value1, Expression<Func<T, Q2>> query2, Q2 value2) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Query(query1, value1, query2, value2);
            return table.Where(queryF);
        }

        public static Collection<T, I, (Q1, Q2, Q3)> Where<T, I, Q1, Q2, Q3>(this Table<T, I> table,
            Expression<Func<T, Q1>> query1, Q1 value1, Expression<Func<T, Q2>> query2, Q2 value2, Expression<Func<T, Q3>> query3, Q3 value3) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Query(query1, value1, query2, value2, query3, value3);
            return table.Where(queryF);
        }

        public static WhereClause<T, I, Q> Where<T, I, Q>(this Table<T, I> table, Expression<Func<T, Q>> query) where T : IDBStore
        {
            var key = query.GetKey();

            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.EmptyWhereClause<Q>(key);
            }

            var reference = table.TableJS.Invoke<IJSInProcessObjectReference>("where", query.GetKey());
            return new(table, reference, key);
        }
        #endregion

        #region WhereMultiEntry
        public static WhereClause<T, I, Q> Where<T, I, Q>(this Table<T, I> table, Expression<Func<T, IEnumerable<Q>>> query) where T : IDBStore
        {
            var key = query.GetKey();

            if (!table.IsMultiEntryKey(key))
            {
                throw new InvalidOperationException("Use '[]' for 'Array Index'");
            }

            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.EmptyWhereClause<Q>(key);
            }

            var reference = table.TableJS.Invoke<IJSInProcessObjectReference>("where", key);
            return new(table, reference, key);
        }
        #endregion

        #region WhereMultipleValues
        public static WhereClause<T, I, (Q1, Q2)> Where<T, I, Q1, Q2>(this Table<T, I> table, Expression<Func<T, Q1>> query1, Expression<Func<T, Q2>> query2) where T : IDBStore
        {
            var keys = new[] { query1.GetKey(), query2.GetKey() };

            if (table.IsMultiEntryKey(keys))
            {
                throw new InvalidOperationException("'MultiEntry Index' not allowed for Compound queries");
            }

            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.EmptyWhereClause<(Q1, Q2)>(keys);
            }

            var reference = table.TableJS.Invoke<IJSInProcessObjectReference>("where", (object)keys);
            return new(table, reference, keys);
        }

        public static WhereClause<T, I, (Q1, Q2, Q3)> Where<T, I, Q1, Q2, Q3>(this Table<T, I> table, Expression<Func<T, Q1>> query1,
            Expression<Func<T, Q2>> query2, Expression<Func<T, Q3>> query3) where T : IDBStore
        {
            var keys = new[] { query1.GetKey(), query2.GetKey(), query3.GetKey() };

            if (table.IsMultiEntryKey(keys))
            {
                throw new InvalidOperationException("'MultiEntry Index' not allowed for Compound queries");
            }

            if (table.AddTableInfo((table.Name, TAMode.Read)))
            {
                return table.EmptyWhereClause<(Q1, Q2, Q3)>(keys);
            }

            var reference = table.TableJS.Invoke<IJSInProcessObjectReference>("where", (object)keys);
            return new(table, reference, keys);
        }
        #endregion
    }
}