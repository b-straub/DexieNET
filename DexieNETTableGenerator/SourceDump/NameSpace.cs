/*
NameSpace.cs

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

using DNTGenerator.Verifier;
using Microsoft.CodeAnalysis;
using System.Text;

namespace DNTGenerator.SourceDump
{
    internal static class NameSpace
    {
        public static string DumpNamespace(this IEnumerable<DBRecord> records, string usedNamespace, bool forCloud)
        {
            StringBuilder sb = new();

            var usings = DefaultUsings(forCloud);

            IEnumerable<DBRecord> usedRecords = records.Where(r => r.Namespace == usedNamespace);

            var recordGroups = usedRecords.GroupBy(r => r.DBName);

            _ = sb.Append(DumpNamespaceFirst(usedNamespace, usings, forCloud));

            foreach (var recordGroup in recordGroups)
            {
                _ = sb.Append(DumpRecords(recordGroup, recordGroup.Key));
            }
            _ = sb.Append(DumpNamespaceLast());

            return sb.ToString();
        }

        public static string DumpNamespaceFirst(string namespaceName, IEnumerable<string> usings, bool forCloud)
        {
            StringBuilder sb = new();

            foreach (string? u in usings)
            {
                _ = sb.Append(@$"using {u};
");
            };

            _ = sb.Append($@"
#nullable enable

namespace {namespaceName}
{{");
            return sb.ToString().Trim();
        }

        public static string DumpNamespaceLast()
        {
            StringBuilder sb = new();

            _ = sb.Append($@"
}}
");
            return sb.ToString();
        }

        private static string DumpRecords(this IEnumerable<DBRecord> records, string dbName)
        {
            StringBuilder sb = new();

            _ = sb.Append(records.MakeDBBuilder(dbName));

            foreach (DBRecord record in records)
            {
                if (record.HasGeneratedPrimaryKey() || record.HasNonCloudGuidPrimaryKey() || record.SchemaDescriptor.HasCloudSync)
                {
                    if (record.HasNonCloudGuidPrimaryKey())
                    {
                        _ = sb.Append($@"
    {record.AccessToString} partial {record.TypeName} {record.Symbol.Name} : IGuidStore <{record.Symbol.Name}>
    {{
");
                    }
                    else if (record.SchemaDescriptor.HasCloudSync)
                    {
                        _ = sb.Append($@"
    {record.AccessToString} partial {record.TypeName} {record.Symbol.Name} : IDBCloudEntity
    {{
");
                    }
                    else
                    {
                        _ = sb.Append($@"
    {record.AccessToString} partial {record.TypeName} {record.Symbol.Name}
    {{
");
                    }

                    _ = sb.Append(record.DumpProperties());
                    _ = sb.Append(record.DumpMethods());


                    _ = sb.Append($@"
    }}
");
                }
            }

            StringBuilder sb1 = new();

            _ = sb1.Append($@"
    public static partial class TableExtensions
    {{
");
            foreach (DBRecord record in records)
            {
                _ = sb1.Append(record.MakeTransaction(records));
            }

            sb.Append(sb1.ToString().TrimEnd());

            _ = sb.Append($@"
    }}");

            return sb.ToString();
        }

        private static IEnumerable<string> DefaultUsings(bool forCloud)
        {
            List<string> usings = new();
            UsingSortComparer comparer = new();

            usings.Add("Microsoft.JSInterop");
            usings.Add("System.CodeDom.Compiler");
            usings.Add("DexieNET");
            if (forCloud)
            {
                usings.Add("DexieCloudNET");
            }

            return usings.OrderBy(u => u, comparer);
        }

        private class UsingSortComparer : IComparer<string>
        {
            int IComparer<string>.Compare(string x, string y)
            {
                string? s1 = x;
                string? s2 = y;

                if (s1.StartsWith("System"))
                {
                    s1 = "0" + s1;
                }

                if (s2.StartsWith("System"))
                {
                    s2 = "0" + s2;
                }

                if (s1.StartsWith("Microsoft"))
                {
                    s1 = "1" + s1;
                }

                if (s2.StartsWith("Microsoft"))
                {
                    s2 = "1" + s2;
                }

                return s1.CompareTo(s2);
            }
        }
    }
}
