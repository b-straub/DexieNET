﻿/*
HelperExtensions.cs

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
*/

using Humanizer;
using System;
using System.Text.Json;

namespace DNTGenerator.Helpers
{
    public static class HelperExtensions
    {
        public static bool True(this bool? value)
        {
            return value.GetValueOrDefault(false);
        }

        public static bool False(this bool? value)
        {
            return !value.GetValueOrDefault(true);
        }

        public static string ToCamelCase(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            var parts = str.Split('.');

            return parts
                .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p))
                .Aggregate((curr, next) => curr + "." + next)
                .TrimEnd('.');
        }

        public static string TrimEnd(this string source, string trimString)
        {
            if (source.EndsWith(trimString))
            {
                source = source[..source.LastIndexOf(trimString)];
            }

            return source;
        }

        public static string LowerFirstChar(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;

            return char.ToLower(str[0]) + str[1..];
        }

        public static string MakeDBOrTableName(this string name, bool isDB, bool isInterface)
        {
            if (isInterface)
            {
                name = name.TrimStart('I');
            }

            if (isDB)
            {
                if (name.ToLowerInvariant().EndsWith("db"))
                {
                    name = name[..^2];
                }

                if (!isInterface)
                {
                    name = name.Pluralize();
                }
                name += "DB";
            }
            else
            {
                name = name.Pluralize();
            }

            return name;
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        {
            return source.Where(x => x is not null)!;
        }

        public static IEnumerable<T> SelectRecursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> recursiveSelector)
        {
            foreach (T? i in source)
            {
                yield return i;

                IEnumerable<T>? directChildren = recursiveSelector(i);
                IEnumerable<T>? allChildren = SelectRecursive(directChildren, recursiveSelector);

                foreach (T? c in allChildren)
                {
                    yield return c;
                }
            }
        }
    }
}
