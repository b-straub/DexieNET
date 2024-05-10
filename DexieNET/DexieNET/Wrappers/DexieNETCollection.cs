/*
DexieNETCollection.cs

Copyright(c) 2024 Bernhard Straub

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
    internal sealed class CollectionJS<T, I, Q> : IDisposable where T : IDBStore
    {
        public DotNetObjectReference<CollectionJS<T, I, Q>> DotnetRef { get; }

        private readonly List<Func<T, bool>> _filterList;
        private readonly List<Func<T, bool>> _untilList;
        private Action<T>? _each;
        private Action<I>? _eachPrimaryKey;
        private Action<Q>? _eachKey;
        private Action<Q>? _eachUniqueKey;
        private Func<T, object?>? _modify;

        public CollectionJS()
        {
            DotnetRef = DotNetObjectReference.Create(this);
            _filterList = [];
            _untilList = [];
        }

        public CollectionJS(CollectionJS<T, I, Q> other)
        {
            _filterList = other._filterList;
            _untilList = other._untilList;
            _each = other._each;
            _eachPrimaryKey = other._eachPrimaryKey;
            _eachUniqueKey = other._eachUniqueKey;
            _modify = other._modify;

            DotnetRef = DotNetObjectReference.Create(this);
        }

        internal int AddFilter(Func<T, bool> filter)
        {
            _filterList.Add(filter);
            return _filterList.Count - 1;
        }

        internal int AddUntil(Func<T, bool> until)
        {
            _untilList.Add(until);
            return _untilList.Count - 1;
        }

        internal void SetEach(Action<T> each)
        {
            _each = each;
        }

        internal void SetEachPrimaryKey(Action<I> eachPrimaryKey)
        {
            _eachPrimaryKey = eachPrimaryKey;
        }

        internal void SetEachKey(Action<Q> eachKey)
        {
            _eachKey = eachKey;
        }

        internal void SetEachUniqueKey(Action<Q> eachUniqueKey)
        {
            _eachUniqueKey = eachUniqueKey;
        }

        internal void SetModify(Func<T, object?> modify)
        {
            _modify = modify;
        }

        internal bool Filter(T item)
        {
            return _filterList.Aggregate(true, (current, next) => current && next(item));
        }

        [JSInvokable]
        public bool Filter(T? item, int index)
        {
            if (index < 0 || index > _filterList.Count || item is null)
            {
                throw new InvalidOperationException($"Collection invalid Filter callback.");
            }

            return _filterList[index](item);
        }

        [JSInvokable]
        public IEnumerable<T> FilterItems(IEnumerable<T> items)
        {
            if (_filterList.Count == 0)
            {
                throw new InvalidOperationException($"Collection invalid FilterItems callback.");
            }

            return items.Where(v => Filter(v));
        }

        [JSInvokable]
        public bool Until(T? item, int index)
        {
            if (index < 0 || index > _untilList.Count || item is null)
            {
                throw new InvalidOperationException($"Collection invalid Until callback.");
            }

            return _untilList[index](item);
        }

        [JSInvokable]
        public void Each(T? item)
        {
            if (_each is null || item is null)
            {
                throw new InvalidOperationException($"Collection invalid Each callBack.");
            }

            _each(item);
        }

        [JSInvokable]
        public void EachPrimaryKey(I? key)
        {
            if (_eachPrimaryKey is null || key is null)
            {
                throw new InvalidOperationException($"Collection invalid EachPrimaryKey callBack.");
            }

            _eachPrimaryKey(key);
        }

        [JSInvokable]
        public void EachKey(Q? query)
        {
            if (_eachKey is null || query is null)
            {
                throw new InvalidOperationException($"Collection invalid EachKey callBack.");
            }

            _eachKey(query);
        }

        [JSInvokable]
        public void EachUniqueKey(Q? query)
        {
            if (_eachUniqueKey is null || query is null)
            {
                throw new InvalidOperationException($"Collection invalid EachUniqueKey callBack.");
            }

            _eachUniqueKey(query);
        }

        [JSInvokable]
        public object? Modify(T? item)
        {
            if (_modify is null || item is null)
            {
                throw new InvalidOperationException($"Collection invalid Modify callBack.");
            }

            var modified = _modify(item).FromObject();
            return modified;
        }

        public void Dispose()
        {
            DotnetRef.Dispose();
        }
    }

    public class Collection<T, I, Q> : WhereClause<T, I, Q> where T : IDBStore
    {
        internal CollectionJS<T, I, Q> CollectionJS { get; }

        public Collection(Table<T, I> table, IJSInProcessObjectReference? reference, params string[] keyArray) : base(table, reference, keyArray)
        {
            CollectionJS = new();
            JSObject.SetReference(reference);
        }

        public Collection(WhereClause<T, I, Q> whereClause, IJSInProcessObjectReference? reference) : base(whereClause.Table, whereClause.JSObject?.Reference, whereClause.Keys)
        {
            CollectionJS = new();
            JSObject.SetReference(reference);
        }

        public Collection(Collection<T, I, Q> other, IJSInProcessObjectReference? reference) : base(other.Table, reference, other.Keys)
        {
            CollectionJS = new(other.CollectionJS);
            JSObject.SetReference(reference);
        }

        internal void SetJSO(IJSInProcessObjectReference reference)
        {
            JSObject.SetReference(reference);
        }
    }

    public static class CollectionExtensions
    {
        #region And
        public static Collection<T, I, Q> And<T, I, Q>(this Collection<T, I, Q> collection, Func<T, bool> filter) where T : IDBStore
        {
            return collection.Filter(filter);
        }
        #endregion

        #region Clone
        public static Collection<T, I, Q> Clone<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return collection;
            }

            var reference = collection.JSObject.Invoke<IJSInProcessObjectReference>("clone");
            var newCollection = new Collection<T, I, Q>(collection, reference);
            return newCollection;
        }
        #endregion

        #region Count
        public static async ValueTask<double> Count<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return 0;
            }

            return await collection.JSObject.InvokeAsync<double>("count");
        }

        public static async ValueTask<R?> Count<T, I, Q, R>(this Collection<T, I, Q> collection, Func<double, R?> callback) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return default;
            }

            var count = await collection.JSObject.InvokeAsync<double>("count");
            return callback(count);
        }
        #endregion

        #region Delete
        public static async ValueTask<double> Delete<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.ReadWrite)))
            {
                return 0;
            }

            return await collection.JSObject.InvokeAsync<double>("delete");
        }
        #endregion

        // Desc -> Deprecated

        #region Distinct
        public static Collection<T, I, Q> Distinct<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return collection;
            }

            var reference = collection.JSObject.Invoke<IJSInProcessObjectReference>("distinct");
            collection.SetJSO(reference);
            return collection;
        }
        #endregion

        #region Each
        public static async ValueTask Each<T, I, Q>(this Collection<T, I, Q> collection, Action<T> each) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return;
            }

            collection.CollectionJS.SetEach(each);
            await collection.JSObject.Module.InvokeVoidAsync("CollectionEach", collection.JSObject.Reference, collection.CollectionJS.DotnetRef);
        }
        #endregion

        #region EachKey
        public static async ValueTask EachKey<T, I, Q>(this Collection<T, I, Q> collection, Action<Q> query) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return;
            }

            collection.CollectionJS.SetEachKey(query);
            await collection.JSObject.Module.InvokeVoidAsync("CollectionEachKey", collection.JSObject.Reference, collection.CollectionJS.DotnetRef);
        }
        #endregion

        #region EachPrimaryKey
        public static async ValueTask EachPrimaryKey<T, I, Q>(this Collection<T, I, Q> collection, Action<I> key) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return;
            }

            collection.CollectionJS.SetEachPrimaryKey(key);
            await collection.JSObject.Module.InvokeVoidAsync("CollectionEachPrimaryKey", collection.JSObject.Reference, collection.CollectionJS.DotnetRef);
        }
        #endregion

        #region EachUniqueKey
        public static async ValueTask EachUniqueKey<T, I, Q>(this Collection<T, I, Q> collection, Action<Q> query) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return;
            }

            collection.CollectionJS.SetEachUniqueKey(query);
            await collection.JSObject.Module.InvokeVoidAsync("CollectionEachUniqueKey", collection.JSObject.Reference, collection.CollectionJS.DotnetRef);
        }
        #endregion

        #region Filter
        public static Collection<T, I, Q> Filter<T, I, Q>(this Collection<T, I, Q> collection, Func<T, bool> filter) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return collection;
            }

            var filterIndex = collection.CollectionJS.AddFilter(filter);


            var reference = collection.JSObject.Module.Invoke<IJSInProcessObjectReference>("CollectionFilter", collection.JSObject.Reference, collection.CollectionJS.DotnetRef, filterIndex);
            collection.SetJSO(reference);
            return collection;
        }
        #endregion

        #region First
        public static async ValueTask<T?> First<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return default;
            }

            return await collection.JSObject.InvokeAsync<T?>("first");
        }

        public static async ValueTask<R?> First<T, I, Q, R>(this Collection<T, I, Q> collection, Func<T?, R?> callback) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return default;
            }

            var first = await collection.JSObject.InvokeAsync<T?>("first");
            return callback(first);
        }
        #endregion

        #region Keys
        public static async ValueTask<IEnumerable<Q>> Keys<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.MixedType)
            {
                throw new InvalidOperationException("Please use 'Keys(Expression)' for collections with mixed type keys");
            }

            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<Q>();
            }

            if (typeof(Q).Equals(typeof(byte[])))
            {
                return await collection.JSObject.Module.InvokeAsync<IEnumerable<Q>>("CollectionKeysByteArray", collection.JSObject.Reference);
            }

            var js = await collection.JSObject.InvokeAsync<JsonElement>("keys");
            return collection.AsEnumerable(js);
        }

        public static async ValueTask<R?> Keys<T, I, Q, R>(this Collection<T, I, Q> collection, Func<IEnumerable<Q>, R?> callback) where T : IDBStore
        {
            if (collection.MixedType)
            {
                throw new InvalidOperationException("Please use 'Keys(Expression)' for collections with mixed type keys");
            }

            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return default;
            }

            if (typeof(Q).Equals(typeof(byte[])))
            {
                var keysBA = await collection.JSObject.Module.InvokeAsync<IEnumerable<Q>>("CollectionKeysByteArray", collection.JSObject.Reference);
                return callback(keysBA);
            }

            var js = await collection.JSObject.InvokeAsync<JsonElement>("keys");
            var keys = collection.AsEnumerable(js);
            return callback(keys);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Type safety")]
        public static async ValueTask<IEnumerable<Q>> Keys<T, I, K, Q>(this Collection<T, I, K> collection, Expression<Func<T, Q>> query) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<Q>();
            }

            if (typeof(Q).Equals(typeof(byte[])))
            {
                return await collection.JSObject.Module.InvokeAsync<IEnumerable<Q>>("CollectionKeysByteArray", collection.JSObject.Reference);
            }

            var js = await collection.JSObject.InvokeAsync<JsonElement>("keys");
            return collection.AsEnumerable<Q>(js);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Type safety")]
        public static async ValueTask<R?> Keys<T, I, K, Q, R>(this Collection<T, I, K> collection, Expression<Func<T, Q>> query, Func<IEnumerable<Q>, R?> callback) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return default;
            }

            if (typeof(Q).Equals(typeof(byte[])))
            {
                var keysBA = await collection.JSObject.Module.InvokeAsync<IEnumerable<Q>>("CollectionKeysByteArray", collection.JSObject.Reference);
                return callback(keysBA);
            }

            var js = await collection.JSObject.InvokeAsync<JsonElement>("keys");
            var keys = collection.AsEnumerable<Q>(js);
            return callback(keys);
        }
        #endregion

        #region Last
        public static async ValueTask<T?> Last<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return default;
            }

            return await collection.JSObject.InvokeAsync<T?>("last");
        }

        public static async ValueTask<R?> Last<T, I, Q, R>(this Collection<T, I, Q> collection, Func<T?, R?> callback) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return default;
            }

            var last = await collection.JSObject.InvokeAsync<T?>("last");
            return callback(last);
        }
        #endregion

        #region Limit
        public static Collection<T, I, Q> Limit<T, I, Q>(this Collection<T, I, Q> collection, double count) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return collection;
            }

            var reference = collection.JSObject.Invoke<IJSInProcessObjectReference>("limit", count);
            collection.SetJSO(reference);
            return collection;
        }
        #endregion

        #region Modify
        internal static async ValueTask<double> Modify<T, I, Q>(this Collection<T, I, Q> collection, Update<T> update) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.ReadWrite)))
            {
                return 0;
            }

            return await collection.JSObject.Module.InvokeAsync<double>("Modify", collection.JSObject.Reference, update);
        }

        public static async ValueTask<double> Modify<T, I, Q>(this Collection<T, I, Q> collection, Func<T, object?> modify) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.ReadWrite)))
            {
                return 0;
            }

            collection.CollectionJS.SetModify(modify);
            return await collection.JSObject.Module.InvokeAsync<double>("CollectionModify", collection.JSObject.Reference, collection.CollectionJS.DotnetRef);
        }

        public static async ValueTask<double> ModifyReplacePrefix<T, I, K>(this Collection<T, I, K> collection, Expression<Func<T, string>> query, string a, string b) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.ReadWrite)))
            {
                return 0;
            }

            return await collection.JSObject.Module.InvokeAsync<double>("CollectionModifyReplacePrefix", collection.JSObject.Reference, query.GetKey(), a, b);
        }

        public static async ValueTask<double> Modify<T, I, K, Q>(this Collection<T, I, K> collection, Expression<Func<T, Q>> query, Q? value) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Update(query, value);
            return await collection.Modify(queryF);
        }

        public static async ValueTask<double> Modify<T, I, K, Q1, Q2>(this Collection<T, I, K> collection,
            Expression<Func<T, Q1>> query1, Q1? value1, Expression<Func<T, Q2>> query2, Q2? value2) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Update(query1, value1, query2, value2);
            return await collection.Modify(queryF);
        }

        public static async ValueTask<double> Modify<T, I, K, Q1, Q2, Q3>(this Collection<T, I, K> collection,
            Expression<Func<T, Q1>> query1, Q1 value1, Expression<Func<T, Q2>> query2, Q2 value2, Expression<Func<T, Q3>> query3, Q3 value3) where T : IDBStore
        {
            var queryF = QueryFactory<T>.Update(query1, value1, query2, value2, query3, value3);
            return await collection.Modify(queryF);
        }
        #endregion

        #region Offset
        public static Collection<T, I, Q> Offset<T, I, Q>(this Collection<T, I, Q> collection, double count) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return collection;
            }

            var reference = collection.JSObject.Invoke<IJSInProcessObjectReference>("offset", count);
            collection.SetJSO(reference);
            return collection;
        }
        #endregion

        #region Or
        // difference to Dexie Keys will return Keys from 'Or' WhereClause and not from the first one in chain
        public static WhereClause<T, I, Q> Or<T, I, K, Q>(this Collection<T, I, K> collection, Expression<Func<T, Q>> query) where T : IDBStore
        {
            var key = query.GetKey();

            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return collection.Table.EmptyWhereClause<Q>(key);
            }

            var reference = collection.JSObject.Invoke<IJSInProcessObjectReference>("or", query.GetKey());
            return new WhereClause<T, I, Q>(collection.Table, reference, key)
            {
                MixedType = typeof(K) != typeof(Q)
            };
        }

        #endregion

        #region PrimaryKeys
        public static async ValueTask<IEnumerable<I>> PrimaryKeys<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<I>();
            }

            if (typeof(I).Equals(typeof(byte[])))
            {
                return await collection.JSObject.Module.InvokeAsync<IEnumerable<I>>("CollectionPrimarykeysByteArray", collection.JSObject.Reference);
            }

            return await collection.JSObject.InvokeAsync<IEnumerable<I>>("primaryKeys");
        }
        #endregion

        // Raw

        #region Reverse
        public static Collection<T, I, Q> Reverse<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return collection;
            }

            var reference = collection.JSObject.Invoke<IJSInProcessObjectReference>("reverse");
            collection.SetJSO(reference);
            return collection;
        }
        #endregion

        #region SortBy
        public static async ValueTask<IEnumerable<T>> SortBy<T, I, K, Q>(this Collection<T, I, K> collection, Expression<Func<T, Q>> query) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<T>();
            }

            var key = query.GetKey();
            return await collection.JSObject.InvokeAsync<IEnumerable<T>>("sortBy", key);
        }

        public static async ValueTask<IEnumerable<T>> SortBy<T, I, K, Q>(this Collection<T, I, K> collection, Expression<Func<T, Q[]>> query) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<T>();
            }

            var key = query.GetKey();
            return await collection.JSObject.InvokeAsync<IEnumerable<T>>("sortBy", key);
        }
        #endregion

        #region ToArray
        public static async ValueTask<IEnumerable<T>> ToArray<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<T>();
            }

            return await collection.JSObject.InvokeAsync<IEnumerable<T>>("toArray");
        }
        #endregion

        #region UniqueKeys
        public static async ValueTask<IEnumerable<Q>> UniqueKeys<T, I, Q>(this Collection<T, I, Q> collection) where T : IDBStore
        {
            if (collection.MixedType)
            {
                throw new InvalidOperationException("Please use 'UniqueKeys(Expression)' for collections with mixed type keys");
            }

            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<Q>();
            }

            if (typeof(Q).Equals(typeof(byte[])))
            {
                return await collection.JSObject.Module.InvokeAsync<IEnumerable<Q>>("CollectionKeysByteArray", collection.JSObject.Reference);
            }

            var js = await collection.JSObject.InvokeAsync<JsonElement>("uniqueKeys");
            return collection.AsEnumerable(js);
        }

        public static async ValueTask<R?> UniqueKeys<T, I, Q, R>(this Collection<T, I, Q> collection, Func<IEnumerable<Q>, R?> callback) where T : IDBStore
        {
            if (collection.MixedType)
            {
                throw new InvalidOperationException("Please use 'UniqueKeys(Expression)' for collections with mixed type keys");
            }

            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return default;
            }

            if (typeof(Q).Equals(typeof(byte[])))
            {
                var keysBA = await collection.JSObject.Module.InvokeAsync<IEnumerable<Q>>("CollectionKeysByteArray", collection.JSObject.Reference);
                return callback(keysBA);
            }

            var js = await collection.JSObject.InvokeAsync<JsonElement>("uniqueKeys");
            var keys = collection.AsEnumerable(js);
            return callback(keys);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Type safety")]
        public static async ValueTask<IEnumerable<Q>> UniqueKeys<T, I, K, Q>(this Collection<T, I, K> collection, Expression<Func<T, Q>> query) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return Enumerable.Empty<Q>();
            }

            if (typeof(Q).Equals(typeof(byte[])))
            {
                return await collection.JSObject.Module.InvokeAsync<IEnumerable<Q>>("CollectionKeysByteArray", collection.JSObject.Reference);
            }

            var js = await collection.JSObject.InvokeAsync<JsonElement>("uniqueKeys");
            return collection.AsEnumerable<Q>(js);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Type safety")]
        public static async ValueTask<R?> UniqueKeys<T, I, K, Q, R>(this Collection<T, I, K> collection, Expression<Func<T, Q>> query, Func<IEnumerable<Q>, R?> callback) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return default;
            }

            if (typeof(Q).Equals(typeof(byte[])))
            {
                var keysBA = await collection.JSObject.Module.InvokeAsync<IEnumerable<Q>>("CollectionKeysByteArray", collection.JSObject.Reference);
                return callback(keysBA);
            }

            var js = await collection.JSObject.InvokeAsync<JsonElement>("uniqueKeys");
            var keys = collection.AsEnumerable<Q>(js);
            return callback(keys);
        }
        #endregion

        #region Until
        public static Collection<T, I, Q> Until<T, I, Q>(this Collection<T, I, Q> collection, Func<T, bool> until, bool includeStopEntry = false) where T : IDBStore
        {
            if (collection.Table.AddTableInfo((collection.Table.Name, TAMode.Read)))
            {
                return collection;
            }

            var untilIndex = collection.CollectionJS.AddUntil(until);
            var reference = collection.JSObject.Module.Invoke<IJSInProcessObjectReference>("CollectionUntil", collection.JSObject.Reference, collection.CollectionJS.DotnetRef, includeStopEntry, untilIndex);
            collection.SetJSO(reference);
            return collection;
        }
        #endregion
    }
}