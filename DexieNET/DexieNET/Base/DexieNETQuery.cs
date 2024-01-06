/*
DexieNETQuery.cs

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

using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DexieNET
{
    internal static class KeyFactory
    {
        public static Func<object> AsQuery<Q>(Func<Q[][]> keyRangeDelegate, ITypeConverter converter)
        {
            return () => AsQuery(keyRangeDelegate(), converter);
        }

        public static object AsQuery<Q>(Q[][] keyRange, ITypeConverter converter)
        {
            return keyRange.Select(v1 => v1.Select(v2 => AsQuery(v2, converter)).ToArray()).ToArray();
        }

        public static Func<object> AsQuery<Q>(Func<Q[]> keysDelegate, ITypeConverter converter)
        {
            return () => AsQuery(keysDelegate(), converter);
        }

        public static object AsQuery<Q>(Q[] keys, ITypeConverter converter)
        {
            return keys.Select(v => AsQuery(v, converter)).ToArray();
        }

        public static Func<object?> AsQuery<Q>(Func<Q> keyDelegate, ITypeConverter converter)
        {
            return () => AsQuery(keyDelegate(), converter);
        }

        public static object AsQuery<Q>(Q key, ITypeConverter converter)
        {
            if (key is null)
            {
                throw new InvalidOperationException("EntityKey not set.");
            }

            if (typeof(Q).IsAssignableTo(typeof(ITuple)))
            {

                var iTuple = (ITuple)key;

                return Enumerable.Range(0, iTuple.Length)
                    .Select(i => converter.Convert(iTuple[i]))
                    .Where(v => v is not null)
                    .Select(v => v!)
                    .ToArray();
            }

            return converter.Convert(key);
        }

        public static (T1, T2) Create2<T1, T2>(JsonElement jsonElement1, JsonElement jsonElement2, ITypeConverter converter)
        {
            var value1 = jsonElement1.Deserialize<T1>();
            value1 = (T1?)converter.Convert(value1);

            if (value1 is null)
            {
                throw new InvalidOperationException($"Can not deserialize to ({typeof(T1).Name}, invalid JSON.");
            }


            var value2 = jsonElement2.Deserialize<T2>();
            value2 = (T2?)converter.Convert(value2);

            if (value2 is null)
            {
                throw new InvalidOperationException($"Can not deserialize to ({typeof(T2).Name}, invalid JSON.");
            }

            return (value1, value2);
        }

        public static (T1, T2, T3) Create3<T1, T2, T3>(JsonElement jsonElement1, JsonElement jsonElement2, JsonElement jsonElement3, ITypeConverter converter)
        {
            var value1 = jsonElement1.Deserialize<T1>();
            value1 = (T1?)converter.Convert(value1);

            if (value1 is null)
            {
                throw new InvalidOperationException($"Can not deserialize to ({typeof(T1).Name}, invalid JSON.");
            }

            var value2 = jsonElement2.Deserialize<T2>();
            value2 = (T2?)converter.Convert(value2);

            if (value2 is null)
            {
                throw new InvalidOperationException($"Can not deserialize to ({typeof(T2).Name}, invalid JSON.");
            }

            var value3 = jsonElement3.Deserialize<T3>();
            value3 = (T3?)converter.Convert(value3);

            if (value3 is null)
            {
                throw new InvalidOperationException($"Can not deserialize to ({typeof(T3).Name}, invalid JSON.");
            }

            return (value1, value2, value3);
        }
    }

    internal class Query<T, Q> : Update<T> where T : IDBStore
    {
    }

    internal class Update<T> : Dictionary<string, object?> where T : IDBStore
    {
    }

    internal class Delete<T> : List<string> where T : IDBStore
    {
    }

    internal static class QueryFactory<T> where T : IDBStore
    {
        public static Update<T> Update<Q>(Expression<Func<T, Q>> query, Q? value)
        {
            return Query(query, value);
        }

        public static Update<T> Update<Q1, Q2>
           (Expression<Func<T, Q1>> query1, Q1? value1,
           Expression<Func<T, Q2>> query2, Q2? value2)
        {
            return Query(query1, value1, query2, value2);
        }

        public static Update<T> Update<Q1, Q2>
          (Expression<Func<T, Q1>> query1, Expression<Func<T, Q2>> query2,
          (Q1? value1, Q2? value2) values)
        {
            return Query(query1, values.value1, query2, values.value2);
        }

        public static Update<T> Update<Q1, Q2, Q3>
            (Expression<Func<T, Q1>> query1, Q1? value1,
            Expression<Func<T, Q2>> query2, Q2? value2,
            Expression<Func<T, Q3>> query3, Q3? value3)
        {
            return Query(query1, value1, query2, value2, query3, value3);
        }

        public static Update<T> Update<Q1, Q2, Q3>
            (Expression<Func<T, Q1>> query1, Expression<Func<T, Q2>> query2, Expression<Func<T, Q3>> query3,
            (Q1? value1, Q2? value2, Q3? value3) values)
        {
            return Query(query1, values.value1, query2, values.value2, query3, values.value3);
        }

        public static Query<T, Q> Query<Q>
            (Expression<Func<T, Q>> query, Q? value)
        {
            return new Query<T, Q>
            {
                { query.GetKey().ToCamelCase(), value },
            };
        }

        public static Query<T, (Q1, Q2)> Query<Q1, Q2>
            (Expression<Func<T, Q1>> query1, Q1? value1,
            Expression<Func<T, Q2>> query2, Q2? value2)
        {
            return new Query<T, (Q1, Q2)>
            {
                { query1.GetKey().ToCamelCase(), value1 },
                { query2.GetKey().ToCamelCase(), value2 }
            };
        }

        public static Query<T, (Q1, Q2)> Query<Q1, Q2>
            (Expression<Func<T, Q1>> query1, Expression<Func<T, Q2>> query2,
            (Q1? value1, Q2? value2) values)
        {
            return Query(query1, values.value1, query2, values.value2);
        }

        public static Query<T, (Q1, Q2, Q3)> Query<Q1, Q2, Q3>
        (Expression<Func<T, Q1>> query1, Q1? value1,
        Expression<Func<T, Q2>> query2, Q2? value2,
        Expression<Func<T, Q3>> query3, Q3? value3)
        {
            return new Query<T, (Q1, Q2, Q3)>
            {
                { query1.GetKey().ToCamelCase(), value1 },
                { query2.GetKey().ToCamelCase(), value2 },
                { query3.GetKey().ToCamelCase(), value3 }
            };
        }

        public static Query<T, (Q1, Q2, Q3)> Query<Q1, Q2, Q3>
            (Expression<Func<T, Q1>> query1, Expression<Func<T, Q2>> query2, Expression<Func<T, Q3>> query3,
            (Q1? value1, Q2? value2, Q3? value3) values)
        {
            return Query(query1, values.value1, query2, values.value2, query3, values.value3);
        }
    }

    public class DBQuery<T, I, Q>(ITypeConverter? converter, params string[] keys) where T : IDBStore
    {
        public string[] Keys { get; } = keys;

        private readonly ITypeConverter? _converter = converter;
        private readonly bool _compoundType = typeof(Q).IsAssignableTo(typeof(ITuple));

        public IEnumerable<Q> AsEnumerable(JsonElement jsonElement)
        {
            return AsEnumerable<Q>(jsonElement, false);
        }

        public IEnumerable<K> AsEnumerable<K>(JsonElement jsonElement, bool tryConvert = true)
        {
            if (jsonElement.ValueKind is not JsonValueKind.Array)
            {
                throw new InvalidOperationException("Can not deserialize single type to array.");
            }

            var jsonElements = jsonElement.Deserialize<IEnumerable<JsonElement>>();

            if (jsonElements is null)
            {
                throw new InvalidOperationException($"Can not deserialize to IEnumerable<{typeof(K).Name}>, invalid JSON.");
            }

            List<K> values = [];

            foreach (var element in jsonElements)
            {
                if (element.ValueKind is JsonValueKind.Array && !_compoundType)
                {
                    var jsonSubElements = element.Deserialize<IEnumerable<JsonElement>>();

                    if (jsonSubElements is null)
                    {
                        throw new InvalidOperationException($"Can not deserialize to IEnumerable<{typeof(K).Name}>, invalid JSON.");
                    }

                    foreach (var subElement in jsonSubElements)
                    {
                        if (tryConvert)
                        {
                            try
                            {
                                values.Add(AsObject<K>(subElement));
                            }
                            catch (Exception ex)
                            {
                                if (ex.GetType() != typeof(JsonException))
                                {
                                    throw;
                                }
                            }
                        }
                        else
                        {
                            values.Add(AsObject<K>(subElement));
                        }
                    }
                }
                else
                {
                    if (tryConvert)
                    {
                        try
                        {
                            values.Add(AsObject<K>(element));
                        }
                        catch (Exception ex)
                        {
                            if (ex.GetType() != typeof(JsonException))
                            {
                                throw;
                            }
                        }
                    }
                    else
                    {
                        values.Add(AsObject<K>(element));
                    }
                }
            }

            return values;
        }

        public K AsObject<K>(JsonElement jsonElement)
        {
            if (jsonElement.ValueKind is JsonValueKind.Array && !_compoundType)
            {
                throw new InvalidOperationException("Can not deserialize compound type to simple type.");
            }

            if (jsonElement.ValueKind is not JsonValueKind.Array && _compoundType)
            {
                throw new InvalidOperationException("Can not deserialize simple type to compound type.");
            }

            if (!_compoundType)
            {

                var value = jsonElement.Deserialize<K>();

                if (value is null)
                {
                    throw new InvalidOperationException($"Can not deserialize to {typeof(K).Name}, invalid JSON.");
                }

                return value;
            }

            var jsonElements = jsonElement.Deserialize<IEnumerable<JsonElement>>();
            var ga = typeof(K).GetGenericArguments();

            if (jsonElements is null || jsonElements.Count() != ga.Length)
            {
                throw new InvalidOperationException($"Can not deserialize to {typeof(K).Name}, invalid JSON.");
            }

            MethodInfo? mi = ga.Length switch
            {
                2 => typeof(KeyFactory).GetMethod("Create2")?.MakeGenericMethod(ga),
                3 => typeof(KeyFactory).GetMethod("Create3")?.MakeGenericMethod(ga),
                // And add other cases as needed
                _ => throw new NotSupportedException($"Can not deserialize to {typeof(K).Name}, invalid tuple length."),
            };

            if (mi is null)
            {
                throw new InvalidOperationException($"Can not deserialize to {typeof(K).Name}, invalid tuple creation.");
            }

            var args = jsonElements.Select(je => (object)je);
            var argsArray = args.Append(_converter).ToArray();

            var tuple = mi.Invoke(null, argsArray);

            if (tuple is null)
            {
                throw new InvalidOperationException($"Can not deserialize to {typeof(K).Name}, invalid tuple.");
            }

            return (K)tuple;
        }
    }

    internal static class QueryExtensions
    {
        public static string GetKey<T, V>(this Expression<Func<T, V>> expression)
        {
            return expression.Body.GetKey();
        }

        public static string GetKey<T>(this Expression<Func<T>> expression)
        {
            return expression.Body.GetKey();
        }

        private static string GetKey(this Expression? expressionBody)
        {
            string propertyName = string.Empty;

            while (expressionBody is not null)
            {
                if (expressionBody.NodeType == ExpressionType.MemberAccess)
                {
                    var memberExpression = (MemberExpression)expressionBody;
                    propertyName = propertyName.Length == 0 ?
                        memberExpression.Member.Name : $"{memberExpression.Member.Name}." + propertyName;

                    if (propertyName == "DefaultPrimaryKey")
                    {
                        return ":id";
                    }

                    expressionBody = memberExpression?.Expression;
                }
                else if (expressionBody.NodeType == ExpressionType.Parameter)
                {
                    break;
                }
                else
                {
                    throw new InvalidOperationException("Not supported expression type.");
                }
            }

            return propertyName.ToCamelCase();
        }
    }
}
