/*
DexieNETHelpers.cs

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

using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DexieNET
{
    internal static class HelperExtensions
    {
        // A key can be one of the following types: string, date, float, a binary blob, and array
        // JSInterop and dexie handle most of the conversion
        public static bool IsAllowedPrimaryIndexType(this Type t)
        {
            return
                t == typeof(sbyte) ||
                t == typeof(byte) ||
                t == typeof(short) ||
                t == typeof(int) ||
                t == typeof(long) ||
                t == typeof(ushort) ||
                t == typeof(uint) ||
                t == typeof(ulong) ||
                t == typeof(float) ||
                t == typeof(double) ||
                t == typeof(decimal) ||
                t == typeof(string) ||
                t == typeof(Guid) ||
                t == typeof(DateTime) ||
                t == typeof(TimeSpan) ||
                t.IsArray && (t.GetElementType()?.IsAllowedPrimaryIndexType()).GetValueOrDefault(false) ||
                t.IsAssignableTo(typeof(ITuple)) && !t.GenericTypeArguments.Where(t => !IsAllowedPrimaryIndexType(t)).Any();
        }

        public static T GetDefaultPrimaryKey<T>()
        {
            var type = typeof(T);

            object? o = type switch
            {
                _ when type.IsArray => type.GetElementType() is null ? null : Array.CreateInstance(type.GetElementType()!, 0),
                _ when type.IsAssignableTo(typeof(ITuple)) => MakeTuple<T>(type),
                _ => GetDefaultPrimaryKey(type)
            };

            if (o is null)
            {
                throw new InvalidOperationException($"Can not create DefaultPrimaryIndex for {type.Name}");
            }

            return (T)o;
        }

        private static object? GetDefaultPrimaryKey(Type type)
        {
            return type switch
            {
                _ when type == typeof(sbyte) => (sbyte)0,
                _ when type == typeof(byte) => (byte)0,
                _ when type == typeof(short) => (short)0,
                _ when type == typeof(int) => 0,
                _ when type == typeof(long) => (long)0,
                _ when type == typeof(ushort) => (ushort)0,
                _ when type == typeof(uint) => (uint)0,
                _ when type == typeof(ulong) => (ulong)0,
                _ when type == typeof(float) => (float)0,
                _ when type == typeof(double) => (double)0,
                _ when type == typeof(decimal) => (decimal)0,
                _ when type == typeof(string) => string.Empty,
                _ when type == typeof(Guid) => Guid.Empty,
                _ when type == typeof(DateTime) => DateTime.UtcNow,
                _ when type == typeof(TimeSpan) => TimeSpan.Zero,
                _ => null
            };
        }

        private static T MakeTuple<T>(Type type)
        {
            var constructor = MakeTupleCTor(type);
            var ctorArguments = type.GenericTypeArguments.Select(ga => GetDefaultPrimaryKey(ga)).ToArray();

            var valueTuple = constructor?.Invoke(ctorArguments);

            if (valueTuple is null)
            {
                throw new InvalidOperationException($"No ValueTuple constructor found for {type.Name}");
            }

            return (T)valueTuple;
        }

        private static ConstructorInfo? MakeTupleCTor(Type type)
        {
            if (!type.IsAssignableTo(typeof(ITuple)))
            {
                return null;
            }

            Type? valueTupleType = type.GenericTypeArguments.Length switch
            {
                2 => typeof(ValueTuple<,>),
                3 => typeof(ValueTuple<,,>),
                4 => typeof(ValueTuple<,,,>),
                5 => typeof(ValueTuple<,,,,>),
                6 => typeof(ValueTuple<,,,,,>),
                7 => typeof(ValueTuple<,,,,,,>),
                8 => typeof(ValueTuple<,,,,,,,>),
                _ => null
            };

            var constructor = valueTupleType?.MakeGenericType(type.GenericTypeArguments).GetConstructor(type.GenericTypeArguments);
            return constructor;
        }
    }
}
