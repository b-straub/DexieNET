/*
DexieNETWhereClause.cs

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

namespace DexieNET
{
    public class WhereClause<T, I, Q> : DBQuery<T, I, Q> where T : IDBStore
    {
        internal Table<T, I> Table { get; }

        internal DexieJSObject JSObject { get; }

        internal ITypeConverter TypeConverter { get; }

        internal bool MixedType { get; set; }

        public WhereClause(Table<T, I> table, IJSInProcessObjectReference? reference, params string[] keyArray) : base(table.TypeConverter, keyArray)
        {
            if (reference is null && !table.TransactionCollectMode)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            Table = table;
            VerifyKey(Table.Keys, keyArray);
            TypeConverter = table.TypeConverter;
            JSObject = new(table.TableJS.Module, reference);
        }

        private void VerifyKey(string[] keys, params string[] keyArray)
        {
            var key = string.Empty;

            if (keyArray.Length == 1)
            {
                key = keyArray.First();
                if (key == ":id") // implicit outbound primary key
                {
                    return;
                }
            }

            var keyIntersection = keys.Intersect(keyArray);

            if (!keyIntersection.Any())
            {
                throw new InvalidOperationException($"Can not create WhereClause for table '{Table.Name}' with '{key}'.");
            }
        }
    }

    public static class WhereClauseExtensions
    {
        public static Collection<T, I, Q> EmptyCollection<T, I, Q>(this WhereClause<T, I, Q> whereClause) where T : IDBStore
        {
            return whereClause.Table.EmptyCollection<Q>(whereClause.Keys);
        }

        public static Collection<T, I, Q> Above<T, I, Q>(this WhereClause<T, I, Q> whereClause, Q lowerBound) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var lowerBoundQ = KeyFactory.AsQuery(lowerBound, whereClause.TypeConverter);
            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("above", lowerBoundQ);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        public static Collection<T, I, Q> AboveOrEqual<T, I, Q>(this WhereClause<T, I, Q> whereClause, Q lowerBound) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var lowerBoundQ = KeyFactory.AsQuery(lowerBound, whereClause.TypeConverter);
            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("aboveOrEqual", lowerBoundQ);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        public static Collection<T, I, Q> AnyOf<T, I, Q>(this WhereClause<T, I, Q> whereClause, params Q[] keys) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var keysQ = KeyFactory.AsQuery(keys, whereClause.TypeConverter);
            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("anyOf", keysQ);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        public static Collection<T, I, Q> AnyOfIgnoreCase<T, I, Q>(this WhereClause<T, I, Q> whereClause, params string[] keys) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var keysQ = KeyFactory.AsQuery(keys, whereClause.TypeConverter);
            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("anyOfIgnoreCase", keysQ);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        public static Collection<T, I, Q> Below<T, I, Q>(this WhereClause<T, I, Q> whereClause, Q upperBound) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var upperBoundQ = KeyFactory.AsQuery(upperBound, whereClause.TypeConverter);
            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("below", upperBoundQ);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        public static Collection<T, I, Q> BelowOrEqual<T, I, Q>(this WhereClause<T, I, Q> whereClause, Q upperBound) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var upperBoundQ = KeyFactory.AsQuery(upperBound, whereClause.TypeConverter);
            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("belowOrEqual", upperBoundQ);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        public static Collection<T, I, Q> Between<T, I, Q>(this WhereClause<T, I, Q> whereClause,
            Q lowerBound, Q upperBound, bool includeLower = true, bool includeUpper = false) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var lowerBoundQ = KeyFactory.AsQuery(lowerBound, whereClause.TypeConverter);
            var upperBoundQ = KeyFactory.AsQuery(upperBound, whereClause.TypeConverter);

            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("between", lowerBoundQ, upperBoundQ, includeLower, includeUpper);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        // renamed from dexie Equals to Equal because of build in extension
        public static Collection<T, I, Q> Equal<T, I, Q>(this WhereClause<T, I, Q> whereClause, Q key) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var keyQ = KeyFactory.AsQuery(key, whereClause.TypeConverter);
            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("equals", keyQ);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        // renamed from dexie EqualsIgnoreCase to EqualIgnoreCase for consistency with Equal
        public static Collection<T, I, string> EqualIgnoreCase<T, I>(this WhereClause<T, I, string> whereClause, string key) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("equalsIgnoreCase", key);
            return new Collection<T, I, string>(whereClause, reference);
        }

        public class AnyRangeOptions : Dictionary<string, object>
        {
            public AnyRangeOptions(bool includeLower, bool includeUpper)
            {
                Add("includeLowers", includeLower);
                Add("includeUppers", includeUpper);
            }
        }

        public static Collection<T, I, Q> InAnyRange<T, I, Q>(this WhereClause<T, I, Q> whereClause, Q[][] ranges, bool includeLower = true, bool includeUpper = false) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var rangesQ = KeyFactory.AsQuery(ranges, whereClause.TypeConverter);
            var options = new AnyRangeOptions(includeLower, includeUpper);

            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("inAnyRange", rangesQ, options);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        public static Collection<T, I, Q> NoneOf<T, I, Q>(this WhereClause<T, I, Q> whereClause, params Q[] keys) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var keysQ = KeyFactory.AsQuery(keys, whereClause.TypeConverter);
            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("noneOf", keysQ);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        public static Collection<T, I, Q> NotEqual<T, I, Q>(this WhereClause<T, I, Q> whereClause, Q key) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var keyQ = KeyFactory.AsQuery(key, whereClause.TypeConverter);
            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("notEqual", keyQ);
            return new Collection<T, I, Q>(whereClause, reference);
        }

        public static Collection<T, I, string> StartsWith<T, I>(this WhereClause<T, I, string> whereClause, string prefix) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("startsWith", prefix);
            return new Collection<T, I, string>(whereClause, reference);
        }

        public static Collection<T, I, string> StartsWithIgnoreCase<T, I>(this WhereClause<T, I, string> whereClause, string prefix) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("startsWithIgnoreCase", prefix);
            return new Collection<T, I, string>(whereClause, reference);
        }

        public static Collection<T, I, string> StartsWithAnyOf<T, I>(this WhereClause<T, I, string> whereClause, params string[] prefixes) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("startsWithAnyOf", prefixes);
            return new Collection<T, I, string>(whereClause, reference);
        }

        public static Collection<T, I, string> StartsWithAnyOfIgnoreCase<T, I>(this WhereClause<T, I, string> whereClause, params string[] prefixes) where T : IDBStore
        {
            if (whereClause.Table.AddTableInfo((whereClause.Table.Name, TAMode.Read)))
            {
                return whereClause.EmptyCollection();
            }

            var reference = whereClause.JSObject.Invoke<IJSInProcessObjectReference>("startsWithAnyOfIgnoreCase", prefixes);
            return new Collection<T, I, string>(whereClause, reference);
        }
    }
}